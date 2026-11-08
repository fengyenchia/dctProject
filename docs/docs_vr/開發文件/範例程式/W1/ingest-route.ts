import { timingSafeEqual } from 'node:crypto';
import { isResult, saveCloudResult } from '@/lib/api/results/cloud';
export const runtime = 'nodejs';
export const dynamic = 'force-dynamic';
export async function POST(request: Request) {
  const secret = process.env.INGEST_SECRET ?? '';
  const expected = Buffer.from('Bearer ' + secret), actual = Buffer.from(request.headers.get('authorization') ?? '');
  if (secret.length < 24) return Response.json({ error: 'not_configured' }, { status: 503 });
  if (actual.length !== expected.length || !timingSafeEqual(actual, expected))
    return Response.json({ error: 'unauthorized' }, { status: 401 });
  try {
    const text = await request.text();
    if (Buffer.byteLength(text) > 65536) return Response.json({ error: 'too_large' }, { status: 413 });
    const body = JSON.parse(text);
    if (body.schema_version !== 1 || typeof body.session_id !== 'string'
      || !/^[a-zA-Z0-9_-]{1,100}$/.test(body.session_id) || !isResult(body.payload))
      return Response.json({ error: 'invalid_result' }, { status: 400 });
    try {
      if (!await saveCloudResult(body.session_id, body.payload)) return Response.json({ error: 'conflict' }, { status: 409 });
    } catch (error) {
      if ((error as { code?: string }).code === '23505') return Response.json({ error: 'conflict' }, { status: 409 });
      throw error;
    }
    return Response.json({ result_id: body.payload.result_id, status: 'saved' });
  } catch (error) {
    const status = error instanceof SyntaxError ? 400 : 503;
    return Response.json({ error: status === 400 ? 'invalid_json' : 'save_unavailable' }, { status });
  }
}
