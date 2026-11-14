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
- VS Code →「File → Open Folder…」→ 選 `dctProject`，可看到本文件。
- **現在先做到這裡。** 尚未出現 `unity-vr/` 時，等待 U0 建立者提交共用專案。
- Node.js／pnpm 到 I1／W1 開工前再準備，見[網頁／Gateway 環境](共用/網頁開發慣例.md)。

## 2. 拿到共用專案後要做

**U0 建立者先做：** 依 [U0 教學](工作包/U0.md)建立唯一的 Universal 3D（URP）專案，放在 repo 的 `unity-vr/`，提交並合併後通知其他兩人。

| 誰要做 | 操作 | 完成標準 |
| --- | --- | --- |
| 其他兩位成員 | 關閉 Unity，在 VS Code 切到 main → Source Control「… → Pull」 | repo 出現 `unity-vr/`；不另建第二份專案。 |
| 三人 | Unity Hub → Add／Add project from disk → 選 `unity-vr/` | 使用 6000.3.19f1 開啟同一套專案，等待初次匯入。 |
| 三人 | 開啟專案後進 Preferences／Settings → External Tools → External Script Editor，選 VS Code；Windows 從 Edit 選單進入，Mac 從 Unity 選單進入 | 在 Unity 雙擊 `.cs`，會用 VS Code 開啟。 |
| 其他兩位成員 | 開啟共用專案，等待匯入與編譯完成 | 無編譯錯誤即可開始自己的 U2／U3；不用完整跑 U1 或另交練習場景。 |
| U0／U1 負責人 | 執行 CaptureLab，依 U1 檢查完整捕捉與回收流程 | 跑完捕捉與回收流程。 |
| 三人 | 提交前確認排除規則生效；依第 4 節安裝並初始化 Git LFS 一次 | 不提交 Library／快取；素材連同 `.meta`。 |

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

**共用規則已設定好，組員不用重做。**

| 共用檔案 | 用途 |
| --- | --- |
| [.gitignore](../../../.gitignore) | 排除 Unity／網頁快取、自動產生的 IDE 檔案、密鑰及本機成果。 |
| [.gitattributes](../../../.gitattributes) | FBX、Blend、WAV、MP4 使用 Git LFS；程式、`.meta` 與 lockfile 仍正常提交。 |

**所有成員：** 安裝 [Git LFS](https://git-lfs.com/)，在自己的 repo 根目錄終端機執行一次：

```text
git lfs install
```

之後照常 Commit／Push 素材，不用再設定追蹤規則。移動 Unity 素材請在 Project 視窗操作，連同 `.meta` 一起提交。

## 5. 每次開發

### 分支安排與名稱

- `main` 放已整合、測試過的版本。
- 每項功能從最新 main 開分支，一個分支由一人負責。
- 分支命名：`範圍/功能`，使用小寫英文，例如 `unity/capture`。只建立目前要做的分支。

工作包與人員分工見[認領表](01_工作包與認領.md)。

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

- 小功能完成就可合併；下一項工作從更新後的 main 開新分支。
- 同一個 Scene／Prefab／Input Actions 同時只由一人修改。

### 一般開發

以下的 **…** 指 VS Code 左側 **Source Control** 面板的 **More Actions**。

1. **開始新工作包**　關閉 Unity → 點左下角分支名稱，切到 `main` → **… → Pull** → 再點左下角分支名稱 → **Create new branch…** → 輸入上表的分支名稱。
2. **開發中，main 有更新**　先 Commit 手上的修改並關閉 Unity → 留在自己的功能分支 → **… → Fetch** → **… → Branch → Merge…** → 選 `origin/main`。有衝突就先處理，再開 Unity 確認。若 main 沒有更新，直接在自己的分支繼續做。
3. **提交自己的修改**　儲存後在 **Source Control** 確認變更 → **Stage Changes**（素材連同 `.meta`）→ 輸入訊息 → **Commit**。

### 自己合併到 main

1. **在自己的功能分支檢查**　Commit 並關閉 Unity → **… → Fetch** → **… → Branch → Merge…** → 選 `origin/main`。有衝突先處理，再開 Unity 確認編譯及場景正常。若有修正，先 Commit。首次上傳功能分支用 **Publish Branch**，之後用 **… → Push**。
2. **切到 main 合併**　關閉 Unity → 切到 `main` → **… → Pull** → **… → Branch → Merge…** → 選自己的功能分支，例如 `unity/capture`。若 Pull 取得新提交，先回功能分支重做上一步。
3. **測試並推送**　有衝突先處理，開 Unity 確認功能正常。有修正就先 Commit，通過後用 **… → Push** 更新 GitHub 的 `main`。

### 看最新成果

儲存並 Commit 手上的修改 → 關閉 Unity → 切到 `main` → **… → Pull** → 開 Unity 看更新。

### Fetch 和 Pull 的差別

- **Fetch** 只更新遠端分支的紀錄，例如 `origin/main`。要接到自己的分支，再 **Merge…** 並選 `origin/main`。
- **Pull** 取得遠端更新並整合到目前分支。在 `main` 上按 **Pull**，會更新本機的 `main`。

<details>
<summary>出現衝突時怎麼處理</summary>

| 狀況 | 處理方式 |
| --- | --- |
| 程式／文字 | 開啟衝突檔，逐段比較 Current／Incoming 與結果；確認後儲存、暫存，依提示完成合併提交。 |
| Scene／Prefab／Input Actions | 找資產主責共同處理，不整份選 Accept Current／Incoming／Both。 |
| 編譯或功能錯誤 | 在功能分支修正並重新測試；main 合併後仍出錯，也先修正、複驗再推送。 |

</details>

## 6. 避免互相卡住

| 檔案 | 協作方式 |
| --- | --- |
| 共用場景 `CaptureLab` | 同一時間一人修改 Scene，其他人可做 Prefab、素材或腳本。交接時先合併到 `main`，接手者更新後再改。 |
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
