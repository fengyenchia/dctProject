import Link from 'next/link';
import { loadGallery } from '@/lib/api/results/server';
import { parseGalleryPage } from '@/lib/api/results/types';
import ResultCard from '@/components/features/ResultCard';

export const dynamic = 'force-dynamic';

export default async function GalleryPage({ searchParams }: {
  searchParams: Promise<{ page?: string | string[] }>;
}) {
  const query = await searchParams;
  const page = Array.isArray(query.page) ? null : parseGalleryPage(query.page);
  if (page === null) return <main className="mx-auto max-w-5xl space-y-4 p-6">
    <h1>頁碼無效</h1><Link href="/results">回成果總覽</Link>
  </main>;
  const gallery = await loadGallery(page);
  return <main className="mx-auto max-w-5xl space-y-6 p-6">
    <h1 className="text-2xl font-bold">成果總覽</h1>
    <p>點開卡片，看看每位觀眾留下的成果。</p>
    {gallery.source === 'demo' && <p role="status">目前為本機示例；頁面內容與視覺待團隊定稿。</p>}
    {gallery.source === 'unavailable'
      ? <section role="status"><p>成果暫時無法載入，請稍後重試。</p><a href={`/results?page=${page}`}>重新載入</a></section>
      : <>
        {!gallery.items.length && <p>{page === 1 ? '目前還沒有已發布的成果。' : '這一頁沒有成果。'}</p>}
        <ul className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {gallery.items.map(result => <ResultCard key={result.result_id} result={result} />)}
        </ul>
        <nav aria-label="成果分頁" className="flex items-center gap-4">
          {page > 1 && <Link href={`/results?page=${page - 1}`}>上一頁</Link>}
          <span>第 {page} 頁</span>
          {gallery.hasMore && <Link href={`/results?page=${page + 1}`}>下一頁</Link>}
        </nav>
      </>}
  </main>;
}
