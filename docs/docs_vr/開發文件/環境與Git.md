# 環境與 Git

依下列階段操作；日常版控使用 **VS Code 圖形介面**，首次取得 repo 使用 `git clone`。

## 1. 現在所有人要做

**對象：三位技術組成員。** 開發設備為 2 台 Windows、1 台 Mac M3；此階段先安裝工具、取得 repo，不用各自建立 Unity 專案。

| 工具 | Windows 成員（2 人） | Mac M3 成員（1 人） | 完成標準 |
| --- | --- | --- | --- |
| Unity Hub | 安裝、登入 | 安裝、登入 | 可開啟 Hub。 |
| Unity Editor | 安裝 **6000.3.19f1** | 安裝 **6000.3.19f1 Apple Silicon 版** | Hub 的 Installs 看得到此版本；已有者不重裝。 |
| VS Code | 安裝 VS Code＋C# 擴充 | 同左 | 可開啟程式編輯器。 |
| Git | 安裝 Git for Windows | 安裝 Git | 終端機執行 `git --version` 能顯示版本。 |

### 取得 repo（三人各做一次）

在要存放專案的上一層資料夾開終端機，執行：

```text
git clone https://github.com/fengyenchia/dctProject.git
```

- 已有 repo 者不用重新 clone。
- VS Code →「檔案 → 開啟資料夾」→ 選 `dctProject`，可看到本文件。
- **現在先做到這裡。** 尚未出現 `unity-vr/` 時，等待 U0 建立者提交共用專案。
- Node.js／pnpm 到 I1／W1 開工前再準備，見[網頁／Gateway 環境](共用/網頁開發慣例.md)。

## 2. 拿到共用專案後要做

**U0 建立者先做：**依 [U0 教學](工作包/U0.md)建立唯一的 Universal 3D（URP）專案，放在 repo 的 `unity-vr/`，提交並合併後通知其他兩人。

| 誰要做 | 操作 | 完成標準 |
| --- | --- | --- |
| 其他兩位成員 | 關閉 Unity，在 VS Code 切到 main → 原始檔控制「… → Pull」 | repo 出現 `unity-vr/`；不另建第二份專案。 |
| 三人 | Unity Hub → Add／Add project from disk → 選 `unity-vr/` | 使用 6000.3.19f1 開啟同一套專案，等待初次匯入。 |
| 三人 | 開啟專案後進 Preferences／Settings → External Tools → External Script Editor，選 VS Code；Windows 從 Edit 選單進入，Mac 從 Unity 選單進入 | 在 Unity 雙擊 `.cs`，會用 VS Code 開啟。 |
| 其他兩位成員 | 開啟共用專案，等待匯入與編譯完成 | 無編譯錯誤即可開始自己的 U2／U3；不用完整跑 U1 或另交練習場景。 |
| U0／U1 負責人（A） | 執行 CaptureLab，依 U1 檢查完整捕捉與回收流程 | 完整規則驗證由 A 負責。 |
| 三人 | 提交前依第 4 節檢查排除規則；首次拉取大素材前設定 Git LFS | 不提交 Library／快取；素材連同 `.meta`。 |

<details>
<summary>首次 Commit 提示缺少署名時才做（三人各自設定）</summary>

只有提交時出現缺少姓名／信箱的提示，才在 repo 根目錄終端機執行；不是登入密碼：

```text
git config user.name "你的姓名或GitHub名稱"
git config user.email "你的GitHub提交信箱"
```

</details>

## 3. 之後打包才要做

**現在不用補裝打包模組。** 以下只由實際輸出執行檔的人處理；一般 Editor 練習不需要完成這張表。

| 誰要做 | 什麼時候 | 操作 | 完成標準 |
| --- | --- | --- | --- |
| X1 負責人（Windows） | 要輸出 Windows 執行檔前 | Unity Hub → Installs → 6000.3.19f1 的管理選單 → Add modules；按 X1 的建置後端確認所需工具，使用 IL2CPP 時補裝 Windows Build Support（IL2CPP）及對應 C++ 建置工具 | 依 [X1](工作包/X1.md)輸出並執行 Windows 桌面版，再進行 Quest Link 驗證。 |
| Mac M3 成員 | 需要輸出 Mac 桌面測試版時 | 在 Hub 確認對應 Mac 建置支援，缺少需要的模組才補裝 | 可執行 Mac 測試版；平常只在 Editor 操作可略過。 |

本方案由電腦執行遊戲，**不需要安裝 Android Build Support 來製作 Quest APK**。

## 4. Git 排除與素材設定

1. 在 repo 根目錄確認 [.gitignore](../../../.gitignore)存在，使用同一份排除規則。
2. 打開 VS Code 左側「原始檔控制」，變更清單不應出現 node_modules、.next、Unity Library、gateway/data。
3. 程式、素材來源、`.meta` 與 lockfile 仍須提交；大型素材另用 Git LFS。

| 納入 Git | 排除 Git |
| --- | --- |
| `Assets/` 與所有 `.meta` | `Library/`、`Temp/`、`Logs/`、`Obj/` |
| `Packages/manifest.json`、`packages-lock.json` | `UserSettings/`、`Builds/` |
| `ProjectSettings/`、本文件與共用契約 | `.env*` 的真實密碼檔；只留 `.env.example` |
| `package.json`、web／gateway 各自的 `pnpm-lock.yaml` | `node_modules/`、`.next/`、本機資料庫與觀眾成果 |

- 完整範本可查 [GitHub Unity gitignore](https://github.com/github/gitignore/blob/main/Unity.gitignore)。
- 首次加入大模型／音檔前，由 U0 建立者設定 Git LFS 追蹤規則並提交 `.gitattributes`；三人安裝 Git LFS 並各執行一次 `git lfs install`。

<details>
<summary>U0 建立者：一次性 Git LFS 設定</summary>

在 repo 根目錄執行 `git lfs track "*.fbx" "*.blend" "*.wav" "*.mp4"`；再用 VS Code 提交產生的 `.gitattributes`。

</details>
- 移動素材請在 Unity Project 視窗操作，避免漏移 `.meta`。

## 5. 每次開發

### 分支安排與名稱

- `main` 保存目前整合版本，同時包含 Unity、Gateway、Web 與文件。
- 每項功能從最新 `main` 開分支；主責先在功能分支整合最新 main、自測並記錄結果，再由當次整合者於本機合併到 main，驗證後 Push。
- 一個分支指定一位主責；按功能認領，不綁定 A／B／C。
- 分支包含整個 repo 的版本；名稱只標示工作範圍，並非只保留該資料夾。

格式：`範圍/功能`，使用小寫英文與連字號。以下是開工時使用的名稱，無須一次全部建立。

工作包代號：U＝Unity、I＝系統串接、W＝Web、X＝XR 整合／驗證。數字是分類內編號；完整認領方式見[三人認領與接續](01_工作包與認領.md#三人怎麼認領與接續)。

| 工作包 | 分支名稱 | 本次工作範圍 |
| --- | --- | --- |
| [U0](工作包/U0.md) | `unity/setup` | 建立 Unity 專案、固定套件與 Git 設定。 |
| [U1](工作包/U1.md) | `unity/capture` | 群落生成、捕捉進度、SAN 與去重。 |
| [U2](工作包/U2.md) | `unity/mask-audio` | 場景 Mask、發聲顯現與捕捉音量。 |
| [U3](工作包/U3.md) | `unity/ui-recycle` | 左右手、介面、到站與回收演出。 |
| I1 前置 | `shared/result-schema` | shared 的成果格式、素材對照與範例資料。 |
| [I1](工作包/I1.md) | `gateway/save` | Gateway 連線、本機保存與 ACK。 |
| [W1](工作包/W1.md) | `web/souvenir` | Next.js 成果頁與聲部播放。 |
| [W1](工作包/W1.md) | `web/admin` | 工作人員管理頁與前導播放頁。 |
| [I2](工作包/I2.md) | `gateway/upload` | 雲端成果 API、outbox 與背景補傳；包含必要的 web API 修改。 |
| [I2](工作包/I2.md) | `gateway/print` | 票面排版、QR 與列印。 |
| [I2](工作包/I2.md) | `unity/session-reset` | 保存／結尾／交票後的下一場重置。 |
| [X1](工作包/X1.md) | `unity/quest-link` | Windows／Quest Link、追蹤與實機整合。 |
| 文件 | `docs/development` | 開發教學與技術文件更新。 |

- **分工安排：** A：U0＋U1，後期接 W1＋I2；B：U2＋I1；C：U3＋X1（另一位 Windows 成員）。各包依分段期限並行，詳見[認領表](01_工作包與認領.md#三人怎麼認領與接續)。
- 每個工作包可分多次小成果合併；不用等整包做完才合併。後續另取具體名稱，例如 `unity/capture-progress`、`web/souvenir-player`。
- 合併後確認內容已進 `main`，即可刪除已合併分支；下一項工作從更新後的 `main` 新開。
- 同時修改同一個 Scene／Prefab／Input Actions 前先協調；分支無法避免同一資產的合併衝突。

### 開分支與提交

在 VS Code 開啟 **repo 根目錄 dctProject**。先停止 Play、儲存並關閉 Unity；拉取／切分支前，先提交目前分支要保留的修改。

「原始檔控制」是左側分支圖示；「…」是該面板的更多動作。按鈕可能依語言顯示中英文。

**1. 開始新功能**

| 步驟 | VS Code 操作 | 檢查 |
| --- | --- | --- |
| 1 | 原始檔控制確認「變更／已暫存變更」皆無待處理項目 | 有修改先提交，不直接捨棄。 |
| 2 | 點左下角分支名稱 → 選 `main` | 左下角顯示 main。 |
| 3 | 原始檔控制 → `…` → `Pull`（提取） | 取得 GitHub 最新整合版。 |
| 4 | 左下角分支名稱 → `Create new branch…`（建立新分支） | 輸入上方分支表的名稱，例如 `unity/capture`。 |
| 5 | 確認左下角已切到自己的分支，再開 Unity | 不在 main 直接開發。 |

接續既有分支時，直接從左下角選該分支，不重建；已發布的分支先 Pull 再開工。

**2. 檢查、暫存與提交**

| 步驟 | VS Code 操作 | 檢查 |
| --- | --- | --- |
| 1 | 停止 Play、儲存；點「原始檔控制」內的變更檔案 | 左右對照差異，確認是本次修改。 |
| 2 | 在本次檔案旁按 `＋`（Stage Changes／暫存變更） | 只暫存本次檔案；Unity 素材連同 `.meta`。 |
| 3 | 展開「已暫存變更」逐一檢查 | 不含其他工作、快取、密鑰及觀眾資料。 |
| 4 | 輸入提交訊息，例如「完成捕捉進度保留」→ `Commit`（提交） | Commit 先保存到本機，尚未上傳。 |
| 5 | 新分支按 `Publish Branch`（發布分支）；後續用 `…` → `Push`（推送） | 發布到既有 origin；依提示登入 GitHub。 |

- 使用 Publish Branch 發布目前分支；不要使用「Publish to GitHub」另建一個 repo。
- Pull 是下載整合更新；Push 是上傳提交。`Sync Changes` 會先 Pull 再 Push，教學先分開操作。
- 找不到按鈕時可在「檢視 → 命令選擇區」搜尋 `Git: Pull`、`Git: Push`；這是 VS Code 選單命令，不需輸入終端機指令。

**3. 合併與推送**

**功能分支接入最新 main → 解衝突 → Unity 自測 → 合併 main → 再驗證 → Push**

先提交修改、關閉 Unity；同時只由一人整合 main。以下 `…` 都是 VS Code 原始檔控制面板的更多動作。

| 步驟 | 在哪個分支 | 操作 |
| --- | --- | --- |
| 1. 接入 main | 自己的功能分支 | `… → Fetch`，再 `… → Branch → Merge… → origin/main`。 |
| 2. 解衝突 | 自己的功能分支 | 有 Merge Changes 就逐檔處理，儲存、暫存，依提示完成合併提交；沒有則繼續。 |
| 3. 自測 | 自己的功能分支 | 開 Unity，確認編譯、場景與相關功能正常；再關閉，必要時 Commit，Push 功能分支。 |
| 4. 合併 main | main | 切 main → `… → Pull` → `… → Branch → Merge…`，選自己的本機功能分支，如 `unity/setup`。 |
| 5. 再驗證 | main | 確認無衝突，再開 Unity 複驗，通過後儲存、關閉；有修正先檢查並 Commit。 |
| 6. 推送 | main | `… → Push`，讓其他人能從 main 拉取。 |

- 步驟 4 的 Pull 若帶入新提交，先回功能分支重做步驟 1～3。
- Git 無衝突仍要做 Unity 測試；任何衝突或功能錯誤未處理完，先不要 Push main。
- Push 被拒絕時先核對遠端更新、重新整合與測試，不使用 Force Push。

<details>
<summary>出現衝突時怎麼處理</summary>

| 狀況 | 處理方式 |
| --- | --- |
| 程式／文字 | 開啟衝突檔，逐段比較 Current／Incoming 與結果；確認後儲存、暫存，依提示完成合併提交。 |
| Scene／Prefab／Input Actions | 找資產主責共同處理，不整份選 Accept Current／Incoming／Both。 |
| 編譯或功能錯誤 | 在功能分支修正並重新測試；main 合併後仍出錯，也先修正、複驗再推送。 |

</details>

**4. 其他組員取得整合版**

**儲存／提交修改 → 關閉 Unity → 切 main →「… → Pull」→ 開 Unity 複驗。** 下一個功能從更新後的 main 開新分支。

功能尚未完成、只想接入其他人的更新時，依上表步驟 1～3 操作即可。

## 6. 避免互相卡住

| 檔案 | 協作方式 |
| --- | --- |
| 測試場景 | 各自一個，如 `CaptureLab`、`AudioLab`、`UiLab`。 |
| 正式 Integration 場景 | 每次指定一位整合者；其他人交 Prefab。 |
| 群落 Prefab | 一位主責；美術交模型子物件，技術保留 Collider 與腳本根物件。 |
| Input Actions／ProjectSettings | 修改前先告知；合併後全員重開驗證。 |
| 套件版本 | 主責單次修改並提交 lockfile；其他人不按 Update all。 |

## 卡住時先看

| 現象 | 檢查 |
| --- | --- |
| 看不到別人的物件 | 有沒有拉取 Scene／Prefab 及 `.meta`？是否開錯場景？ |
| 腳本不能掛上 | Console 第一個紅字、檔名是否與 public class 相同。 |
| Missing Script | 腳本／`.meta` 漏交，或專案尚未編譯成功。 |
| Windows 才能用的功能在 Mac 報錯 | 列印／原生音訊介面用平台條件隔離；Mac 用模擬適配器。 |
| Play 中改的數值不見 | 停止 Play 後再修改；執行中改值通常不會保存到場景。 |

來源：[VS Code 衝突處理](https://code.visualstudio.com/docs/sourcecontrol/merge-conflicts)、[VS Code 版控](https://code.visualstudio.com/docs/sourcecontrol/overview)、[分支操作](https://code.visualstudio.com/docs/sourcecontrol/branches-worktrees)、[Pull／Push](https://code.visualstudio.com/docs/sourcecontrol/repos-remotes)、[Unity 系統需求](https://docs.unity3d.com/6000.3/Documentation/Manual/system-requirements.html)、[Node LTS](https://nodejs.org/en/about/previous-releases)、[GitHub flow](https://docs.github.com/en/get-started/using-github/github-flow)。

[回工作包入口](README.md)
