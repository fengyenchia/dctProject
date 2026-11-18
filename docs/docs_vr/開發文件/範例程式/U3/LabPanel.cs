using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AudioBugs.Tutorial
{
    public sealed class LabPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public DesktopLab lab;
        public TMP_Text statusText;
        public TMP_Text collectionText;
        public GameObject mainPage;
        public bool enableCollectionPreview;
        public GameObject collectionPage;
        public GameObject recyclePage;
        public Button confirmButton;
        public VariantCatalog catalog;
        public Transform cardRoot;
        public CollectionCard cardPrefab;
        private string cardsKey;
        private bool collectionOpen;
        private GameRules displayedRules;
        private readonly HashSet<int> pointers = new HashSet<int>();

        private void Update()
        {
            if (lab == null) return;
            var rules = lab.Rules;
            if (!ReferenceEquals(displayedRules, rules)) { displayedRules = rules; collectionOpen = false; ClearHover(); }
            var target = lab.CurrentTarget;
            statusText.text = $"SAN {rules.San}\n"
                + (target == null ? "TARGET --" : $"TARGET {Code(target.Variant)}  {target.Progress:P0}");
            if (enableCollectionPreview && collectionText != null) UpdateCollection(rules);
            bool recycle = rules.CanRecycle && rules.StationVisited;
            bool showCollection = enableCollectionPreview && collectionPage != null && collectionOpen && !recycle;
            mainPage.SetActive(!showCollection && !recycle);
            if (collectionPage != null) collectionPage.SetActive(showCollection);
            recyclePage.SetActive(recycle);
            confirmButton.interactable = recycle && lab.CanInteract;
            lab.uiBusy = pointers.Count > 0;
        }

        private void UpdateCollection(GameRules rules)
        {
            string key = string.Join(",", rules.Collected);
            if (cardsKey == key) return;
            cardsKey = key;
            bool showCards = catalog != null && cardRoot != null && cardPrefab != null;
            if (showCards) {
                foreach (Transform child in cardRoot) Destroy(child.gameObject);
                foreach (int index in rules.Collected) Instantiate(cardPrefab, cardRoot).Show(catalog, index);
            }
            collectionText.text = rules.Collected.Count == 0 ? "NO COLLECTION"
                : showCards ? "" : string.Join("\n", rules.Collected.Select(Code));
        }

        private static string Code(int variant) => GameRules.VariantCode(variant);
        public void ShowMain() { collectionOpen = false; }
        public void ShowCollection() { if (enableCollectionPreview) collectionOpen = true; }
        public void ConfirmRecycle() { lab.ConfirmRecycle(); }
        public void StartSession() { lab.BeginSession(); }
        public void CompleteRepair() { lab.CompleteRepair(); }
        public void ClearHover() { pointers.Clear(); if (lab != null) lab.uiBusy = false; }
        public void OnPointerEnter(PointerEventData data) { pointers.Add(data.pointerId); }
        public void OnPointerExit(PointerEventData data) { pointers.Remove(data.pointerId); }
        private void OnDisable() { ClearHover(); }
    }
}
