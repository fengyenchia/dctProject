import { loadResult } from "@/lib/api/results/server";
import ResultIcon from "@/components/common/ResultIcon";
import Link from "next/link";

export const dynamic = 'force-dynamic';
export const metadata = { title: 'Result' };

export default async function ResultPage({
  params,
}: {
  params: Promise<{ token: string }>;
}) {
  const { token } = await params;
  const result = await loadResult(token);
  if (!result)
    return (
      <main className="page-shell page-shell-narrow">
        <h1 className="title">成果尚未就緒或連結無效</h1>
        <p className="page-description">請稍後重新整理。</p>
        <Link className="text-link" href="/results">看看其他人的成果</Link>
      </main>
    );
  const count = result.variants.length;
  // 暫用文案，正式版與 VR 內三個版本共用企劃交付的內容。
  const letter = count === 1 ? "你帶回了第一份聲音。謝謝你為城市修復出力。"
    : count <= 3 ? "幾處街角已開始恢復。謝謝你帶回這些聲音。"
    : "這些聲音讓更多街角得以修復。謝謝你一路探索。";
  return (
    <main className="page-shell page-shell-narrow">
      <h1 className="title">{result.result_id} 的成果</h1>
      <p className="page-description">{letter}</p>
      <ul className="grid grid-cols-2 gap-3">
        {result.variants.map((item) => (
          <li className="content-panel" key={item.variant_id}>
            <ResultIcon url={`/assets/${encodeURIComponent(result.asset_version)}/images/${encodeURIComponent(item.image_id)}.png`} name={item.name} code={item.variant_id} />
            <h2>{item.name}</h2>
            <p className="page-note">{item.source_label}</p>
          </li>
        ))}
      </ul>
      <p className="page-note">
        本場共收藏 {count} 個變體，已完成城市修復。請保存此連結；服務保留期間依團隊公告。
      </p>
    </main>
  );
}
