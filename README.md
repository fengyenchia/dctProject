# 音虫 VR｜畢業製作開發專案

以聲音探索與音虫捕捉為核心的單人 VR 互動作品。觀眾扮演捕蟲人，在虛擬城市中循聲尋找音虫，回收噪音能量、修復城市，並帶走屬於這次收集的聲音紀念品。

> [!NOTE]
> 目前以 **VR 方案**為開發主線，repo 已有技術文件與教學範例；正式 Unity、網頁及 Gateway 專案尚未建立。
> 電腦可玩原型測試目標：**2027/1/31**。

## 互動方式

觀眾配戴 Quest 3，在小型活動圈內轉身、小步移動，以控制器操作虛擬捕捉器。

| 階段 | 觀眾操作與系統反應 |
| --- | --- |
| 開場 | 觀看前導影片；配戴後觀看 VR 短版開場與操作教學。 |
| 循聲尋找 | 音虫起初隱藏但持續發聲；觀眾循聲轉向，發聲可讓前方音虫短暫顯現。 |
| 對準捕捉 | 光罩隨控制器照向場景；同一群累積有效對準 3 秒後自動捕捉，過程逐漸顯現、聲音變小。 |
| 移開再繼續 | 移開保留原群的捕捉進度、顯現程度與音量，再對準即可接續。 |
| 完成收集 | SAN（遊戲精神值）由 100 開始，每次成功捕捉扣 20；收集 5 個不同變體後歸零，轉入回收。 |
| 回收與修復 | 轉向後方回收站，將捕捉器前端放入感應區，再於介面確認；能量轉為物品，呈現城市修復。 |
| 帶走成果 | 摘下頭戴後取得聲音收藏票，掃票上 QR 開啟感謝信、收集摘要與個人旋律。 |

- 音虫共 3 種基礎類型、各 3 個變體；同場不重複相同變體。
- 同時最多存在 3 群；捕捉後 SAN 尚未歸零就補一群。捕捉器不發聲。
- 每個已收集變體對應一個預製聲部，組成本場的紀念旋律。

## 技術組成

| 部分 | 採用技術／用途 |
| --- | --- |
| VR 遊戲 | Unity 6.3 LTS（6000.3.19f1）、C#、URP、OpenXR、XR Interaction Toolkit。 |
| 頭戴與控制器 | Quest 3／Touch Plus，透過現有 USB Link 線連接 Windows 電腦；遊戲由電腦執行。 |
| 聲音互動 | 頭戴內建喇叭／麥克風；規劃本機回音消除（AEC）與人聲偵測（VAD）。 |
| 本機串接服務（Gateway） | Node.js、TypeScript、WebSocket、SQLite，使用 pnpm 管理套件，處理成果保存、背景上傳與收藏票列印。 |
| 網頁 | Next.js、React、TypeScript、pnpm，製作成果頁、API 及工作人員管理頁。 |
| 線上服務 | 原型規劃使用 Vercel／Neon；網頁以 Web Audio API 同步播放聲部。 |

遊戲判定由 Unity 處理；網際網路暫時中斷時先保存本機成果，恢復後補傳。詳細版本與限制見[開發規格](docs/docs_vr/開發規格.md)。

## 資料夾安排

各部分使用同一個 Git repo，依功能分資料夾；標示「規劃」的目錄於建立專案時加入。

```text
dctProject/
├─ docs/
│  ├─ docs_vr/             VR 主線技術文件
│  │  ├─ 開發文件/         分工、時程、逐步教學與範例程式
│  │  │  ├─ 環境與Git.md    工具安裝與 clone
│  │  │  ├─ Unity基本操作.md 視窗與基本操作
│  │  │  ├─ 工作包/        直接按 U0／U1／U2／U3／I1／W1／I2／X1 實作
│  │  │  ├─ 共用/          素材交付、網頁慣例與驗收
│  │  │  └─ 範例程式/      依 U0～X1 工作包分類的教學來源
│  │  ├─ assets/           介面示意、配置圖與技術流程圖
│  │  └─ tools/            圖稿產生工具
│  └─ archive/             手機、紅外線投影與 MR 的歷史規劃
├─ unity-vr/               ［規劃］Unity 專案
├─ web/                    ［規劃］Next.js 網頁，使用 pnpm
├─ gateway/                ［規劃］本機串接服務，使用 pnpm
├─ shared/                 ［規劃］共用資料格式與素材對照
└─ .gitignore              排除安裝檔、快取、產物與本機資料
```

## 文件入口

| 想了解什麼 | 文件 |
| --- | --- |
| 作品如何運作、需要哪些設備 | [技術架構](docs/docs_vr/技術架構.md) |
| 模組之間如何傳資料 | [技術流程圖](docs/docs_vr/技術流程圖.md) |
| 遊戲規則、資料格式與驗收條件 | [開發規格](docs/docs_vr/開發規格.md) |
| 選好工作包後實作 | [開發文件入口](docs/docs_vr/開發文件/README.md) |
| 工作認領與交付日期 | [工作包](docs/docs_vr/開發文件/01_工作包與認領.md)／[里程碑與美術期限](docs/docs_vr/開發文件/02_里程碑與美術期限.md) |
| 器材、參考來源與必要測試 | [器材清單](docs/docs_vr/器材清單.md)／[重要連結](docs/docs_vr/重要連結.md)／[待確認與驗證清單](docs/docs_vr/待確認與驗證清單.md) |

## 開發與協作

第一次取得專案，在想存放 repo 的上一層資料夾執行：

```text
git clone https://github.com/fengyenchia/dctProject.git
cd dctProject
```

新加入先讀[環境與 Git](docs/docs_vr/開發文件/環境與Git.md)的安裝與 clone，再看 [Unity 基本操作](docs/docs_vr/開發文件/Unity基本操作.md)入門，接著依工作包操作。三人可認領 Unity 功能；Windows 負責正式 Quest Link 整合，Mac 可先做桌面模擬。

協作採 `main`＋功能分支；命名為 `範圍/功能`，如 `unity/capture`、`web/souvenir`。完整名稱與工作包對應見[分支安排](docs/docs_vr/開發文件/環境與Git.md#分支安排與名稱)。每項工作一位主責，完成自測並記錄結果後，以 PR 合併 main。

專案建立完成後，從對應位置啟動：

| 部分 | 開啟／執行位置 | 日常操作 |
| --- | --- | --- |
| Unity | Unity Hub 選 `unity-vr/` | 開啟團隊共用專案。 |
| 網頁 | 終端機先 `cd web` | `pnpm dev`；首次拉取依賴用 `pnpm install --frozen-lockfile`。 |
| Gateway | 另一個終端機先 `cd gateway` | `pnpm dev`；首次拉取依賴用 `pnpm install --frozen-lockfile`。 |

- 留在 repo 根目錄時，可用 `pnpm --dir web dev` 啟動網頁，或 `pnpm --dir gateway dev` 啟動本機服務。
- web 與 gateway 各自保留 `package.json`、`pnpm-lock.yaml`，分開安裝與啟動。
- 提交程式、`.meta`、設定及 lockfile；快取與本機資料依根目錄 [.gitignore](.gitignore) 排除。
- 大模型、音檔與影片先設定 Git LFS；同一場景／Prefab 同時由一人修改。
- 教學範例與實機驗收分開記錄；桌面模擬通過後，仍需測試 Quest Link、收音、循聲及效能。
