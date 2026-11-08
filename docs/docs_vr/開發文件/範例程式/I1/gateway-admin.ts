import type { IncomingMessage, ServerResponse } from 'node:http';
import type { DatabaseSync } from 'node:sqlite';
import type { WebSocket } from 'ws';
type Delivery = { list(): unknown[]; confirmHandoff(id: string, mode: 'printed'): unknown; reprint(id: string): Promise<unknown> };
export function createAdmin(options: { db: DatabaseSync; key: string; unity(): { socket?: WebSocket; session?: string }; delivery(): Delivery | undefined }) {
  const { db } = options; let snapshot: unknown = null, lastSeen = 0;
  db.exec(`CREATE TABLE IF NOT EXISTS commands(command_id TEXT PRIMARY KEY, session_id TEXT NOT NULL,
    type TEXT NOT NULL, status TEXT NOT NULL, receipt TEXT, created_at INTEGER NOT NULL)`);
  function receive(value: { type?: string; session_id?: string; command_id?: string; payload?: unknown }, boundSession: string) {
    if (value.session_id !== boundSession) return false;
    if (value.type === 'STATE_SNAPSHOT') { snapshot = value.payload; lastSeen = Date.now(); return true; }
    if (value.type === 'COMMAND_ACK' && typeof value.command_id === 'string') {
      db.prepare("UPDATE commands SET status='acknowledged',receipt=? WHERE command_id=? AND session_id=?")
        .run(JSON.stringify(value.payload), value.command_id, boundSession); return true;
    }
    return false;
  }
  async function handle(request: IncomingMessage, response: ServerResponse): Promise<boolean> {
    const path = new URL(request.url ?? '/', 'http://localhost').pathname;
    if (!['/state','/commands'].includes(path)) return false;
    const reply = (status: number, body: unknown) => { response.writeHead(status).end(JSON.stringify(body)); };
    if (request.headers.authorization !== 'Bearer ' + options.key) { reply(401, {}); return true; }
    const unity = options.unity();
    if (path === '/state' && request.method === 'GET') {
      db.prepare("UPDATE commands SET status='unknown' WHERE status='pending' AND created_at<?").run(Date.now() - 10000);
      reply(200, { connected: unity.socket?.readyState === 1 && Date.now() - lastSeen < 5000,
        session_id: unity.session, snapshot: Date.now() - lastSeen < 5000 ? snapshot : null,
        commands: db.prepare('SELECT command_id,type,status,receipt FROM commands ORDER BY created_at DESC LIMIT 10').all(),
        deliveries: options.delivery()?.list() ?? [] }); return true;
    }
    if (path !== '/commands' || request.method !== 'POST') { reply(405, {}); return true; }
    try {
      let text = ''; for await (const chunk of request) { text += chunk; if (text.length > 4096) throw new Error('Too large'); }
      const command = JSON.parse(text);
      if (!['SESSION_START','PAUSE','RESUME','CALIBRATE','ABORT','RESET','REPRINT','HANDOFF_PRINTED'].includes(command.type)
        || typeof command.command_id !== 'string' || !/^[a-zA-Z0-9_-]{1,100}$/.test(command.command_id)) { reply(400, {}); return true; }
      const prior = db.prepare('SELECT * FROM commands WHERE command_id=?').get(command.command_id);
      if (prior) { reply(prior.type === command.type && prior.session_id === (command.session_id ?? '') ? 200 : 409, prior); return true; }
      if (['REPRINT','HANDOFF_PRINTED'].includes(command.type)) {
        const delivery = options.delivery(); if (!delivery || typeof command.result_id !== 'string') { reply(409, {}); return true; }
        // 管理票務也要求人工確認；原始自動列印工作不改成重算遊戲成果。
        if (command.type === 'REPRINT') await delivery.reprint(command.result_id);
        else delivery.confirmHandoff(command.result_id, 'printed');
        reply(200, { status: 'done' }); return true;
      }
      if (!unity.session || command.session_id !== unity.session || unity.socket?.readyState !== 1 || Date.now() - lastSeen >= 5000) {
        reply(409, { error: 'unity_offline_or_stale_session' }); return true;
      }
      db.prepare('INSERT INTO commands VALUES(?,?,?,?,?,?)').run(command.command_id, unity.session, command.type, 'pending', null, Date.now());
      unity.socket.send(JSON.stringify({ ...command, type: 'COMMAND', command: command.type }));
      reply(202, { command_id: command.command_id, status: 'pending' });
    } catch { reply(409, { error: 'command_failed_check_state' }); }
    return true;
  }
  return { receive, handle };
}
