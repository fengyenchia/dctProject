'use client';
import { useState } from 'react';
export default function ResultTools() {
  const [status, setStatus] = useState('');
  async function copy() { try { await navigator.clipboard.writeText(location.href); setStatus('已複製連結'); } catch { setStatus('請複製瀏覽器網址'); } }
  async function share() { try { if (navigator.share) await navigator.share({ title: '聲音收藏紀錄', url: location.href }); else await copy(); } catch { setStatus('可改用複製連結'); } }
  return <div className="space-x-4"><button onClick={() => void copy()}>複製連結</button><button onClick={() => void share()}>分享</button><span role="status">{status}</span></div>;
}
