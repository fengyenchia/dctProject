'use client';
import { useState } from 'react';
export default function ResultIcon({ url, name, code }: { url: string; name: string; code: string }) {
  const [failed, setFailed] = useState(false);
  return failed ? <div className="bg-slate-100 p-5 text-center">{code}（圖像尚未提供）</div>
    : <img src={url} alt={name} width={160} height={160} onError={() => setFailed(true)} className="mx-auto h-28 w-28 object-contain" />;
}
