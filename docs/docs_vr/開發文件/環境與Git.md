# 環境與 Git

所有工作包共用。**日常版控使用 VS Code 圖形介面；第一次取得專案使用 git clone。** 建立 Unity 專案請到 [U0](工作包/U0.md)，Node.js／pnpm 可到 I1／W1 開工前再安裝。

## 完成條件

- [ ] 三人使用 Unity **6000.3.19f1**，可開同一個 URP 專案。
- [ ] 各自提交一個測試 Prefab；其他人拉取後可看到。
- [ ] 不提交 `Library`、密碼、成果資料及輸出執行檔。

## 1. 安裝工具

| 工具 | Windows（2 台） | macOS（Mac M3，1 台） |
| --- | --- | --- |
| Unity Hub | 安裝後登入；Installs → Locate 可加入已有 Editor | 同左 |
| Unity Editor | 6000.3.19f1 | 6000.3.19f1，選 Apple Silicon 版 |
| 程式編輯器 | VS Code＋C# 擴充 | 同左 |
| Git | 安裝 Git for Windows，終端機輸入 `git --version` 確認 | 安裝 Git，終端機輸入 `git --version` 確認 |
| Node.js | 共用 **24 LTS** 主版本；初次建置記錄完整版本 | 同左 |
| pnpm | web／gateway 固定 **11.9.0** | 同版本。 |

1. Unity Hub → Installs → 該版本 → Add modules。
2. Windows 正式打包環境安裝 Windows Build Support／IL2CPP 需要的工具；先做 Editor 練習可不先打包。
3. Mac 安裝對應的 Mac Build Support，供桌面測試；Quest Link 留在 Windows 驗證。
4. 原型不需要 Android APK；目前遊戲由電腦執行。
5. Unity → Preferences／Settings → External Tools → 選 VS Code；讓雙擊 `.cs` 可開啟編輯器。

終端機核對：

```text
git --version
node --version
npm --version
pnpm --version
```

若未安裝 pnpm，執行 `npm install --global pnpm@11.9.0`，再用 `pnpm --version` 核對；此 npm 命令只安裝工具。已安裝 pnpm 11.9.0 者可略過；npm 與 pnpm 是不同工具，分別核對版本。

| 資料夾 | 套件管理 | 要提交的 lockfile | 其他組員初次安裝 |
| --- | --- | --- | --- |
| `web/` | pnpm 11.9.0 | `pnpm-lock.yaml` | 在 web 執行 `pnpm install --frozen-lockfile`。 |
| `gateway/` | pnpm 11.9.0 | `pnpm-lock.yaml` | 在 gateway 執行 `pnpm install --frozen-lockfile`。 |

Node 與框架由建置者固定版本；web／gateway 分別安裝、各自保留 lockfile，不混用 npm／Yarn 安裝依賴。終端機位置見[目錄與啟動表](README.md#同一個-repo-的目錄)。

### 第一次取得 repo

終端機先切到要存放專案的**上一層資料夾**，再執行；已有 repo 的人不必重新 clone。

```text
git clone https://github.com/fengyenchia/dctProject.git
```

- clone 後，在 VS Code「檔案 → 開啟資料夾」選 `dctProject`；Unity、Web、Gateway 共用這個 Git。
- Unity 專案加入 repo 後，到 Unity Hub → Add → 選其中的 `unity-vr/`；首次建立由主責依 [U0](工作包/U0.md)操作。
<details>
<summary>首次署名設定（一次性設定，不是日常提交流程）</summary>

首次提交若提示缺少署名，依提示設定下列資料；這不是登入密碼。

```text
git config user.name "你的姓名或GitHub名稱"
git config user.email "你的GitHub提交信箱"
```

</details>

## 2. 先設 Git 排除

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

## 3. 每次開發

### 分支安排與名稱

- `main` 保存目前整合版本，同時包含 Unity、Gateway、Web 與文件。
- 每項功能從最新 `main` 開分支；主責完成自測、記錄結果後，提 Pull Request（PR）回 `main` 並確認可合併。
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
- 每個工作包可拆多次 PR；不用等整包做完才合併。後續另取具體名稱，例如 `unity/capture-progress`、`web/souvenir-player`。
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

**3. 在 GitHub 網頁建立 PR**

| 步驟 | 操作 |
| --- | --- |
| 1 | repo → Pull requests → New pull request。 |
| 2 | base 選 `main`，compare 選剛發布的功能分支。 |
| 3 | 填完成內容、操作入口、自測結果與限制 → Create pull request。 |
| 4 | 檢查差異與可合併狀態；有問題在同一分支修正，再 Commit／Push。 |
| 5 | 確認可合併並完成合併後，依下一節取得整合版。 |

**4. 合併後取得整合版**

| 步驟 | VS Code 操作 | 預期 |
| --- | --- | --- |
| 1 | 關閉 Unity；確認目前修改已提交 | 沒有未處理的變更。 |
| 2 | 左下角分支名稱 → 選 `main` | main 為目前分支。 |
| 3 | 原始檔控制 → `…` → `Pull` | 本機取得剛合併的內容。 |
| 4 | 再開 Unity，執行相關測試場景 | 看得到更新，Console 無新增紅字。 |

**自己的功能尚未完成，但要接入別人的更新：**

1. 提交自己的修改，關閉 Unity，留在自己的功能分支。
2. 原始檔控制 → `…` → `Fetch`（擷取），取得遠端分支資訊。
3. 「檢視 → 命令選擇區」→ 搜尋 `Git: Merge`，選合併分支操作 → 選 `origin/main`。
4. 無衝突時自測後 Commit／Push；如有 Scene／Prefab 衝突，找該資產主責一起處理，不直接選全部接受或覆蓋。

## 4. 避免互相卡住

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

來源：[VS Code 版控](https://code.visualstudio.com/docs/sourcecontrol/overview)、[分支操作](https://code.visualstudio.com/docs/sourcecontrol/branches-worktrees)、[Pull／Push](https://code.visualstudio.com/docs/sourcecontrol/repos-remotes)、[Unity 系統需求](https://docs.unity3d.com/6000.3/Documentation/Manual/system-requirements.html)、[Node LTS](https://nodejs.org/en/about/previous-releases)、[GitHub flow](https://docs.github.com/en/get-started/using-github/github-flow)。

[回工作包入口](README.md)
