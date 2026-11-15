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
    const value: unknown = await response.json();
    return isResult(value, token) ? value : null;
  } catch { return null; }
}
