import 'server-only';
import { demoResults } from '@/lib/fixtures/demo-results';
import { GALLERY_PAGE_SIZE, toResultCard, type Result, type ResultGallery } from './types';
import { readCloudResult, readCloudGallery, isResult } from './cloud';

export async function loadResult(token: string): Promise<Result | null> {
  if (process.env.NODE_ENV !== 'production') {
    const sample = demoResults().find(item => item.token === token);
    if (sample) return sample;
  }
  if (!/^[a-f0-9]{64}$/.test(token)) return null;
  try {
    if (process.env.DATABASE_URL) return await readCloudResult(token);
    if (!process.env.GATEWAY_HTTP_BASE) return null;
    const response = await fetch(`${process.env.GATEWAY_HTTP_BASE}/results/${encodeURIComponent(token)}`, { cache: 'no-store' });
    if (!response.ok) return null;
    const value: unknown = await response.json();
    return isResult(value, token) ? value : null;
  } catch { return null; }
}

export async function loadGallery(page: number): Promise<ResultGallery> {
  try {
    if (process.env.DATABASE_URL) return await readCloudGallery(page);
    if (process.env.NODE_ENV !== 'production') {
      const items = demoResults();
      const offset = (page - 1) * GALLERY_PAGE_SIZE;
      return {
        items: items.slice(offset, offset + GALLERY_PAGE_SIZE).map(toResultCard),
        page, hasMore: items.length > offset + GALLERY_PAGE_SIZE, source: 'demo',
      };
    }
  } catch { /* 雲端失敗顯示重試，不以樣本冒充真實成果。 */ }
  return { items: [], page, hasMore: false, source: 'unavailable' };
}
