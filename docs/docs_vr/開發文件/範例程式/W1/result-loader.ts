import 'server-only';
import demo from '@/lib/fixtures/result-ready.example.json';
import type { Result } from './types';
import { readCloudResult, isResult } from './cloud';

export async function loadResult(token: string): Promise<Result | null> {
  if (process.env.NODE_ENV !== 'production' && token === demo.payload.token) return demo.payload;
  if (!/^[a-f0-9]{64}$/.test(token)) return null;
  try {
    if (process.env.DATABASE_URL) return await readCloudResult(token);
    if (!process.env.GATEWAY_HTTP_BASE) return null;
    const response = await fetch(`${process.env.GATEWAY_HTTP_BASE}/results/${encodeURIComponent(token)}`, { cache: 'no-store' });
    if (!response.ok) return null;
    const value = await response.json() as Result;
    if (!value || typeof value.result_id !== 'string' || value.token !== token || typeof value.asset_version !== 'string'
      || !/^[a-zA-Z0-9_-]+$/.test(value.asset_version)
      || !Array.isArray(value.variants) || value.variants.length !== 5 || !Array.isArray(value.melody?.stem_ids)
      || value.melody.stem_ids.length !== 5
      || !value.variants.every(item => item && /^[ABC][123]$/.test(item.variant_id)
        && typeof item.name === 'string' && typeof item.source_label === 'string' && typeof item.image_id === 'string')
      || new Set(value.variants.map(item => item.variant_id)).size !== 5
      || !value.melody.stem_ids.every((stem, index) => stem === `stem-${value.variants[index].variant_id}`)) return null;
    return isResult(value, token) ? value : null;
  } catch { return null; }
}
