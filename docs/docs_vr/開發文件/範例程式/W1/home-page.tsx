import Link from 'next/link';

export default function HomePage() {
  return <main className="mx-auto max-w-5xl space-y-8 p-6">
    <section className="flex min-h-80 items-center justify-center rounded-xl bg-slate-800 p-8 text-white" aria-label="主視覺預留區">
      <h1 className="text-3xl font-bold">作品主視覺</h1>
    </section>
    <p>主視覺與作品介紹由團隊定稿後替換。</p>
    <div className="flex gap-6">
      <Link className="underline" href="/results">看看遊戲成果</Link>
      <Link className="underline" href="/about">認識團隊</Link>
    </div>
  </main>;
}
