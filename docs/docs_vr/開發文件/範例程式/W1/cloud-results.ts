import 'server-only';
import { neon } from '@neondatabase/serverless';
import { createHash } from 'node:crypto';
import type { Result } from './types';
export function isResult(value: unknown, token?: string): value is Result {
  if (!value || typeof value !== 'object') return false;
  const p = value as Result & Record<string, unknown>;
  return typeof p.result_id === 'string' && /^[a-zA-Z0-9_-]{1,100}$/.test(p.result_id)
    && typeof p.token === 'string' && /^[a-f0-9]{64}$/.test(p.token) && (!token || p.token === token)
    && p.san === 0 && p.capture_count === 5 && p.recycled === true
    && typeof p.asset_version === 'string' && /^[a-zA-Z0-9_-]+$/.test(p.asset_version)
    && typeof p.recipe_version === 'string' && p.recipe_version.length > 0
    && Array.isArray(p.variants) && p.variants.length === 5 && new Set(p.variants.map(v => v?.variant_id)).size === 5
    && p.variants.every(v => v && /^[ABC][123]$/.test(v.variant_id) && v.image_id === v.variant_id
      && v.stem_id === `stem-${v.variant_id}` && typeof v.name === 'string' && v.name.length <= 100
      && typeof v.source_label === 'string' && v.source_label.length <= 200)
    && Array.isArray(p.melody?.stem_ids) && p.melody.stem_ids.length === 5
    && p.melody.stem_ids.every((id, i) => id === `stem-${p.variants[i].variant_id}`);
}
function canonical(value: unknown): string {
  if (Array.isArray(value)) return '[' + value.map(canonical).join(',') + ']';
  if (value && typeof value === 'object') return '{' + Object.entries(value).sort(([a], [b]) => a.localeCompare(b))
    .map(([k, v]) => JSON.stringify(k) + ':' + canonical(v)).join(',') + '}';
  return JSON.stringify(value) ?? 'null';
}
function sql() {
  if (!process.env.DATABASE_URL) throw new Error('DATABASE_URL missing');
  return neon(process.env.DATABASE_URL);
}
export async function saveCloudResult(session: string, payload: Result): Promise<boolean> {
  const hash = createHash('sha256').update(canonical(payload)).digest('hex');
  const rows = await sql()`INSERT INTO results(result_id,session_id,token,payload,payload_hash)
    VALUES(${payload.result_id},${session},${payload.token},${JSON.stringify(payload)}::jsonb,${hash})
    ON CONFLICT(result_id) DO UPDATE SET payload_hash=results.payload_hash
    WHERE results.payload_hash=EXCLUDED.payload_hash AND results.session_id=EXCLUDED.session_id
    RETURNING result_id`;
  return rows.length === 1;
}
export async function readCloudResult(token: string): Promise<Result | null> {
  const rows = await sql()`SELECT payload FROM results WHERE token=${token} LIMIT 1`;
  return isResult(rows[0]?.payload, token) ? rows[0].payload : null;
}
