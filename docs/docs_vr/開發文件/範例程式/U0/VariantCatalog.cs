using System;
using UnityEngine;
namespace AudioBugs.Tutorial
{
    public sealed class VariantCatalog : MonoBehaviour
    {
        [Serializable] public sealed class Entry { public string name, sourceLabel; public Sprite icon; }
        public Entry[] entries = new Entry[GameRules.VariantSlotCount];
        public string assetVersion = "prototype-2", recipeVersion = "stems-1";
        [Min(1)] public int bpm = 100, beatsPerBar = 4, bars = 4;
        public string Code(int index) => GameRules.VariantCode(index);
        public Entry Get(int index) => index >= 0 && index < entries.Length ? entries[index] : null;
        public string Name(int index) => string.IsNullOrWhiteSpace(Get(index)?.name) ? DefaultName(index) : Get(index).name;
        public string Source(int index) => string.IsNullOrWhiteSpace(Get(index)?.sourceLabel) ? DefaultSource(index) : Get(index).sourceLabel;
        public static string DefaultName(int index)
        {
            string code = GameRules.VariantCode(index);
            return code[0] switch {
                'A' => "海天使 " + code[1],
                'B' => "海蛞蝓 " + code[1],
                'C' when index == GameRules.RainVariant => "魟魚",
                _ => "Sample " + code
            };
        }

        public static string DefaultSource(int index) => GameRules.VariantCode(index) switch {
            "A1" => "鬧鐘",
            "A2" => "廣播壞掉",
            "A3" => "警報",
            "B1" => "電視雜訊",
            "B2" => "飆仔",
            "B3" => "玩具",
            "B4" => "冷氣",
            "C1" => "下雨",
            _ => "Source " + GameRules.VariantCode(index)
        };
    }
}
