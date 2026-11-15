import { loadResult } from "@/lib/api/results/server";
import { StemPlayer } from "@/components/features/StemPlayer";
import ResultTools from "@/components/features/ResultTools";
import ResultIcon from "@/components/features/ResultIcon";

export default async function ResultPage({
  params,
}: {
  params: Promise<{ token: string }>;
}) {
  const { token } = await params;
  const result = await loadResult(token);
  if (!result)
    return (
      <main className="mx-auto max-w-xl p-6">
        <h1>成果尚未就緒或連結無效</h1>
        <p>請稍後重新整理。</p>
      </main>
    );
  const urls = result.melody.stem_ids.map(
    (id) =>
      `/assets/${encodeURIComponent(result.asset_version)}/stems/${encodeURIComponent(id)}.wav`,
  );
  const count = result.variants.length;
  // 暫用文案，正式版與 VR 內三個版本共用企劃交付的內容。
  const letter = count === 1 ? "你帶回了第一份聲音。謝謝你為城市修復出力。"
    : count <= 3 ? "幾處街角已開始恢復。謝謝你帶回這些聲音。"
    : "這些聲音讓更多街角得以修復。謝謝你一路探索。";
  return (
    <main className="mx-auto max-w-xl space-y-6 p-6">
      <h1 className="text-2xl font-bold">聲音收藏紀錄</h1>
      <p>{letter}</p>
      <ul className="grid grid-cols-2 gap-3">
        {result.variants.map((item) => (
          <li className="rounded border p-4" key={item.variant_id}>
            <ResultIcon url={`/assets/${encodeURIComponent(result.asset_version)}/images/${encodeURIComponent(item.image_id)}.png`} name={item.name} code={item.variant_id} />
            <h2>{item.name}</h2>
            <p className="text-sm">{item.source_label}</p>
          </li>
        ))}
      </ul>
      <StemPlayer key={result.result_id} urls={urls} />
      <ResultTools />
      <p className="text-sm text-slate-500">
        本場共收藏 {count} 個變體，已完成城市修復。請保存此連結；服務保留期間依團隊公告。
      </p>
    </main>
  );
}
