using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace AudioBugs.Tutorial
{
    public sealed class CollectionCard : MonoBehaviour
    {
        public Image icon;
        public TMP_Text title, source;
        public void Show(VariantCatalog catalog, int index) {
            title.text = catalog.Name(index); source.text = catalog.Source(index);
            icon.sprite = catalog.Get(index)?.icon; icon.enabled = icon.sprite != null;
        }
    }
}
