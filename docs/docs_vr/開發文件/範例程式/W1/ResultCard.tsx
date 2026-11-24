import Link from 'next/link';
import type { ResultCardData } from '@/lib/api/results/types';
import ResultIcon from '@/components/common/ResultIcon';
import { Card } from '@/components/ui/card';

export default function ResultCard({ result }: { result: ResultCardData }) {
  const cover = result.variants[0];
  return <li>
    <Link href={`/results/${encodeURIComponent(result.token)}`} prefetch={false}
      className="block h-full" aria-label={`查看成果 ${result.result_id}`}>
      <Card className="result-card">
      <div className="result-cover" aria-hidden="true">
        <ResultIcon
          url={cover ? `/assets/${encodeURIComponent(result.asset_version)}/images/${encodeURIComponent(cover.image_id)}.png` : ''}
          name={cover?.name ?? '成果封面'} code={result.result_id} variant="cover" />
      </div>
      <h2 className="result-number">{result.result_id}</h2>
      </Card>
    </Link>
  </li>;
}
