import demo from './result-ready.example.json';
import type { Result } from '@/lib/api/results/types';

// 僅由開發模式的資料層使用；每張卡片各有自己的成果，正式站不回退至樣本。
export function demoResults(): Result[] {
  const occurrences = new Map<number, number>();
  return [5, 3, 1].map(count => {
    const variants = demo.payload.variants.slice(0, count);
    const occurrence = (occurrences.get(count) ?? 0) + 1;
    occurrences.set(count, occurrence);
    // 數量可重複；重複範例的編號與連結仍須獨立。
    const suffix = occurrence > 1 ? `_copy${occurrence}` : '';
    return {
      ...demo.payload,
      result_id: `${demo.payload.result_id}_${count}${suffix}`,
      token: `${count === 5 ? demo.payload.token : `${demo.payload.token}_${count}`}${suffix}`,
      capture_count: count,
      san: 100 - count * 20,
      variants,
      melody: { ...demo.payload.melody, stem_ids: variants.map(item => item.stem_id) },
    };
  });
}
