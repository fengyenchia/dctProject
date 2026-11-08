# Unity 基本操作

建物件、拖欄位、接按鈕或看錯誤時查本頁。實作順序請回[工作包入口](README.md)。

## 視窗在哪裡

| 名稱 | 看什麼 | 找不到時 |
| --- | --- | --- |
| Hierarchy | 目前場景裡的物件 | Window → General → Hierarchy。 |
| Scene | 編輯物件位置 | Window → General → Scene。 |
| Game | 按 Play 後的玩家畫面 | Window → General → Game。 |
| Project | Assets 裡的檔案 | Window → General → Project。 |
| Inspector | 選中物件的欄位及元件 | Window → General → Inspector。 |
| Console | 錯誤與執行訊息 | Window → General → Console。 |

版面亂掉：右上 Layout → Default；本教學不要求自訂版面。

## 共用操作

| 要做的事 | 逐步操作 | 成功時 |
| --- | --- | --- |
| 建資料夾 | Project 選父資料夾 → 右鍵 Create → Folder → 輸入名稱 | 出現新資料夾。 |
| 建空物件 | Hierarchy 空白處右鍵 → Create Empty → 改名 | 出現有 Transform 的物件。 |
| 建子物件 | 選父物件 → 右鍵 Create Empty；檢查新物件縮排在父物件下 | 拖父物件時子物件一起移動。 |
| 建球／方塊 | Hierarchy 右鍵 → 3D Object → Sphere／Cube | Scene 看得到模型。 |
| 改位置 | 選物件 → Inspector → Transform → Position X／Y／Z | 填的是公尺。 |
| 歸零 | Transform 右上選單 → Reset | Position／Rotation 0，Scale 1。 |
| 加腳本 | 選 Hierarchy 物件 → Inspector → Add Component → 輸入 class 名稱 | Inspector 出現該腳本元件。 |
| 停用物件 | 取消 Inspector 最上方、物件名稱旁的勾選 | 自己與子物件都不執行。 |
| 停用元件 | 取消該元件標題左側勾選 | 只停用該功能。 |
| 刪元件 | 元件右上選單 → Remove Component | 其他元件不受影響。 |
| 儲存場景 | File → Save；首次選 `_Project/Scenes/` | Hierarchy 場景名旁不再有星號。 |
| 複製場景 | Project 選 `.unity` 檔 → Duplicate／Ctrl+D（Mac Cmd+D）→ 改名 | 雙擊新檔才切換到副本。 |

## 拖欄位：看來源是哪裡

| 欄位需要 | 從哪裡拖 | 例子 |
| --- | --- | --- |
| 場景物件／Transform | Hierarchy | Tracked Head 拖 Rig 裡的 Main Camera。 |
| 場景上的腳本元件 | Hierarchy 的物件 | Lab 欄位拖掛有 DesktopLab 的物件；**不是拖 `.cs` 檔**。 |
| Prefab 資產 | Project | Group Prefab 拖 `BugGroup.prefab`。 |
| Shader／音檔 | Project | Reveal Shader 拖 `.shader`；Source Clips 拖 `.wav`。 |
| Array 清單 | 先展開欄位，填 Size／拖拉長度，再填 Element 0、1… | 6 個出現點對應 6 個欄位。 |

`None` 代表還沒接；欄位顯示物件名稱才算完成。點欄位中的名稱可確認指到哪個物件。

## 接 Button／UnityEvent

1. 選要按的 Button，找到 Inspector 的 **On Click ()**。
2. 按 `+`；把有接收腳本的 **Hierarchy 物件**拖進空格。
3. 點 No Function → 腳本名稱 → 本章指定的方法。
4. 一個操作先只留一列；多列會重複呼叫。
5. 按 Play 再測；編輯模式按 Scene 中的按鈕不會執行遊戲。

例如U3會接 `LabPanel → ShowCollection()`；方法不存在就檢查是否漏複製腳本或 Console 有紅字。

## 匯入教學程式

1. Project 選目標資料夾 → 右鍵 **Show in Explorer／Reveal in Finder**。
2. 在檔案總管複製本文件附的 `.cs`／`.shader` 到該資料夾。
3. 回 Unity 等待編譯；Console 沒紅字再掛元件。
4. 需改程式時，雙擊 `.cs` → VS Code 修改 → 儲存 → 回 Unity 等編譯。

- 檔名與 public class 名稱一致，例如 `BugView.cs`／`BugView`。
- 不把整段程式貼進 Inspector；不要存成 `BugView.cs.txt`。
- `GameRules.cs` 是資料規則，**不掛在物件上**。

## 座標、Play 與錯誤

| 情況 | 做法 |
| --- | --- |
| 物件看不到 | Hierarchy 選它 → 滑鼠移到 Scene → 按 F 聚焦。 |
| 子物件的 Position | 相對父物件；本教學會標「世界」或「Local」。 |
| X／Y／Z | 左右／上下／前後；藍色 Z 軸作為瞄準前方。 |
| Play 中出現物件 | 執行期生成；停止 Play 後消失是正常的。 |
| 要保存欄位或擺位 | 停止 Play 再改，然後儲存；不要以為執行期變更會自動保存。 |
| Console 紅字 | 點最上方第一個錯誤 → 雙擊定位；先處理它，再看後面的連鎖錯誤。 |
| `NullReferenceException` | 檢查該元件是否有必填欄位仍為 None。 |
| `Missing Script` | 腳本或 `.meta` 漏交；先確認 Git 拉取完整且編譯成功。 |

## 每段的完成標準

**操作完成 → 看預期畫面／數值 → 對照錯誤表 → 儲存提交**。同一操作不在每章重複說明，遇到不熟的動作回本頁查。

[回工作包入口](README.md)
