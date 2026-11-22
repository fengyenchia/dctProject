import { loadGallery } from '@/lib/api/results/server';
import { parseGalleryPage } from '@/lib/api/results/types';

export const runtime = 'nodejs';
export const dynamic = 'force-dynamic';
export async function GET(request: Request) {
  const params = new URL(request.url).searchParams;
  const page = params.getAll('page').length > 1 ? null : parseGalleryPage(params.get('page'));
  const headers = { 'Cache-Control': 'no-store' };
  if (page === null) return Response.json({ error: 'invalid_page' }, { status: 400, headers });
  const gallery = await loadGallery(page);
  return Response.json(gallery, { status: gallery.source === 'unavailable' ? 503 : 200, headers });
}
