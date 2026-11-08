import { readFileSync } from 'node:fs';
import { timingSafeEqual } from 'node:crypto';
export const runtime = 'nodejs';
export const dynamic = 'force-dynamic';
export async function POST(request: Request) {
  if (process.env.ENABLE_LOCAL_ADMIN !== '1') return new Response(null, { status: 404 });
  // Next 的內部 request.url 不一定沿用瀏覽器輸入的 hostname；核對實際 Origin 與 Host。
  const origin = request.headers.get('origin');
  if (!origin) return new Response(null, { status: 403 });
  try {
    const parsed = new URL(origin);
    if (parsed.protocol !== 'http:' || !['localhost','127.0.0.1','[::1]'].includes(parsed.hostname)
      || parsed.host !== request.headers.get('host')) return new Response(null, { status: 403 });
  } catch { return new Response(null, { status: 403 }); }
  try {
    const { password, action, command_id, session_id, result_id } = await request.json();
    const expected = Buffer.from(process.env.LOCAL_ADMIN_PASSWORD ?? ''), actual = Buffer.from(typeof password === 'string' ? password : '');
    if (expected.length < 24 || actual.length !== expected.length || !timingSafeEqual(actual, expected)) return new Response(null, { status: 401 });
    const key = readFileSync(process.env.GATEWAY_KEY_FILE ?? '', 'utf8').trim();
    const body = action === 'status' ? undefined : JSON.stringify({ type: action, command_id, session_id, result_id });
    const response = await fetch('http://127.0.0.1:8788' + (body ? '/commands' : '/state'), {
      method: body ? 'POST' : 'GET', cache: 'no-store', signal: AbortSignal.timeout(5000),
      headers: { Authorization: 'Bearer ' + key, 'Content-Type': 'application/json' }, body,
    });
    return new Response(await response.text(), { status: response.status, headers: { 'Content-Type': 'application/json', 'Cache-Control': 'no-store' } });
  } catch { return Response.json({ error: 'local_service_unavailable' }, { status: 503 }); }
}
