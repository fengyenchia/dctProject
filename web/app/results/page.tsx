import Link from "next/link";
import { loadGallery } from "@/lib/api/results/server";
import { parseGalleryPage } from "@/lib/api/results/types";
import ResultCard from "@/components/results/ResultCard";
import ResultPagination from "@/components/results/ResultPagination";

export const dynamic = "force-dynamic";
export const metadata = { title: "Results" };

export default async function GalleryPage({
  searchParams,
}: {
  searchParams: Promise<{ page?: string | string[] }>;
}) {
  const query = await searchParams;
  const page = Array.isArray(query.page) ? null : parseGalleryPage(query.page);
  if (page === null)
    return (
      <main className="page-shell">
        <h1 className="title">頁碼無效</h1>
        <Link className="text-link" href="/results">
          回成果總覽
        </Link>
      </main>
    );
  const gallery = await loadGallery(page);
  return (
    <main className="page-shell">
      {gallery.source === "unavailable" ? (
        <section className="content-panel" role="status">
          <p>成果暫時無法載入，請稍後重試。</p>
          <a className="text-link" href={`/results?page=${page}`}>
            重新載入
          </a>
        </section>
      ) : (
        <>
          {!gallery.items.length && (
            <p>
              {page === 1 ? "目前還沒有已發布的成果。" : "這一頁沒有成果。"}
            </p>
          )}
          <ul className="result-grid">
            {gallery.items.map((result) => (
              <ResultCard key={result.result_id} result={result} />
            ))}
          </ul>
          <ResultPagination page={page} hasMore={gallery.hasMore} />
        </>
      )}
    </main>
  );
}
