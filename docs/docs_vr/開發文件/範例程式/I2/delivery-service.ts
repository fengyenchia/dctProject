import { existsSync } from 'node:fs';
import { randomUUID } from 'node:crypto';
import type { DatabaseSync } from 'node:sqlite';
import { renderTicket, type TicketResult, type TicketFiles } from './ticket-renderer.js';
import { printTicket } from './windows-ticket-print.js';

type Saved = { result_id: string; session_id: string; payload_json: string };
type FileRow = { result_id: string; png_file: string; svg_file: string; qr_url: string; layout_version: string };
type StatusRow = { status: string };
export type DeliveryConfig = {
  resultsBase: string; ingestUrl: string; ingestSecret: string;
  printMode: 'file' | 'windows'; printerName: string; allowLocalIngest: boolean; requireArt: boolean;
};
export function deliveryConfig(): DeliveryConfig {
  return {
    resultsBase: process.env.RESULTS_BASE_URL ?? '', ingestUrl: process.env.INGEST_URL ?? '',
    ingestSecret: process.env.INGEST_SECRET ?? '', printMode: process.env.PRINT_MODE === 'windows' ? 'windows' : 'file',
    printerName: process.env.PRINTER_NAME ?? '', allowLocalIngest: process.env.ALLOW_LOCAL_INGEST === '1',
    requireArt: process.env.REQUIRE_TICKET_ART === '1',
  };
}
export function createDeliveryService(options: {
  db: DatabaseSync; dataDir: string; config?: DeliveryConfig;
  now?: () => number; fetcher?: typeof fetch; printer?: typeof printTicket;
}) {
  const { db, dataDir } = options; const config = options.config ?? deliveryConfig();
  const now = options.now ?? Date.now; const request = options.fetcher ?? fetch; const print = options.printer ?? printTicket;
  if (!config.resultsBase) throw new Error('Set RESULTS_BASE_URL before enabling delivery');
  db.exec(`PRAGMA foreign_keys=ON; PRAGMA busy_timeout=3000;
    CREATE TABLE IF NOT EXISTS ticket_files(result_id TEXT PRIMARY KEY REFERENCES results(result_id),
      png_file TEXT NOT NULL, svg_file TEXT NOT NULL, qr_url TEXT NOT NULL, layout_version TEXT NOT NULL);
    CREATE TABLE IF NOT EXISTS ticket_handoffs(result_id TEXT PRIMARY KEY REFERENCES results(result_id),
      mode TEXT NOT NULL, delivered_at INTEGER NOT NULL);
    CREATE TABLE IF NOT EXISTS upload_retries(result_id TEXT PRIMARY KEY REFERENCES results(result_id),
      attempts INTEGER NOT NULL DEFAULT 0, next_at INTEGER NOT NULL DEFAULT 0, last_error TEXT NOT NULL DEFAULT '');
    CREATE TABLE IF NOT EXISTS print_attempts(attempt_id TEXT PRIMARY KEY, result_id TEXT NOT NULL REFERENCES results(result_id),
      kind TEXT NOT NULL, status TEXT NOT NULL, created_at INTEGER NOT NULL);`);
  let timer: ReturnType<typeof setInterval> | undefined; let busy = false;
  function saved(id: string): Saved {
    const row = db.prepare('SELECT result_id,session_id,payload_json FROM results WHERE result_id=?').get(id) as Saved | undefined;
    if (!row) throw new Error('Result not saved locally');
    return row;
  }
  function files(id: string): FileRow | undefined {
    return db.prepare('SELECT * FROM ticket_files WHERE result_id=?').get(id) as FileRow | undefined;
  }
  function filesExist(row: FileRow | undefined): row is FileRow {
    return !!row && existsSync(row.png_file) && existsSync(row.svg_file);
  }
  async function ensureTicket(id: string): Promise<FileRow> {
    const previous = files(id);
    if (previous) {
      if (!filesExist(previous)) throw new Error('Restore the saved ticket files before continuing');
      db.prepare("UPDATE print_jobs SET status='ready_file' WHERE result_id=? AND status='file_error'").run(id);
      return previous;
    }
    const payload = JSON.parse(saved(id).payload_json) as TicketResult;
    const ticket: TicketFiles = await renderTicket(payload, dataDir, config.resultsBase, config.requireArt);
    db.exec('BEGIN IMMEDIATE');
    try {
      db.prepare('INSERT INTO ticket_files VALUES(?,?,?,?,?)').run(id, ticket.png, ticket.svg, ticket.qrUrl, ticket.layout);
      db.prepare("UPDATE print_jobs SET status='ready_file' WHERE result_id=? AND status IN ('pending','file_error')").run(id);
      db.exec('COMMIT');
    } catch (error) { db.exec('ROLLBACK'); throw error; }
    return files(id)!;
  }
  async function submitPrint(id: string, attempt: string, kind: 'automatic' | 'manual'): Promise<void> {
    const ticket = await ensureTicket(id);
    if (db.prepare('SELECT attempt_id FROM print_attempts WHERE attempt_id=?').get(attempt)) return;
    db.exec('BEGIN IMMEDIATE');
    try {
      const state = db.prepare('SELECT status FROM print_jobs WHERE result_id=?').get(id) as StatusRow | undefined;
      if (kind === 'automatic' && state?.status !== 'ready_file') { db.exec('ROLLBACK'); return; }
      if (kind === 'manual' && !['unknown','sent_to_spool','delivered'].includes(state?.status ?? ''))
        throw new Error('Wait for the automatic print attempt before requesting a reprint');
      db.prepare('INSERT INTO print_attempts VALUES(?,?,?,?,?)').run(attempt, id, kind, 'submitting', now());
      db.prepare("UPDATE print_jobs SET status='submitting' WHERE result_id=?").run(id);
      db.exec('COMMIT');
    } catch (error) { db.exec('ROLLBACK'); throw error; }
    try {
      await print(ticket.png_file, config.printerName);
      db.prepare("UPDATE print_attempts SET status='sent_to_spool' WHERE attempt_id=?").run(attempt);
      db.prepare("UPDATE print_jobs SET status='sent_to_spool' WHERE result_id=?").run(id);
    } catch (error) {
      // 可能已送進 Windows，狀態不明時不自動重印。
      db.prepare("UPDATE print_attempts SET status='unknown' WHERE attempt_id=?").run(attempt);
      db.prepare("UPDATE print_jobs SET status='unknown' WHERE result_id=?").run(id);
      throw error;
    }
  }
  async function upload(row: Saved): Promise<void> {
    if (!config.ingestUrl || !config.ingestSecret) return;
    const prior = db.prepare('SELECT attempts,next_at FROM upload_retries WHERE result_id=?').get(row.result_id) as { attempts: number; next_at: number } | undefined;
    if (prior && prior.next_at > now()) return;
    const attempts = Number(prior?.attempts ?? 0) + 1;
    let blocked = false; let reason = 'Network error';
    try {
      const url = new URL(config.ingestUrl);
      const local = config.allowLocalIngest && ['localhost', '127.0.0.1', '[::1]'].includes(url.hostname) && url.protocol === 'http:';
      if ((!local && url.protocol !== 'https:') || url.username || url.password) { blocked = true; throw new Error('INGEST_URL must use HTTPS; loopback HTTP is prototype only'); }
      const response = await request(url, {
        method: 'POST', headers: { Authorization: 'Bearer ' + config.ingestSecret, 'Content-Type': 'application/json' },
        body: JSON.stringify({ schema_version: 1, session_id: row.session_id, payload: JSON.parse(row.payload_json) }),
        signal: AbortSignal.timeout(5000),
      });
      if (!response.ok) {
        blocked = response.status >= 400 && response.status < 500 && ![408, 429].includes(response.status);
        throw new Error('Upload HTTP ' + response.status);
      }
      const ack = await response.json() as { result_id?: string; status?: string };
      if (ack.result_id !== row.result_id || ack.status !== 'saved') { blocked = true; throw new Error('Upload receipt mismatch'); }
      db.prepare("UPDATE outbox SET status='uploaded' WHERE result_id=?").run(row.result_id);
      db.prepare('INSERT INTO upload_retries VALUES(?,?,?,?) ON CONFLICT(result_id) DO UPDATE SET attempts=excluded.attempts,next_at=0,last_error=\'\'')
        .run(row.result_id, attempts, 0, '');
      return;
    } catch (error) { reason = error instanceof Error && !error.message.includes('fetch') ? error.message.slice(0, 140) : 'Network error'; }
    db.prepare('INSERT INTO upload_retries VALUES(?,?,?,?) ON CONFLICT(result_id) DO UPDATE SET attempts=excluded.attempts,next_at=excluded.next_at,last_error=excluded.last_error')
      .run(row.result_id, attempts, now() + Math.min(30000, 1000 * 2 ** Math.min(attempts, 5)), reason);
    if (blocked) db.prepare("UPDATE outbox SET status='blocked' WHERE result_id=?").run(row.result_id);
  }
  function getStatus(id: string) {
    const result = db.prepare('SELECT result_id FROM results WHERE result_id=?').get(id);
    if (!result) return null;
    const ticket = files(id);
    const handoff = db.prepare('SELECT mode FROM ticket_handoffs WHERE result_id=?').get(id) as { mode: string } | undefined;
    const printJob = db.prepare('SELECT status FROM print_jobs WHERE result_id=?').get(id) as StatusRow | undefined;
    const outbox = db.prepare('SELECT status FROM outbox WHERE result_id=?').get(id) as StatusRow | undefined;
    const retry = db.prepare('SELECT attempts,last_error FROM upload_retries WHERE result_id=?').get(id);
    return { result_id: id, ticket_ready: filesExist(ticket), delivered: !!handoff, delivery_mode: handoff?.mode ?? '',
      print_status: printJob?.status ?? '', upload_status: outbox?.status ?? '',
      png_file: ticket?.png_file ?? '', upload_attempts: Number(retry?.attempts ?? 0), last_upload_error: retry?.last_error ?? '' };
  }
  async function processOnce(): Promise<void> {
    if (busy) return;
    busy = true;
    try {
      const jobs = db.prepare("SELECT result_id FROM print_jobs WHERE status IN ('pending','ready_file','file_error') LIMIT 10").all() as Array<{ result_id: string }>;
      for (const job of jobs) {
        try { await ensureTicket(job.result_id); }
        catch { db.prepare("UPDATE print_jobs SET status='file_error' WHERE result_id=?").run(job.result_id); continue; }
        if (config.printMode === 'windows') {
          try { await submitPrint(job.result_id, job.result_id + ':automatic', 'automatic'); }
          catch { /* unknown 已保存；由工作人員確認後處理。 */ }
        }
      }
      const rows = db.prepare("SELECT r.result_id,r.session_id,r.payload_json FROM results r JOIN outbox o USING(result_id) WHERE o.status='pending' LIMIT 10").all() as Saved[];
      for (const row of rows) await upload(row);
    } finally { busy = false; }
  }
  function confirmHandoff(id: string, mode: 'simulated' | 'printed') {
    saved(id);
    if (!filesExist(files(id))) throw new Error('Ticket is not ready');
    const existing = db.prepare('SELECT mode FROM ticket_handoffs WHERE result_id=?').get(id) as { mode: string } | undefined;
    if (existing) { if (existing.mode !== mode) throw new Error('Handoff mode conflict'); return getStatus(id); }
    if (mode === 'simulated' && config.printMode !== 'file') throw new Error('Simulated handoff requires PRINT_MODE=file');
    const job = db.prepare('SELECT status FROM print_jobs WHERE result_id=?').get(id) as StatusRow | undefined;
    if (mode === 'printed' && job?.status !== 'sent_to_spool') throw new Error('Confirm actual output after a sent_to_spool job');
    db.exec('BEGIN IMMEDIATE');
    try {
      db.prepare('INSERT INTO ticket_handoffs VALUES(?,?,?)').run(id, mode, now());
      db.prepare("UPDATE print_jobs SET status='delivered' WHERE result_id=?").run(id);
      db.exec('COMMIT');
    } catch (error) { db.exec('ROLLBACK'); throw error; }
    return getStatus(id);
  }
  return {
    getStatus, processOnce, confirmHandoff,
    list: () => (db.prepare('SELECT result_id FROM results').all() as Array<{ result_id: string }>).map(r => getStatus(r.result_id)),
    retryUpload: (id: string) => { saved(id); db.prepare("UPDATE outbox SET status='pending' WHERE result_id=?").run(id); db.prepare('DELETE FROM upload_retries WHERE result_id=?').run(id); },
    reprint: async (id: string) => {
      if (config.printMode !== 'windows') throw new Error('Set PRINT_MODE=windows to print; file mode only generates ticket files');
      if (busy) throw new Error('Delivery worker is busy; retry after it finishes');
      await submitPrint(id, randomUUID(), 'manual');
      return getStatus(id);
    },
    start: () => {
      if (timer) return;
      db.prepare("UPDATE print_jobs SET status='unknown' WHERE status='submitting'").run();
      db.prepare("UPDATE print_attempts SET status='unknown' WHERE status='submitting'").run();
      timer = setInterval(() => { void processOnce().catch(() => console.error('Delivery worker error; check local jobs')); }, 1000);
      timer.unref(); void processOnce().catch(() => console.error('Delivery worker error; check local jobs'));
    },
    stop: () => { if (timer) clearInterval(timer); timer = undefined; },
  };
}
