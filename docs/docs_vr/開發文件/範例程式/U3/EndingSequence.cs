using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AudioBugs.Tutorial
{
    public sealed class EndingSequence : MonoBehaviour
    {
        public DesktopLab lab;
        public CanvasGroup blackout;
        public GameObject exitGuide;
        public GameObject letterPanel;
        public Button openButton;
        public Button finishButton;
        public TMP_Text body;
        public Animator letterAnimator;
        [Min(.1f)] public float fadeSeconds = 1f;
        [TextArea] public string letterOne = "你帶回了第一份聲音。謝謝你為城市修復出力。";
        [TextArea] public string letterTwo = "幾處街角已開始恢復。謝謝你帶回這些聲音。";
        [TextArea] public string letterThree = "這些聲音讓更多街角得以修復。謝謝你一路探索。";

        private GamePhase previousPhase = GamePhase.Ready;
        private bool opening;
        private bool bodyShown;

        private void Start()
        {
            blackout.alpha = 0f;
            blackout.interactable = false;
            blackout.blocksRaycasts = false;
            letterPanel.SetActive(false);
            if (exitGuide != null) exitGuide.SetActive(false);
            if (letterAnimator != null) letterAnimator.enabled = false;
        }

        private void Update()
        {
            var phase = lab.Rules.Phase;
            if (phase != previousPhase) {
                previousPhase = phase;
                opening = false;
                bodyShown = false;
                if (letterAnimator != null) letterAnimator.enabled = false;
                letterPanel.SetActive(phase == GamePhase.ThankYou);
                if (exitGuide != null) exitGuide.SetActive(phase == GamePhase.ExitRoute);
            }
            if (phase == GamePhase.EndingVideo) {
                if (lab.CanInteract) {
                    blackout.alpha = Mathf.MoveTowards(blackout.alpha, 1f, Time.deltaTime / fadeSeconds);
                    if (blackout.alpha >= 1f) lab.CompleteEndingVideo();
                }
                return;
            }
            blackout.alpha = phase == GamePhase.ThankYou || phase == GamePhase.Ending ? 1f : 0f;
            if (phase != GamePhase.ThankYou) return;
            if (lab.Rules.LetterOpened && !opening && !bodyShown) BeginOpening();
            if (letterAnimator != null && opening) letterAnimator.speed = lab.CanInteract ? 1f : 0f;
            openButton.gameObject.SetActive(!lab.Rules.LetterOpened);
            openButton.interactable = lab.CanInteract;
            finishButton.gameObject.SetActive(bodyShown);
            finishButton.interactable = lab.CanInteract;
            body.gameObject.SetActive(bodyShown);
        }

        public void OpenLetter()
        {
            if (!lab.CanInteract || lab.Rules.Phase != GamePhase.ThankYou) return;
            lab.OpenThankYou();
        }

        private void BeginOpening()
        {
            if (letterAnimator == null) {
                ShowLetterBody();
                return;
            }
            opening = true;
            letterAnimator.enabled = true;
            letterAnimator.Play("OpenLetter", 0, 0f);
        }

        // 若有美術動畫，加在 OpenLetter 的最後一格。
        public void ShowLetterBody()
        {
            if (lab.Rules.Phase != GamePhase.ThankYou || !lab.Rules.LetterOpened) return;
            opening = false;
            bodyShown = true;
            body.text = lab.Rules.LetterTier == 1 ? letterOne : lab.Rules.LetterTier == 2 ? letterTwo : letterThree;
        }

        public void FinishLetter()
        {
            if (bodyShown) lab.FinishThankYou();
        }
    }
}
