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
            statusText.text = $"{rules.Phase}\nSAN {rules.San}   COLLECTED {rules.Collected.Count}/5\n"
                + (target == null ? "TARGET --" : $"TARGET {Code(target.Variant)}  {target.Progress:P0}");
            collectionText.text = rules.Collected.Count == 0 ? "NO COLLECTION" : string.Join("\n", rules.Collected.Select(Code));
            string key = string.Join(",", rules.Collected);
            if (catalog != null && cardRoot != null && cardPrefab != null && cardsKey != key) {
                foreach (Transform child in cardRoot) Destroy(child.gameObject);
                foreach (int index in rules.Collected) Instantiate(cardPrefab, cardRoot).Show(catalog, index);
                cardsKey = key; collectionText.text = rules.Collected.Count == 0 ? "NO COLLECTION" : "";
            }
            if (catalog != null && cardRoot != null && cardPrefab != null && rules.Collected.Count > 0) collectionText.text = "";
            bool recycle = rules.Phase == GamePhase.Return && rules.StationVisited;
            mainPage.SetActive(!collectionOpen && !recycle);
            collectionPage.SetActive(collectionOpen && !recycle);
            recyclePage.SetActive(recycle);
            confirmButton.interactable = recycle && lab.CanInteract;
            lab.uiBusy = pointers.Count > 0;
        }

        private static string Code(int variant) => $"{(char)('A' + variant / 3)}{variant % 3 + 1}";
        public void ShowMain() { collectionOpen = false; }
        public void ShowCollection() { collectionOpen = true; }
        public void ConfirmRecycle() { lab.ConfirmRecycle(); }
        public void StartSession() { lab.BeginSession(); }
        public void CompleteRepair() { lab.CompleteRepair(); }
        public void ClearHover() { pointers.Clear(); if (lab != null) lab.uiBusy = false; }
        public void OnPointerEnter(PointerEventData data) { pointers.Add(data.pointerId); }
        public void OnPointerExit(PointerEventData data) { pointers.Remove(data.pointerId); }
        private void OnDisable() { ClearHover(); }
    }
}
