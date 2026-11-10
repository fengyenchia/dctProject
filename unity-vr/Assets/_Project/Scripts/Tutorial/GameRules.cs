using System;
using System.Collections.Generic;
using System.Linq;

// 教學用純 C# 核心：不依賴 XR、材質、網頁或音訊套件。
namespace AudioBugs.Tutorial
{
    public enum GamePhase { Ready, Intro, Tutorial, Active, Return, Recycling, Ending }

    public sealed class BugState
    {
        public string Id;
        public int Point;
        public int Variant;
        public float Seconds;
        public float VoiceAge = -1f;
        public float Progress => Math.Min(1f, Seconds / 3f);
        public float Gain => 1f - Progress;
        public float VoiceVisibility => VoiceAge < 0f || VoiceAge >= 4f ? 0f
            : VoiceAge <= 3f ? VoiceAge / 3f : 4f - VoiceAge;
        public float Visibility => Math.Max(Progress, VoiceVisibility);
    }

    public sealed class GameRules
    {
        private readonly Random random;
        private readonly HashSet<int> appeared = new HashSet<int>();
        private readonly List<BugState> groups = new List<BugState>();
        private readonly List<int> collected = new List<int>();
        public IReadOnlyList<BugState> Groups => groups;
        public IReadOnlyList<int> Collected => collected;
        public GamePhase Phase { get; private set; } = GamePhase.Ready;
        public bool Paused { get; private set; }
        public bool StationVisited { get; private set; }
        public int San => Math.Max(0, 100 - collected.Count * 20);

        public GameRules(int? seed = null) { random = seed.HasValue ? new Random(seed.Value) : new Random(); }

        public bool Begin()
        {
            if (Phase != GamePhase.Ready) return false;
            Phase = GamePhase.Intro;
            return true;
        }

        public bool CompleteIntro()
        {
            if (Paused || Phase != GamePhase.Intro) return false;
            Phase = GamePhase.Tutorial;
            return true;
        }

        public bool CompleteTutorial()
        {
            if (Paused || Phase != GamePhase.Tutorial) return false;
            Phase = GamePhase.Active;
            for (int i = 0; i < 3; i++) SpawnOne();
            return true;
        }

        public void SetPaused(bool paused) { Paused = paused; }
        // 只接受 I1 已落盤的完整成功成果；不從 UI 文字重建進行中的遊戲。
        public bool RestoreCompleted(int[] variants) {
            if (Phase != GamePhase.Ready || variants == null || variants.Length != 5
                || variants.Distinct().Count() != 5 || variants.Any(i => i < 0 || i >= 9)) return false;
            collected.AddRange(variants); foreach (var variant in variants) appeared.Add(variant);
            Phase = GamePhase.Ending; return true;
        }

        // 只有已經通過 AEC/VAD 或開發事件的發聲才呼叫此方法。
        public bool Reveal(IEnumerable<string> eligibleIds)
        {
            if (Paused || Phase != GamePhase.Active) return false;
            var ids = new HashSet<string>(eligibleIds);
            foreach (var group in groups)
                if (ids.Contains(group.Id)) group.VoiceAge = 0f;
            return true;
        }

        // 每幀呼叫一次；目標無效時傳 null，仍更新發聲動畫。
        public string Tick(float delta, string aimedId)
        {
            if (Paused || Phase != GamePhase.Active || delta <= 0f) return null;
            foreach (var group in groups)
                if (group.VoiceAge >= 0f) group.VoiceAge += delta;
            var target = groups.Find(x => x.Id == aimedId);
            if (target == null) return null;
            target.Seconds = Math.Min(3f, target.Seconds + delta);
            if (target.Progress < 1f) return null;
            // 先移除，舊 Id 再次送進來也不會重複結算。
            groups.Remove(target);
            collected.Add(target.Variant);
            if (San == 0) Phase = GamePhase.Return;
            else SpawnOne();
            return target.Id;
        }

        private void SpawnOne()
        {
            if (Paused || Phase != GamePhase.Active || San <= 0 || groups.Count >= 3) return;
            var variants = Enumerable.Range(0, 9).Where(x => !appeared.Contains(x)).ToArray();
            var points = Enumerable.Range(0, 6).Where(x => groups.All(g => g.Point != x)).ToArray();
            if (variants.Length == 0 || points.Length == 0) return;
            int variant = variants[random.Next(variants.Length)];
            groups.Add(new BugState {
                Id = Guid.NewGuid().ToString("N"), Point = points[random.Next(points.Length)],
                Variant = variant
            });
            appeared.Add(variant);
        }

        public bool AuthorizeStation(bool validTracking, bool insideCircle, bool headNear, bool tipInside)
        {
            if (Paused || Phase != GamePhase.Return || !validTracking || !insideCircle || !headNear || !tipInside)
                return false;
            StationVisited = true;
            return true;
        }

        public void InvalidateStation() { StationVisited = false; }

        public bool ConfirmRecycle(bool validTracking, bool insideCircle)
        {
            if (Paused || Phase != GamePhase.Return || !StationVisited || !validTracking || !insideCircle)
                return false;
            Phase = GamePhase.Recycling;
            // 收集清單保留供成果使用；場內剩餘群落在回收結算時退場。
            groups.Clear();
            StationVisited = false;
            return true;
        }

        public bool CompleteRepair()
        {
            if (Paused || Phase != GamePhase.Recycling) return false;
            Phase = GamePhase.Ending;
            return true;
        }
    }
}
