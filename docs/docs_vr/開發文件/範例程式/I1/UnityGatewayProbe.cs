using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using UnityEngine;

namespace AudioBugs.Tutorial
{
    // 本機保存、重連與管理命令；票務及雲端交付分別由 I2／W1 接續。
    public sealed class UnityGatewayProbe : MonoBehaviour
    {
        public DesktopLab lab;
        public bool connected;
        public bool resultSaved;
        public string status = "Not connected";
        public string publicToken;
        public bool automaticConnection = true, automaticResultSave = true;
        private string session = Guid.NewGuid().ToString("N");
        private readonly ConcurrentQueue<Received> incoming = new ConcurrentQueue<Received>();
        private readonly CancellationTokenSource cancel = new CancellationTokenSource();
        private ClientWebSocket socket;
        private Envelope pending;
        private bool connecting, sending;
        private int connectionGeneration;
        private readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);
        private readonly ConcurrentDictionary<string, string> commandReceipts = new ConcurrentDictionary<string, string>();
        private float nextConnect, nextResult, nextSnapshot;
        private float reconnectSeconds = 1;
        private bool recoveryFailed;
        public string SessionId => session;
        public string ResultId => pending?.payload.result_id;
        public bool CanReset => resultSaved && pending != null && !sending && !connecting;

        private void Start() {
            Debug.Log("Gateway key file: " + Path.Combine(Application.persistentDataPath, "gateway-key.txt"));
            try {
                var candidates = Directory.GetFiles(Application.persistentDataPath, "pending-*.json")
                    .Where(path => !File.Exists(Path.Combine(Application.persistentDataPath, "delivered-" + Path.GetFileNameWithoutExtension(path).Substring(8) + ".json")))
                    .OrderBy(File.GetLastWriteTimeUtc).ToArray();
                if (candidates.Length > 0) {
                    var recovered = JsonUtility.FromJson<Envelope>(File.ReadAllText(candidates[0]));
                    if (recovered?.payload?.variants == null || recovered.payload.san != 0 || recovered.payload.capture_count != 5
                        || !recovered.payload.recycled || string.IsNullOrEmpty(recovered.session_id)
                        || string.IsNullOrEmpty(recovered.payload.result_id) || recovered.payload.token?.Length != 64) throw new InvalidDataException("Invalid pending result");
                    var variants = recovered.payload.variants.Select(item => {
                        string id = item.variant_id;
                        if (id == null || id.Length != 2 || id[0] < 'A' || id[0] > 'C' || id[1] < '1' || id[1] > '3') throw new InvalidDataException("Invalid variant");
                        return (id[0] - 'A') * 3 + id[1] - '1';
                    }).ToArray();
                    if (!lab.RestoreEnding(variants)) throw new InvalidDataException("Cannot restore Ending");
                    pending = recovered; session = recovered.session_id; publicToken = recovered.payload.token;
                    status = "Recovered fixed Ending result; complete ticket delivery before new session";
                }
            } catch (Exception error) { recoveryFailed = true; lab.SetPaused(true); status = "Recovery blocked: " + error.Message; Debug.LogError(status); }
        }

        public async void Connect()
        {
            if (connecting || connected || recoveryFailed) return;
            connecting = true;
            nextConnect = Time.unscaledTime + reconnectSeconds; reconnectSeconds = Mathf.Min(10, reconnectSeconds * 2);
            int generation = ++connectionGeneration;
            try {
                string key = File.ReadAllText(Path.Combine(Application.persistentDataPath, "gateway-key.txt")).Trim();
                socket?.Dispose();
                var client = new ClientWebSocket();
                socket = client;
                await client.ConnectAsync(new Uri("ws://127.0.0.1:8787"), cancel.Token);
                await Send(JsonUtility.ToJson(new Hello { key = key, session_id = session }));
                status = "Waiting for HELLO_OK";
                Receive(client, generation);
            } catch (Exception error) { status = error.Message; Debug.LogWarning(status); }
            finally { connecting = false; }
        }

        private async void Receive(ClientWebSocket client, int generation)
        {
            var buffer = new byte[4096];
            try {
                while (!cancel.IsCancellationRequested && client.State == WebSocketState.Open) {
                    using (var message = new MemoryStream()) {
                        WebSocketReceiveResult part;
                        do {
                            part = await client.ReceiveAsync(new ArraySegment<byte>(buffer), cancel.Token);
                            if (part.MessageType == WebSocketMessageType.Close) { QueueReceived(generation, "{\"type\":\"DISCONNECTED\"}"); return; }
                            if (part.MessageType != WebSocketMessageType.Text) throw new InvalidDataException("Text only");
                            message.Write(buffer, 0, part.Count);
                            if (message.Length > 65536) throw new InvalidDataException("Message too large");
                        } while (!part.EndOfMessage);
                        QueueReceived(generation, Encoding.UTF8.GetString(message.ToArray()));
                    }
                }
            } catch (Exception) {
                if (!cancel.IsCancellationRequested) QueueReceived(generation, "{\"type\":\"DISCONNECTED\"}");
            }
        }

        private void QueueReceived(int generation, string json)
        {
            incoming.Enqueue(new Received { generation = generation, json = json });
        }

        private void Update()
        {
            while (incoming.TryDequeue(out var message)) {
                if (message.generation != connectionGeneration) continue;
                var ack = JsonUtility.FromJson<Ack>(message.json);
                if (ack.type == "HELLO_OK") { connected = true; reconnectSeconds = 1; status = "Connected"; }
                else if (ack.type == "DISCONNECTED") { connected = false; status = "Disconnected; retrying when Automatic Connection is enabled"; }
                else if (ack.type == "RESULT_SAVED" && pending != null && ack.session_id == pending.session_id
                    && ack.event_id == pending.event_id && ack.result_id == pending.payload.result_id) {
                    resultSaved = true;
                    status = "RESULT_SAVED (local SQLite only)";
                    Debug.Log(status);
                } else if (ack.type == "COMMAND" && ack.session_id == session) ExecuteCommand(ack);
                else if (ack.type == "ERROR") { status = ack.message; Debug.LogWarning(status); }
            }
            if (recoveryFailed) return;
            if (automaticConnection && !connected && !connecting && Time.unscaledTime >= nextConnect) Connect();
            if (automaticResultSave && connected && !resultSaved && lab != null && lab.Rules.Phase == GamePhase.Ending && Time.unscaledTime >= nextResult) {
                nextResult = Time.unscaledTime + 3; SendCurrentResult();
            }
            if (connected && Time.unscaledTime >= nextSnapshot) { nextSnapshot = Time.unscaledTime + 1; SendSnapshot(); }
        }

        private async void SendSnapshot() {
            try { await Send(JsonUtility.ToJson(new SnapshotEnvelope { session_id = session,
                payload = new Snapshot { phase = lab.Rules.Phase.ToString(), san = lab.Rules.San,
                    collected = lab.Rules.Collected.Count, groups = lab.Rules.Groups.Count,
                    paused = lab.Rules.Paused, pose_valid = lab.poseValid, calibration_valid = lab.calibrationValid,
                    audio_ready = lab.audioReady, station_visited = lab.Rules.StationVisited, result_saved = resultSaved } })); }
            catch { connected = false; }
        }

        private async void ExecuteCommand(Ack command) {
            if (string.IsNullOrEmpty(command.command_id)) return;
            if (commandReceipts.TryGetValue(command.command_id, out var prior)) { try { await Send(prior); } catch { connected = false; } return; }
            bool accepted = false; string reason = "invalid phase or unavailable input"; System.Action afterAck = null;
            switch (command.command) {
                case "SESSION_START": if (lab.Rules.Phase == GamePhase.Ready && lab.CanInteract && lab.calibrationValid) { lab.BeginSession(); accepted = lab.Rules.Phase == GamePhase.Intro; } break;
                case "PAUSE": lab.SetPaused(true); accepted = true; break;
                case "RESUME": lab.SetPaused(false); accepted = !lab.Rules.Paused; break;
                case "CALIBRATE": lab.onCalibrationRequested.Invoke(); accepted = lab.calibrationValid && lab.poseValid; break;
                case "RESET":
                    if (lab.ticketHandoffReady) { accepted = true; afterAck = lab.onResetRequested.Invoke; } break;
                case "ABORT":
                    if (pending == null && lab.Rules.Phase != GamePhase.Ending) {
                        try {
                            File.WriteAllText(Path.Combine(Application.persistentDataPath, "aborted-" + session + ".txt"), DateTime.UtcNow.ToString("o") + " " + lab.Rules.Phase);
                            accepted = true; afterAck = () => { lab.AbortSession(); NewConnectionSession(); };
                        } catch (Exception error) { reason = error.Message; }
                    } break;
            }
            var receipt = JsonUtility.ToJson(new CommandReceipt { session_id = session, command_id = command.command_id,
                payload = new CommandStatus { accepted = accepted, reason = accepted ? "accepted; check next snapshot" : reason } });
            commandReceipts.TryAdd(command.command_id, receipt);
            try { await Send(receipt); afterAck?.Invoke(); } catch { connected = false; }
        }

        public async void SendCurrentResult()
        {
            if (!connected || sending || lab == null || lab.Rules.Phase != GamePhase.Ending) return;
            sending = true;
            try {
                if (pending == null) pending = MakeResult();
                string json = JsonUtility.ToJson(pending, true);
                string path = Path.Combine(Application.persistentDataPath, "pending-" + pending.payload.result_id + ".json");
                // 寫檔失敗時保留同一結果，下次仍先重試寫檔，成功後才可傳送。
                if (!File.Exists(path)) {
                    string temp = path + ".tmp";
                    File.WriteAllText(temp, json);
                    File.Move(temp, path);
                    Debug.Log("Pending result: " + path);
                } else if (File.ReadAllText(path) != json) throw new InvalidDataException("Pending result conflict");
                publicToken = pending.payload.token;
                await Send(JsonUtility.ToJson(pending));
                status = "Waiting for RESULT_SAVED";
            } catch (Exception error) { status = error.Message; Debug.LogWarning(status); }
            finally { sending = false; }
        }

        private async System.Threading.Tasks.Task Send(string json)
        {
            await sendLock.WaitAsync(cancel.Token);
            try { var bytes = Encoding.UTF8.GetBytes(json); await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancel.Token); }
            finally { sendLock.Release(); }
        }

        private Envelope MakeResult()
        {
            var items = lab.Rules.Collected.Select(index => {
                string type = ((char)('A' + index / 3)).ToString();
                string id = type + (index % 3 + 1);
                return new Item { type_id = type, variant_id = id, name = lab.catalog == null ? "Sample " + id : lab.catalog.Name(index),
                    source_id = "source-" + id, source_label = lab.catalog == null ? "Source " + id : lab.catalog.Source(index), stem_id = "stem-" + id, image_id = id };
            }).ToArray();
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes);
            return new Envelope { event_id = Guid.NewGuid().ToString("N"), session_id = session,
                payload = new Payload { result_id = Guid.NewGuid().ToString("N"), token = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant(),
                    completed_at = DateTime.UtcNow.ToString("o"), variants = items,
                    asset_version = lab.catalog == null ? "prototype-1" : lab.catalog.assetVersion,
                    recipe_version = lab.catalog == null ? "stems-1" : lab.catalog.recipeVersion,
                    melody = new Melody { stem_ids = items.Select(item => item.stem_id).ToArray() } } };
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(580, 12, 440, 190), GUI.skin.box);
            GUILayout.Label("GATEWAY PROBE | " + status);
            if (!connected && GUILayout.Button("Connect Gateway")) Connect();
            if (connected && lab != null && lab.Rules.Phase == GamePhase.Ending && GUILayout.Button("Send same result / retry")) SendCurrentResult();
            if (!string.IsNullOrEmpty(publicToken) && GUILayout.Button("Copy result token")) GUIUtility.systemCopyBuffer = publicToken;
            if (!string.IsNullOrEmpty(ResultId) && GUILayout.Button("Copy result id")) GUIUtility.systemCopyBuffer = ResultId;
            GUILayout.EndArea();
        }

        public bool ClearDeliveredSession(string expectedResultId, string mode)
        {
            if (!CanReset || ResultId != expectedResultId || lab == null || lab.Rules.Phase != GamePhase.Ending) return false;
            // 不刪成果暫存；另記已交付，之後的復原工作可辨識這筆已完成。
            var receipt = new Receipt { result_id = expectedResultId, mode = mode, delivered_at = DateTime.UtcNow.ToString("o") };
            File.WriteAllText(Path.Combine(Application.persistentDataPath, "delivered-" + expectedResultId + ".json"), JsonUtility.ToJson(receipt, true));
            NewConnectionSession();
            return true;
        }

        private void NewConnectionSession() {
            connectionGeneration++; socket?.Abort(); socket?.Dispose(); socket = null;
            while (incoming.TryDequeue(out _)) { }
            session = Guid.NewGuid().ToString("N"); pending = null;
            commandReceipts.Clear(); connected = false; resultSaved = false; publicToken = null;
            nextConnect = 0; status = "Ready for next session";
        }

        private void OnDestroy() { cancel.Cancel(); socket?.Abort(); socket?.Dispose(); }

        private sealed class Received { public int generation; public string json; }
        [Serializable] private sealed class Receipt { public string result_id, mode, delivered_at; }
        [Serializable] private sealed class Hello { public string type = "HELLO"; public string key; public string session_id; }
        [Serializable] private sealed class Ack { public string type = "", session_id = "", event_id = "", result_id = "", message = "", command = "", command_id = ""; }
        [Serializable] private sealed class SnapshotEnvelope { public string type = "STATE_SNAPSHOT", session_id; public Snapshot payload; }
        [Serializable] private sealed class Snapshot { public string phase; public int san, collected, groups; public bool paused, pose_valid, calibration_valid, audio_ready, station_visited, result_saved; }
        [Serializable] private sealed class CommandReceipt { public string type = "COMMAND_ACK", session_id, command_id; public CommandStatus payload; }
        [Serializable] private sealed class CommandStatus { public bool accepted; public string reason; }
        [Serializable] private sealed class Envelope { public int schema_version = 1; public string type = "RESULT_READY"; public string event_id; public string session_id; public int revision = 1; public Payload payload; }
        [Serializable] private sealed class Payload {
            public string result_id, token, completed_at;
            public string layout_id = "vr-prototype-1", asset_version = "prototype-1", recipe_version = "stems-1";
            public int san = 0, capture_count = 5;
            public bool recycled = true;
            public Item[] variants;
            public Melody melody;
            public string repair_id = "repair-demo";
        }
        [Serializable] private sealed class Item { public string type_id, variant_id, name, source_id, source_label, stem_id, image_id; }
        [Serializable] private sealed class Melody { public int bpm = 100, beats_per_bar = 4, bars = 4; public string[] stem_ids; }
    }
}
