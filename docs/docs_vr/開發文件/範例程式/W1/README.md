# W1 範例程式

本資料夾只放本工作包維護的教學來源；實際複製位置見工作包。

- 導覽：`site-layout.tsx`＋`SiteHeader.tsx`，串接 home `/`、results `/results`、about `/about`。
- 成果主線：`gallery-page.tsx`＋`ResultCard.tsx` → `result-page.tsx`。
- 紙票 QR 仍使用 `/results/<token>`，直接開該份個人成果。
- 總覽讀 `gallery-route.ts`／`loadGallery`，每頁 12 張；雲端未設定時僅開發模式可用三筆示例。
- 收藏、文字、播放器是可替換佔位內容，正式展示項目見討論清單。
- `admin-*`、`AdminPanel.tsx`、`local-admin-route.ts` 保留為後續選配，本次不要求實作。

[開始實作 W1](../../工作包/W1.md)｜[全部工作包](../README.md)
