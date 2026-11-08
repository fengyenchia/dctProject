import { createServer } from 'node:http';
import { mkdirSync, existsSync, readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { createHash, randomBytes } from 'node:crypto';
import { DatabaseSync } from 'node:sqlite';
import { WebSocketServer, WebSocket } from 'ws';
import { createAdmin } from './gateway-admin.js';

const data = resolve('data');
mkdirSync(data, { recursive: true });
const keyPath = resolve(data, 'gateway-key.txt');
if (!existsSync(keyPath)) writeFileSync(keyPath, randomBytes(32).toString('hex'), { mode: 0o600 });
const key = readFileSync(keyPath, 'utf8').trim();
const db = new DatabaseSync(resolve(data, 'tutorial.sqlite'));
db.exec(`PRAGMA foreign_keys=ON;
CREATE TABLE IF NOT EXISTS results(result_id TEXT PRIMARY KEY, session_id TEXT UNIQUE NOT NULL,
 token TEXT UNIQUE NOT NULL, payload_json TEXT NOT NULL, payload_hash TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS outbox(result_id TEXT PRIMARY KEY REFERENCES results(result_id), status TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS print_jobs(job_id TEXT PRIMARY KEY, result_id TEXT UNIQUE NOT NULL REFERENCES results(result_id), status TEXT NOT NULL);`);

type Item = { variant_id: string; stem_id: string };
type Payload = { result_id: string; token: string; san: number; capture_count: number; recycled: boolean;
 variants: Item[]; melody: { stem_ids: string[] }; [key: string]: unknown };
type Envelope = { schema_version: number; type: string; event_id: string; session_id: string; revision: number; payload: Payload };

function valid(value: unknown): value is Envelope {
  if (!value || typeof value !== 'object') return false;
  const e = value as Envelope;
  const p = e.payload;
  return e.schema_version === 1 && e.type === 'RESULT_READY' && typeof e.event_id === 'string'
    && typeof e.session_id === 'string' && Number.isInteger(e.revision) && !!p
    && typeof p.result_id === 'string' && !!p.result_id && typeof p.token === 'string' && /^[a-f0-9]{64}$/.test(p.token)
    && typeof p.asset_version === 'string' && /^[a-zA-Z0-9_-]+$/.test(p.asset_version)
    && typeof p.recipe_version === 'string' && !!p.recipe_version
    && p.san === 0 && p.capture_count === 5 && p.recycled === true && Array.isArray(p.variants)
    && p.variants.length === 5 && new Set(p.variants.map(v => v?.variant_id)).size === 5
    && p.variants.every(v => /^[ABC][123]$/.test(v?.variant_id) && v.stem_id === `stem-${v.variant_id}`)
    && p.variants.every(v => ['name', 'source_label', 'image_id'].every(field => typeof (v as Record<string, unknown>)[field] === 'string'))
    && Array.isArray(p.melody?.stem_ids) && JSON.stringify(p.melody.stem_ids) === JSON.stringify(p.variants.map(v => v.stem_id));
}

function canonical(value: unknown): string {
  if (Array.isArray(value)) return '[' + value.map(canonical).join(',') + ']';
  if (value && typeof value === 'object') return '{' + Object.entries(value).sort(([a], [b]) => a.localeCompare(b))
    .map(([k, v]) => JSON.stringify(k) + ':' + canonical(v)).join(',') + '}';
  return JSON.stringify(value) ?? 'null';
}

function save(e: Envelope): void {
  const json = canonical(e.payload);
  const hash = createHash('sha256').update(json).digest('hex');
  const prior = db.prepare('SELECT payload_hash, session_id FROM results WHERE result_id=?').get(e.payload.result_id);
  if (prior) {
    if (prior.payload_hash !== hash || prior.session_id !== e.session_id) throw new Error('Result conflict');
    return;
  }
  db.exec('BEGIN IMMEDIATE');
  try {
    db.prepare('INSERT INTO results VALUES(?,?,?,?,?)').run(e.payload.result_id, e.session_id, e.payload.token, json, hash);
    db.prepare('INSERT INTO outbox VALUES(?,?)').run(e.payload.result_id, 'pending');
    db.prepare('INSERT INTO print_jobs VALUES(?,?,?)').run(e.payload.result_id + ':automatic', e.payload.result_id, 'pending');
    db.exec('COMMIT');
  } catch (error) { db.exec('ROLLBACK'); throw error; }
}

const wss = new WebSocketServer({ host: '127.0.0.1', port: 8787, maxPayload: 65536 });
let active: WebSocket | undefined;
let activeSession: string | undefined;
let delivery: { getStatus(id: string): unknown; list(): unknown[]; confirmHandoff(id: string, mode: 'printed'): unknown; reprint(id: string): Promise<unknown>; start(): void } | undefined;
const admin = createAdmin({ db, key, unity: () => ({ socket: active, session: activeSession }), delivery: () => delivery });
wss.on('connection', socket => {
  let session: string | undefined;
  const timeout = setTimeout(() => socket.close(1008, 'Handshake required'), 5000);
  socket.on('close', () => { clearTimeout(timeout); if (active === socket) { active = undefined; activeSession = undefined; } });
  socket.on('error', () => {});
  socket.on('message', raw => {
    try {
      const message: unknown = JSON.parse(raw.toString());
      if (!session) {
        const hello = message as { type?: string; key?: string; session_id?: string };
        if (hello.type !== 'HELLO' || hello.key !== key || typeof hello.session_id !== 'string' || !hello.session_id
          || active && active !== socket && active.readyState === WebSocket.OPEN) { socket.close(1008, 'Rejected'); return; }
        session = hello.session_id; active = socket; activeSession = session; clearTimeout(timeout);
        socket.send(JSON.stringify({ type: 'HELLO_OK' })); return;
      }
      if (admin.receive(message as Parameters<typeof admin.receive>[0], session)) return;
      if (!valid(message) || message.session_id !== session) throw new Error('Invalid result');
      save(message);
      socket.send(JSON.stringify({ type: 'RESULT_SAVED', session_id: session, event_id: message.event_id, result_id: message.payload.result_id }));
    } catch (error) { socket.send(JSON.stringify({ type: 'ERROR', message: error instanceof Error ? error.message : 'Rejected' })); }
  });
});

createServer(async (request, response) => {
  response.setHeader('Content-Type', 'application/json; charset=utf-8');
  response.setHeader('Cache-Control', 'no-store');
  if (await admin.handle(request, response)) return;
  if (request.method !== 'GET') { response.writeHead(405).end('{}'); return; }
  const path = new URL(request.url ?? '/', 'http://localhost').pathname;
  if (path === '/health') { response.end(JSON.stringify({ ok: true })); return; }
  const deliveryId = path.match(/^\/delivery\/([^/]+)$/)?.[1];
  if (deliveryId) {
    if (request.headers.authorization !== 'Bearer ' + key) { response.writeHead(401).end('{}'); return; }
    if (!delivery) { response.writeHead(503).end(JSON.stringify({ error: 'delivery_not_ready' })); return; }
    try {
      const status = delivery.getStatus(decodeURIComponent(deliveryId));
      response.writeHead(status ? 200 : 404).end(JSON.stringify(status ?? { error: 'not_saved' }));
    } catch { response.writeHead(400).end('{}'); }
    return;
  }
  const token = path.match(/^\/results\/([a-f0-9]{64})$/)?.[1];
  const row = token ? db.prepare('SELECT payload_json FROM results WHERE token=?').get(token) : undefined;
  if (!row) { response.writeHead(404).end(JSON.stringify({ error: 'not_ready_or_invalid' })); return; }
  response.end(String(row.payload_json));
}).listen(8788, '127.0.0.1', async () => {
  console.log('Tutorial Gateway: ws://127.0.0.1:8787 | http://127.0.0.1:8788/health');
  console.log('Local key file (copy contents to Unity persistentDataPath/gateway-key.txt): ' + keyPath);
  if (process.env.DELIVERY_ENABLED === '1') {
    try {
      // I1 不啟用此選項時，不需要安裝 I2 的票面依賴或複製其程式。
      const extensionPath = './delivery-service.js';
      const extension = await import(extensionPath);
      delivery = extension.createDeliveryService({ db, dataDir: data });
      delivery!.start();
      console.log('I2 delivery enabled; PRINT_MODE=' + (process.env.PRINT_MODE ?? 'file'));
    } catch (error) { console.error('I2 not ready:', error instanceof Error ? error.message : 'setup error'); }
  }
});
