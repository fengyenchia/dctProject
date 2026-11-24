# U3 範例程式

本資料夾只放本工作包維護的教學來源；實際複製位置見工作包。

- [RightStickLook.cs](RightStickLook.cs)／[RightStickLook.inputactions](RightStickLook.inputactions)　右搖桿左右轉身、上下俯仰、按下回正；暫停／追蹤／UI 閘控與回中後恢復。
- 自動接線與驗證工具：repo 的 `unity-vr/Assets/_Project/Editor/RightStickLookSetup.cs`，Unity 選 **Tools → AudioBugs → Configure CaptureLab Right Stick Look**。

- [RepairSequence.cs](RepairSequence.cs)　回收時依實收量播放粒子，可接正式建物動畫。
- [CaptureModelPreview.cs](CaptureModelPreview.cs)　成功捕捉後在捕捉器 Raw Image 顯示精細版模型。
- [ReturnToStartGuide.cs](ReturnToStartGuide.cs)　修復完成播放一次返回起點語音，暫停及新場重置。
- [EndingSequence.cs](EndingSequence.cs)　結尾定點淡黑、播放美術開信動畫、三版正文與完成事件。
- [DemoVideoSequence.cs](DemoVideoSequence.cs)　有正式開場與操作示範素材時使用。

[開始實作 U3](../../工作包/U3.md)｜[全部工作包](../README.md)
