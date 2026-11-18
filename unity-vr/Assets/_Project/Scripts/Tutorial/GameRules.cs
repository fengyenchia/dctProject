using System;
using System.Collections.Generic;
using System.Linq;

// 教學用純 C# 核心：不依賴 XR、材質、網頁或音訊套件。
namespace AudioBugs.Tutorial
{
    public enum GamePhase { Ready, Intro, Tutorial, Active, Return, Recycling, ExitRoute, EndingVideo, ThankYou, Ending }

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
        public const int CaptureLimit = 5;
        public const int ConcurrentGroupLimit = 3;
        public const int DefaultSpawnPointCount = 8;
        public const float RainStartsAt = 60f;
        public const float RainEndsAt = 120f;
        private static readonly string[] VariantCodes = { "A1", "A2", "A3", "B1", "B2", "B3", "C1", "C2", "C3", "B4" };
        private static readonly int[] RegularVariants = { 0, 1, 2, 3, 4, 5, 9 };
        private static readonly int[] RainVariants = { RainVariant };
        public const int RainVariant = 6;
        public const int VariantSlotCount = 10;
        public static int CurrentVariantCount => RegularVariants.Length + RainVariants.Length;
        private int[] pointVariants;
        private readonly Random random;
        private readonly HashSet<int> appeared = new HashSet<int>();
        private readonly HashSet<int> usedPoints = new HashSet<int>();
        private readonly List<BugState> groups = new List<BugState>();
        private readonly List<int> collected = new List<int>();
        private int spawnPointCount = DefaultSpawnPointCount;
        public IReadOnlyList<BugState> Groups => groups;
        public IReadOnlyList<int> Collected => collected;
        public GamePhase Phase { get; private set; } = GamePhase.Ready;
        public bool Paused { get; private set; }
        public bool StationVisited { get; private set; }
        public bool CanRecycle => collected.Count > 0 && (Phase == GamePhase.Active || Phase == GamePhase.Return);
        public bool LetterOpened { get; private set; }
        public float ExplorationSeconds { get; private set; }
        public bool IsRaining { get; private set; }
        public int LetterTier => GetLetterTier(collected.Count);
        public int San => Math.Max(0, 100 - collected.Count * 20);
        public static int GetLetterTier(int count)
        {
            if (count < 1 || count > CaptureLimit) return 0;
            if (count == 1) return 1;
            return count <= 3 ? 2 : 3;
        }

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

        public static string VariantCode(int index) => VariantCodes[index];
        public static int VariantIndex(string code) => Array.IndexOf(VariantCodes, code);
        public static bool IsCurrentVariant(int index) => index == RainVariant || Array.IndexOf(RegularVariants, index) >= 0;

        public bool CompleteTutorial(int? firstPoint = null, int pointCount = DefaultSpawnPointCount,
            int[] configuredPointVariants = null)
        {
            if (Paused || Phase != GamePhase.Tutorial || pointCount < ConcurrentGroupLimit) return false;
            if (configuredPointVariants != null && (configuredPointVariants.Length != pointCount
                || configuredPointVariants.Any(index => !IsCurrentVariant(index)))) return false;
            spawnPointCount = pointCount;
            pointVariants = configuredPointVariants;
            Phase = GamePhase.Active;
            SpawnOne(firstPoint);
            for (int i = 1; i < ConcurrentGroupLimit; i++) SpawnOne();
            return true;
        }

        public void SetPaused(bool paused) { Paused = paused; }
        // 只接受 I1 已落盤的完整成功成果；不從 UI 文字重建進行中的遊戲。
        public bool RestoreCompleted(int[] variants) {
            if (Phase != GamePhase.Ready || variants == null || GetLetterTier(variants.Length) == 0
                || variants.Distinct().Count() != variants.Length
                || variants.Any(i => i < 0 || i >= VariantSlotCount)) return false;
            collected.AddRange(variants); foreach (var variant in variants) appeared.Add(variant);
            LetterOpened = true;
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
            float nextTime = ExplorationSeconds + delta;
            bool startsRaining = ExplorationSeconds < RainStartsAt && nextTime >= RainStartsAt;
            bool endsRaining = ExplorationSeconds < RainEndsAt && nextTime >= RainEndsAt;
            float validDelta = delta;
            if (endsRaining && groups.Any(group => group.Id == aimedId && group.Variant == RainVariant))
                validDelta = RainEndsAt - ExplorationSeconds;
            string captured = AdvanceCapture(validDelta, aimedId);
            ExplorationSeconds = nextTime;
            if (startsRaining && !endsRaining && Phase == GamePhase.Active) {
                IsRaining = true;
                SpawnOne(rain: true);
            }
            if (endsRaining) {
                IsRaining = false;
                groups.RemoveAll(group => group.Variant == RainVariant);
            }
            return captured;
        }

        private string AdvanceCapture(float delta, string aimedId)
        {
            if (delta <= 0f) return null;
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

        private void SpawnOne(int? preferredPoint = null, bool rain = false)
        {
            if (Paused || Phase != GamePhase.Active || San <= 0) return;
            if (!rain && groups.Count(group => group.Variant != RainVariant) >= ConcurrentGroupLimit) return;
            var pool = rain ? RainVariants : RegularVariants;
            var variants = pool.Where(x => !appeared.Contains(x)).ToArray();
            var points = Enumerable.Range(0, spawnPointCount).Where(x => !usedPoints.Contains(x)
                && (pointVariants == null || variants.Contains(pointVariants[x]))).ToArray();
            if (variants.Length == 0 || points.Length == 0) return;
            int point = preferredPoint.HasValue && points.Contains(preferredPoint.Value)
                ? preferredPoint.Value : points[random.Next(points.Length)];
            int variant = pointVariants == null ? variants[random.Next(variants.Length)] : pointVariants[point];
            groups.Add(new BugState {
                Id = Guid.NewGuid().ToString("N"), Point = point,
                Variant = variant
            });
            appeared.Add(variant);
            usedPoints.Add(point);
        }

        public bool AuthorizeStation(bool validTracking, bool insideCircle, bool headNear, bool tipInside)
        {
            if (Paused || !CanRecycle || !validTracking || !insideCircle || !headNear || !tipInside)
                return false;
            StationVisited = true;
            return true;
        }

        public void InvalidateStation() { StationVisited = false; }

        public bool ConfirmRecycle(bool validTracking, bool insideCircle)
        {
            if (Paused || !CanRecycle || !StationVisited || !validTracking || !insideCircle)
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
            Phase = GamePhase.ExitRoute;
            return true;
        }

        public bool ArriveAtExit(bool insideExit)
        {
            if (Paused || Phase != GamePhase.ExitRoute || !insideExit) return false;
            Phase = GamePhase.EndingVideo;
            return true;
        }

        public bool CompleteEndingVideo()
        {
            if (Paused || Phase != GamePhase.EndingVideo) return false;
            Phase = GamePhase.ThankYou;
            return true;
        }

        public bool OpenThankYou()
        {
            if (Paused || Phase != GamePhase.ThankYou || LetterOpened) return false;
            LetterOpened = true;
            return true;
        }

        public bool FinishThankYou()
        {
            if (Paused || Phase != GamePhase.ThankYou || !LetterOpened) return false;
            Phase = GamePhase.Ending;
            return true;
        }
    }
}
