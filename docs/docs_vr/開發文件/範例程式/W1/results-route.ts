import { loadResult } from '@/lib/api/results/server';
export const runtime = 'nodejs';
export const dynamic = 'force-dynamic';
export async function GET(_request: Request, context: { params: Promise<{ token: string }> }) {
  const { token } = await context.params;
  const result = await loadResult(token);
  return Response.json(result ?? { error: 'not_ready_or_invalid' }, {
    status: result ? 200 : 404, headers: { 'Cache-Control': 'no-store' },
  });
}
