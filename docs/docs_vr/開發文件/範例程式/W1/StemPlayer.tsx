'use client';

import { useEffect, useRef, useState } from 'react';

export function StemPlayer({ urls }: { urls: string[] }) {
  const context = useRef<AudioContext | null>(null);
  const abort = useRef<AbortController | null>(null);
  const started = useRef(false);
  const busy = useRef(false);
  const [state, setState] = useState('idle');
  const [message, setMessage] = useState('');

  useEffect(() => () => {
    abort.current?.abort();
    void context.current?.close().catch(() => {});
  }, []);

  async function toggle() {
    if (busy.current) return;
    busy.current = true;
    try {
      if (!context.current) context.current = new AudioContext();
      const ctx = context.current;
      if (started.current) {
        if (ctx.state === 'running') { await ctx.suspend(); setState('paused'); }
        else { await ctx.resume(); setState('playing'); }
        return;
      }
      // 在觀眾點擊時先啟用音訊，再非同步讀檔。
      await ctx.resume();
      setState('loading'); setMessage('');
      const controller = new AbortController(); abort.current = controller;
      const buffers = await Promise.all(urls.map(async url => {
        const response = await fetch(url, { signal: controller.signal });
        if (!response.ok) throw new Error('缺少聲部素材');
        return ctx.decodeAudioData(await response.arrayBuffer());
      }));
      if (controller.signal.aborted) return;
      if (!buffers.length || buffers.some(b => Math.abs(b.duration - buffers[0].duration) > .02)) throw new Error('聲部長度不一致');
      const gain = ctx.createGain(); gain.gain.value = .2; gain.connect(ctx.destination);
      const when = ctx.currentTime + .1;
      buffers.forEach(buffer => {
        const source = ctx.createBufferSource(); source.buffer = buffer;
        source.loop = true; source.connect(gain); source.start(when);
      });
      started.current = true; setState('playing');
    } catch (error) {
      if (abort.current?.signal.aborted) return;
      await context.current?.close().catch(() => {});
      context.current = null; started.current = false;
      setState('error'); setMessage(error instanceof Error ? error.message : '音訊載入失敗');
    } finally { busy.current = false; }
  }

  return <section className="space-y-2">
    <button className="rounded bg-slate-800 px-5 py-3 text-white disabled:opacity-50" disabled={state === 'loading'} onClick={() => void toggle()}>
      {state === 'loading' ? '載入中' : state === 'playing' ? '暫停旋律' : '播放旋律'}
    </button>
    {message && <p role="alert">{message}，請確認音檔後重試。</p>}
  </section>;
}
