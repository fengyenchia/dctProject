import Link from 'next/link';
import type { ResultCardData } from '@/lib/api/results/types';
import ResultIcon from './ResultIcon';

export default function ResultCard({ result }: { result: ResultCardData }) {
  return <li>
    <Link href={`/results/${encodeURIComponent(result.token)}`} prefetch={false}
      className="block h-full space-y-4 rounded-xl border p-4 hover:bg-slate-50 focus-visible:outline-2">
      <div className="flex flex-wrap gap-2" aria-hidden="true">
        {result.variants.map(item => <div key={item.variant_id}>
          <ResultIcon url={`/assets/${encodeURIComponent(result.asset_version)}/images/${encodeURIComponent(item.image_id)}.png`}
            name={item.name} code={item.variant_id} />
        </div>)}
      </div>
      <h2 className="font-semibold">{result.variants.map(item => item.name).join('・')}</h2>
      <p>查看這份成果 →</p>
    </Link>
  </li>;
}
