using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace AudioBugs.Tutorial
{
    // 工作人員電腦的教學面板；正式管理頁可呼叫同一個 TryResetSession。
    public sealed class UnityDeliveryReset : MonoBehaviour
    {
        public DesktopLab lab;
        public UnityGatewayProbe probe;
        public bool allowSimulatedDelivery;
        public bool showDeveloperPanel = true;
        public string status = "Waiting for local save";
        private Delivery receipt;
        public bool ReadyForReset => Ready;
        private bool Ready => lab != null && probe != null && probe.CanReset && lab.Rules.Phase == GamePhase.Ending
            && receipt != null && receipt.result_id == probe.ResultId && receipt.ticket_ready && receipt.delivered
            && (receipt.delivery_mode == "printed" || allowSimulatedDelivery && receipt.delivery_mode == "simulated");

        private IEnumerator Start()
        {
            lab.onResetRequested.AddListener(TryResetSession);
            while (true) {
                if (lab == null || probe == null || !probe.resultSaved || lab.Rules.Phase != GamePhase.Ending || string.IsNullOrEmpty(probe.ResultId)) {
                    receipt = null; status = "Waiting for Ending and RESULT_SAVED";
                    yield return new WaitForSeconds(1f); continue;
                }
                string expected = probe.ResultId; string key = null;
                try { key = File.ReadAllText(Path.Combine(Application.persistentDataPath, "gateway-key.txt")).Trim(); }
                catch { status = "Check gateway-key.txt"; }
                if (string.IsNullOrEmpty(key)) { yield return new WaitForSeconds(1f); continue; }
                using (var request = UnityWebRequest.Get("http://127.0.0.1:8788/delivery/" + Uri.EscapeDataString(expected))) {
                    request.SetRequestHeader("Authorization", "Bearer " + key); request.timeout = 3;
                    yield return request.SendWebRequest();
                    if (probe.ResultId == expected) {
                        if (request.result == UnityWebRequest.Result.Success) {
                            receipt = JsonUtility.FromJson<Delivery>(request.downloadHandler.text);
                            status = Ready ? "Saved + Ending + ticket handoff confirmed" : "Waiting for ticket file / handoff";
                        } else { receipt = null; status = "I2 status unavailable; check Gateway delivery mode"; }
                    }
                }
                yield return new WaitForSeconds(1f);
            }
        }

        private void Update() { if (lab != null) lab.ticketHandoffReady = Ready; }
        private void OnDestroy() { if (lab != null) { lab.onResetRequested.RemoveListener(TryResetSession); lab.ticketHandoffReady = false; } }

        public void TryResetSession()
        {
            if (!Ready) return;
            try {
                string id = receipt.result_id; string mode = receipt.delivery_mode;
                if (!probe.ClearDeliveredSession(id, mode)) return;
                if (!lab.ResetForNextSession()) throw new InvalidOperationException("Game is not in Ending");
                receipt = null; status = "EXHIBIT_READY; check area and reconnect before starting";
            } catch (Exception error) { status = error.Message; Debug.LogWarning(status); }
        }

        private void OnGUI()
        {
            if (!showDeveloperPanel) return;
            GUILayout.BeginArea(new Rect(580, 215, 440, 135), GUI.skin.box);
            GUILayout.Label("I2 DELIVERY | " + status);
            GUILayout.Label(allowSimulatedDelivery ? "PROTOTYPE: simulated handoff allowed" : "Printed ticket handoff required");
            GUI.enabled = Ready;
            if (GUILayout.Button("Reset after ticket handoff")) TryResetSession();
            GUI.enabled = true; GUILayout.EndArea();
        }
        [Serializable] private sealed class Delivery {
            public string result_id = "", delivery_mode = "";
            public bool ticket_ready = false, delivered = false;
        }
    }
}
