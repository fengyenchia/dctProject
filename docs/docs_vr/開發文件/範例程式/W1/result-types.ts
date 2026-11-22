export type Result = {
  result_id: string;
  token: string;
  completed_at: string;
  layout_id: string;
  asset_version: string;
  recipe_version: string;
  capture_count: number;
  san: number;
  recycled: boolean;
  repair_id: string;
  variants: Array<{ variant_id: string; name: string; source_label: string; image_id: string; stem_id: string }>;
  melody: { bpm: number; beats_per_bar: number; bars: number; stem_ids: string[] };
};

// 總覽只傳公開卡片資料；不帶 session、SAN、裝置狀態或完整配方。
export type ResultCardData = Pick<Result, 'result_id' | 'token' | 'asset_version'> & {
  variants: Array<Pick<Result['variants'][number], 'variant_id' | 'name' | 'image_id'>>;
};
export type ResultGallery = {
  items: ResultCardData[];
  page: number;
  hasMore: boolean;
  source: 'demo' | 'cloud' | 'unavailable';
};
export const GALLERY_PAGE_SIZE = 12;
export function parseGalleryPage(value: string | null | undefined): number | null {
  if (value == null) return 1;
  if (!/^[1-9]\d{0,3}$/.test(value)) return null;
  const page = Number(value);
  return page <= 1000 ? page : null;
}
export function toResultCard(result: Result): ResultCardData {
  return {
    result_id: result.result_id, token: result.token, asset_version: result.asset_version,
    variants: result.variants.map(({ variant_id, name, image_id }) => ({ variant_id, name, image_id })),
  };
}
