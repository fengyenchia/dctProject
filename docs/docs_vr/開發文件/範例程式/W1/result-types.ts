export type Result = {
  result_id: string;
  token: string;
  asset_version: string;
  variants: Array<{ variant_id: string; name: string; source_label: string; image_id: string; stem_id?: string }>;
  melody: { stem_ids: string[] };
};
