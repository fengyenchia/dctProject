import { createServer } from 'node:http';
import { mkdirSync } from 'node:fs';
import { resolve, join } from 'node:path';
import { createHash } from 'node:crypto';
import { DatabaseSync } from 'node:sqlite';

// 僅供 I2 在同一電腦驗證上傳；正式 HTTPS API／Neon 由 W1 接續。
const secret = process.env.INGEST_SECRET;
if (!secret || secret.length < 24) throw new Error('Set a development INGEST_SECRET with at least 24 characters');
const data = resolve('data'); mkdirSync(data, { recursive: true });
const db = new DatabaseSync(join(data, 'mock-cloud.sqlite'));
db.exec('CREATE TABLE IF NOT EXISTS received(result_id TEXT PRIMARY KEY, session_id TEXT UNIQUE NOT NULL, token TEXT UNIQUE NOT NULL, hash TEXT NOT NULL)');
function canonical(value: unknown): string {
  if (Array.isArray(value)) return '[' + value.map(canonical).join(',') + ']';
  if (value && typeof value === 'object') return '{' + Object.entries(value).sort(([a],[b]) => a.localeCompare(b)).map(([k,v]) => JSON.stringify(k)+':'+canonical(v)).join(',') + '}';
  return JSON.stringify(value) ?? 'null';
}
createServer(async (request, response) => {
  response.setHeader('Content-Type', 'application/json');
  const reply = (status: number, value: unknown) => response.writeHead(status).end(JSON.stringify(value));
  if (request.method !== 'POST' || request.url !== '/ingest') { reply(404, { error: 'not_found' }); return; }
  if (request.headers.authorization !== 'Bearer ' + secret) { reply(401, { error: 'unauthorized' }); return; }
  try {
    let raw = '';
    for await (const chunk of request) { raw += chunk.toString(); if (Buffer.byteLength(raw) > 65536) throw new Error('body_too_large'); }
    const e = JSON.parse(raw); const p = e?.payload;
    if (e?.schema_version !== 1 || typeof e.session_id !== 'string' || typeof p?.result_id !== 'string'
      || !/^[a-f0-9]{64}$/.test(p?.token) || p.san !== 0 || p.capture_count !== 5 || p.recycled !== true
      || !Array.isArray(p.variants) || p.variants.length !== 5
      || p.variants.some((v: { variant_id?: string } | null) => !v || !/^[ABC][123]$/.test(v.variant_id ?? ''))
      || new Set(p.variants.map((v: { variant_id: string }) => v.variant_id)).size !== 5) {
      reply(400, { error: 'invalid_result' }); return;
    }
    const hash = createHash('sha256').update(canonical(p)).digest('hex');
    const previous = db.prepare('SELECT hash,session_id FROM received WHERE result_id=?').get(p.result_id);
    if (previous && (previous.hash !== hash || previous.session_id !== e.session_id)) { reply(409, { error: 'conflict' }); return; }
    try { if (!previous) db.prepare('INSERT INTO received VALUES(?,?,?,?)').run(p.result_id, e.session_id, p.token, hash); }
    catch { reply(409, { error: 'conflict' }); return; }
    reply(200, { result_id: p.result_id, status: 'saved' });
  } catch { reply(400, { error: 'invalid_request' }); }
}).listen(9190, '127.0.0.1', () => console.log('Prototype upload receiver: http://127.0.0.1:9190/ingest (not a public cloud service)'));
