'use client';
import { useEffect, useRef, useState } from 'react';
type State = { connected?: boolean; session_id?: string; snapshot?: Record<string, unknown>; commands?: unknown[]; deliveries?: Array<{ result_id: string; delivered: boolean; print_status: string }> };
export default function AdminPanel() {
  const [password, setPassword] = useState(''), [active, setActive] = useState(false);
  const [state, setState] = useState<State>({}), [message, setMessage] = useState('輸入工作人員密碼');
  const busy = useRef(false);
  async function call(action: string, result_id?: string) {
    if (busy.current) return; busy.current = true;
    try {
      const response = await fetch('/api/local', { method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ password, action, result_id, session_id: state.session_id, command_id: crypto.randomUUID() }) });
      if (!response.ok) throw new Error(`請求失敗 ${response.status}`);
      const value = await response.json();
      if (action === 'status') { setState(value); setActive(true); }
      setMessage(action === 'status' ? '狀態已更新' : '已送出；核對 Unity 回執及狀態');
    } catch (error) { setMessage(error instanceof Error ? error.message : '服務離線'); }
    finally { busy.current = false; }
  }
  useEffect(() => { if (!active) return; const timer = setInterval(() => void call('status'), 1500); return () => clearInterval(timer); });
  return <main className="max-w-3xl space-y-4 p-6"><h1>展場管理</h1>
    <input aria-label="工作人員密碼" type="password" value={password} onChange={e => setPassword(e.target.value)} className="border p-2" />
    <button onClick={() => void call('status')}>連線／更新</button><p>{message}</p>
    <p>Unity：{state.connected ? '連線' : '離線'}；場次：{state.session_id ?? '無'}</p>
    <div className="flex flex-wrap gap-3">{['SESSION_START','PAUSE','RESUME','CALIBRATE','ABORT','RESET'].map(action =>
      <button className="border p-2" key={action} disabled={!state.connected} onClick={() => {
        if (['ABORT','RESET','CALIBRATE'].includes(action) && !confirm(`確認 ${action}？`)) return;
        void call(action);
      }}>{action}</button>)}</div>
    <pre className="overflow-auto text-xs">{JSON.stringify(state.snapshot, null, 2)}</pre>
    <h2>最近命令回執</h2><pre className="overflow-auto text-xs">{JSON.stringify(state.commands, null, 2)}</pre>
    <h2>票務</h2>{state.deliveries?.map(item => <div key={item.result_id} className="space-x-3 border p-2">
      <span>{item.result_id}／{item.print_status}／{item.delivered ? '已交票' : '未交票'}</span>
      {['REPRINT','HANDOFF_PRINTED'].map(action => <button key={action} onClick={() => {
        if (confirm(action === 'REPRINT' ? '核對出紙後，確定補印？' : '確定已交付實體票？')) void call(action, item.result_id);
      }}>{action}</button>)}
    </div>)}</main>;
}
