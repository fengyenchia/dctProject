using System;
using UnityEngine;
namespace AudioBugs.Tutorial
{
    public sealed class VariantCatalog : MonoBehaviour
    {
        [Serializable] public sealed class Entry { public string name, sourceLabel; public Sprite icon; }
        public Entry[] entries = new Entry[9];
        public string assetVersion = "prototype-1", recipeVersion = "stems-1";
        public string Code(int index) => $"{(char)('A' + index / 3)}{index % 3 + 1}";
        public Entry Get(int index) => index >= 0 && index < entries.Length ? entries[index] : null;
        public string Name(int index) => string.IsNullOrWhiteSpace(Get(index)?.name) ? "Sample " + Code(index) : Get(index).name;
        public string Source(int index) => string.IsNullOrWhiteSpace(Get(index)?.sourceLabel) ? "Source " + Code(index) : Get(index).sourceLabel;
    }
}
