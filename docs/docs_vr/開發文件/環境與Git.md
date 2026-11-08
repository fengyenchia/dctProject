# 環境與 Git

所有工作包共用。工具安裝與 clone 在本頁；建立 Unity 專案請到 [U0](工作包/U0.md)，Node.js／pnpm 可到 I1／W1 開工前再安裝。

## 完成條件

- [ ] 三人使用 Unity **6000.3.19f1**，可開同一個 URP 專案。
- [ ] 各自提交一個測試 Prefab；其他人拉取後可看到。
- [ ] 不提交 `Library`、密碼、成果資料及輸出執行檔。

## 1. 安裝工具

| 工具 | Windows | macOS |
| --- | --- | --- |
| Unity Hub | 安裝後登入；Installs → Locate 可加入已有 Editor | 同左 |
| Unity Editor | 6000.3.19f1 | 同版本；Apple 選單 → 關於這台 Mac，先查看晶片 |
| Mac Editor 分支 | 不適用 | M1／M2／M3 等用 Apple Silicon；Intel 用 Intel 版 |
| 程式編輯器 | VS Code＋C# 擴充 | 同左 |
| Git | 安裝 Git for Windows，終端機輸入 `git --version` 確認 | 安裝 Git，終端機輸入 `git --version` 確認 |
| Node.js | 共用 **24 LTS** 主版本；初次建置記錄完整版本 | 同左 |
| pnpm | web／gateway 固定 **11.8.0** | 同版本。 |

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
```

若未安裝 pnpm，執行 `npm install --global pnpm@11.8.0`，再用 `pnpm --version` 核對；此 npm 命令只安裝工具。

| 資料夾 | 套件管理 | 要提交的 lockfile | 其他組員初次安裝 |
| --- | --- | --- | --- |
| `web/` | pnpm 11.8.0 | `pnpm-lock.yaml` | 在 web 執行 `pnpm install --frozen-lockfile`。 |
| `gateway/` | pnpm 11.8.0 | `pnpm-lock.yaml` | 在 gateway 執行 `pnpm install --frozen-lockfile`。 |

Node 與框架由建置者固定版本；web／gateway 分別安裝、各自保留 lockfile，不混用 npm／Yarn 安裝依賴。終端機位置見[目錄與啟動表](README.md#同一個-repo-的目錄)。

### 第一次取得 repo

終端機先切到要存放專案的**上一層資料夾**，再執行；已有 repo 的人不必重新 clone。

```text
git clone https://github.com/fengyenchia/dctProject.git
cd dctProject
git status
```

- clone 後得到整個 repo；Unity、Web、Gateway 共用這個 Git。
- Unity 專案加入 repo 後，到 Unity Hub → Add → 選其中的 `unity-vr/`；首次建立由主責依 [U0](工作包/U0.md)操作。
- 首次提交前設定本 repo 的署名；將下面兩個值換成自己的資料，這不是登入密碼。

```text
git config user.name "你的姓名或GitHub名稱"
git config user.email "你的GitHub提交信箱"
```

## 2. 先設 Git 排除

1. 在 repo 根目錄確認 [.gitignore](../../../.gitignore)存在，使用同一份排除規則。
2. 執行 `git status --short`，不應出現 node_modules、.next、Unity Library、gateway/data。
3. 程式、素材來源、`.meta` 與 lockfile 仍須提交；大型素材另用 Git LFS。

| 納入 Git | 排除 Git |
| --- | --- |
| `Assets/` 與所有 `.meta` | `Library/`、`Temp/`、`Logs/`、`Obj/` |
| `Packages/manifest.json`、`packages-lock.json` | `UserSettings/`、`Builds/` |
| `ProjectSettings/`、本文件與共用契約 | `.env*` 的真實密碼檔；只留 `.env.example` |
| `package.json`、web／gateway 各自的 `pnpm-lock.yaml` | `node_modules/`、`.next/`、本機資料庫與觀眾成果 |

- 完整範本可查 [GitHub Unity gitignore](https://github.com/github/gitignore/blob/main/Unity.gitignore)。
- 首次加入大模型／音檔前，三人安裝 Git LFS；在 repo 根目錄執行 `git lfs install`，再執行 `git lfs track "*.fbx" "*.blend" "*.wav" "*.mp4"`，提交產生的 `.gitattributes`。
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

- **建議認領：** A：U0 → U1 → W1；B：U2 → X1；C：U3 → I1 → I2。可互換，詳見[認領表](01_工作包與認領.md#三人怎麼認領與接續)。
- 每個工作包可拆多次 PR；不用等整包做完才合併。後續另取具體名稱，例如 `unity/capture-progress`、`web/souvenir-player`。
- 合併後確認內容已進 `main`，即可刪除已合併分支；下一項工作從更新後的 `main` 新開。
- 同時修改同一個 Scene／Prefab／Input Actions 前先協調；分支無法避免同一資產的合併衝突。

### 開分支與提交

以下指令在 **repo 根目錄**執行。拉取、合併或切分支前，先儲存並關閉 Unity；有尚未處理的變更時，先完成提交或處理，再切換。

**1. 開始新功能**

先執行 `git status --short`；沒有輸出代表工作目錄乾淨，再執行：

```text
git switch main
git pull --ff-only
git switch -c unity/capture
```

將 `unity/capture` 換成認領的分支名稱。接續既有分支時，用 `git switch unity/capture`，不重跑 `-c`；已推送過的分支再用 `git pull --ff-only` 更新。

**2. 檢查並提交本次修改**

停止 Play、儲存後執行：

```text
git status --short
git diff
```

依 status 顯示的實際路徑，逐一加入本次修改。以下以U1的 GameRules 腳本為例；其他功能換成自己的檔案，不照抄無關路徑。

```text
git add "unity-vr/Assets/_Project/Scripts/Tutorial/GameRules.cs" "unity-vr/Assets/_Project/Scripts/Tutorial/GameRules.cs.meta"
git diff --cached
git commit -m "完成捕捉進度保留"
git push -u origin unity/capture
```

- `git diff --cached` 是這次要提交的內容；確認沒有其他工作、快取或密鑰。
- 新增／搬移 Unity 素材時連同 `.meta` 提交；不使用 `git add .` 一次加入未檢查的變更。
- 首次 push 用 `-u` 設定遠端追蹤；同一分支後續提交可用 `git push`。
- HTTPS 驗證可用憑證管理器登入；終端機要求密碼時使用 personal access token。無法推送時確認帳號具有 repo 寫入權限；見 [GitHub HTTPS 驗證](https://docs.github.com/en/get-started/git-basics/about-remote-repositories#cloning-with-https-urls)。

**3. 在 GitHub 網頁建立 PR**

| 步驟 | 操作 |
| --- | --- |
| 1 | 開 repo → Pull requests → New pull request。 |
| 2 | base 選 `main`，compare 選剛推送的功能分支。 |
| 3 | 填寫完成內容、操作入口、測試步驟與限制，Create pull request。 |
| 4 | 主責核對變更與自測結果；有問題繼續在同一分支 commit／push，PR 會更新。 |
| 5 | 確認可合併並完成合併後，回本機執行下方指令，取得整合版本。 |

```text
git switch main
git pull --ff-only
```

**開發中需要整合別人的更新：**先提交自己的變更，留在功能分支執行 `git fetch origin`、`git merge origin/main`；若有 Scene／Prefab 衝突，找原主責一起處理後再提交，不自行覆蓋對方內容。

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

來源：[Unity 系統需求](https://docs.unity3d.com/6000.3/Documentation/Manual/system-requirements.html)、[Node LTS](https://nodejs.org/en/about/previous-releases)、[GitHub flow](https://docs.github.com/en/get-started/using-github/github-flow)。

[回工作包入口](README.md)
