using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

namespace AudioBugs.Tutorial
{
    // 桌面教學入口；OnGUI、V 鍵及樣本音色不作為正式觀眾介面。
    public sealed class DesktopLab : MonoBehaviour
    {
        public Shader revealShader;
        public AudioClip[] sourceClips = new AudioClip[9];
        public BugView groupPrefab;
        public BugView[] variantPrefabs = new BugView[0];
        public Transform[] spawnPoints = new Transform[0];
        public bool createLabGeometry = true;
        public Transform stationAnchor;
        public Collider stationZone;
        public bool showDeveloperPanel = true;
        public bool automaticDemos = true;
        public bool useExternalPose;
        public Transform trackedHead;
        public Transform trackedAim;
        public Transform trackedTip;
        public bool uiBusy;
        public bool poseValid = true;
        public bool calibrationValid = true;
        public bool audioReady;
        public VariantCatalog catalog;
        public UnityEvent onCalibrationRequested = new UnityEvent();
        public UnityEvent onResetRequested = new UnityEvent();
        public bool ticketHandoffReady;
        public UnityEvent onCaptured = new UnityEvent();

        private GameRules rules = new GameRules();
        private Camera labCamera;
        private readonly Vector3[] points = new Vector3[6];
        private readonly Dictionary<string, GameObject> views = new Dictionary<string, GameObject>();
        private readonly Dictionary<Collider, string> targets = new Dictionary<Collider, string>();
        private readonly List<AudioClip> generatedClips = new List<AudioClip>();
        private GameObject mask;
        private float yaw, pitch, phaseSeconds;
        private readonly Vector3 station = new Vector3(0f, .9f, -.65f);
        public GameRules Rules => rules;
        public string CurrentTargetId { get; private set; }
        public BugState CurrentTarget => rules.Groups.FirstOrDefault(g => g.Id == CurrentTargetId);
        public bool CanInteract => !rules.Paused && poseValid && Inside;
        private Vector3 StationPosition => stationAnchor == null ? station : stationAnchor.position;

        private void Start()
        {
            if (revealShader == null) { Debug.LogError("Assign PrototypeReveal shader first."); enabled = false; return; }
            if (useExternalPose && (trackedHead == null || trackedAim == null)) {
                Debug.LogError("Assign Tracked Head and Tracked Aim for external pose mode."); enabled = false; return;
            }
            labCamera = Camera.main;
            if (!useExternalPose && labCamera == null) {
                var go = new GameObject("LabCamera");
                labCamera = go.AddComponent<Camera>();
                go.tag = "MainCamera";
            }
            if (!useExternalPose) {
                labCamera.transform.SetPositionAndRotation(new Vector3(0, 1.6f, 0), Quaternion.identity);
                if (FindFirstObjectByType<AudioListener>() == null) labCamera.gameObject.AddComponent<AudioListener>();
            }
            if (spawnPoints.Length != 0 && (spawnPoints.Length != 6 || spawnPoints.Any(p => p == null))) {
                Debug.LogError("Spawn Points must be empty or contain exactly six assigned transforms."); enabled = false; return;
            }
            if (variantPrefabs.Length != 0 && variantPrefabs.Length != 9) {
                Debug.LogError("Variant Prefabs must be empty or have nine entries; empty entries use Group Prefab."); enabled = false; return;
            }
            if (createLabGeometry) MakeSolid("LabFloor", new Vector3(0, -.1f, 0), new Vector3(14, .2f, 14));
            for (int i = 0; i < 6; i++) {
                float angle = (i + .5f) * Mathf.PI / 3f;
                var direction = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
                points[i] = spawnPoints.Length == 6 ? spawnPoints[i].position : direction * 4f + Vector3.up * 1.3f;
                if (createLabGeometry) {
                    var wall = MakeSolid("LabWall_" + i, direction * 5f + Vector3.up * 1.5f, new Vector3(4, 3, .2f));
                    wall.transform.rotation = Quaternion.LookRotation(direction);
                }
            }
            if (stationAnchor == null) MakeSolid("LabStation", station, new Vector3(.6f, .5f, .3f));
            mask = GameObject.CreatePrimitive(PrimitiveType.Quad);
            mask.name = "LabMask";
            mask.GetComponent<Collider>().enabled = false;
            var material = new Material(revealShader);
            material.SetColor("_BaseColor", new Color(.4f, .85f, 1f, .25f));
            material.SetFloat("_Visibility", 1f);
            material.SetFloat("_RoundMask", 1f);
            mask.GetComponent<Renderer>().material = material;
            mask.transform.localScale = Vector3.one * .7f;
            mask.SetActive(false);
        }

        private Transform Head => useExternalPose ? trackedHead : labCamera.transform;
        // 桌面射線與畫面中心一致；虛擬到站前端使用另一個位置，不能混作瞄準原點。
        private Vector3 AimPosition => useExternalPose ? trackedAim.position : Head.position;
        private Vector3 AimForward => useExternalPose ? trackedAim.forward : Head.forward;
        private bool Inside => Head != null && new Vector2(Head.position.x, Head.position.z).magnitude <= 1.5f;

        private void Update()
        {
            if (useExternalPose && (trackedHead == null || trackedAim == null)) return;
            if (!useExternalPose && Mouse.current != null && Mouse.current.rightButton.isPressed) {
                var delta = Mouse.current.delta.ReadValue();
                yaw += delta.x * .15f;
                pitch = Mathf.Clamp(pitch - delta.y * .15f, -75f, 75f);
                Head.rotation = Quaternion.Euler(pitch, yaw, 0);
            }
            if (!CanInteract) { CurrentTargetId = null; mask.SetActive(false); SyncViews(); return; }
            if (automaticDemos && (rules.Phase == GamePhase.Intro || rules.Phase == GamePhase.Tutorial)) {
                phaseSeconds += Time.deltaTime;
                if (rules.Phase == GamePhase.Intro && phaseSeconds >= 15f) { rules.CompleteIntro(); phaseSeconds = 0f; }
                else if (rules.Phase == GamePhase.Tutorial && phaseSeconds >= 16f) { rules.CompleteTutorial(); phaseSeconds = 0f; }
            }
            SyncViews();
            if (rules.Phase == GamePhase.Active) {
                if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame) TriggerVoiceForDevelopment();
                CurrentTargetId = FindTargetAndMoveMask();
                string captured = rules.Tick(Time.deltaTime, CurrentTargetId);
                if (captured != null) {
                    Debug.Log("CAPTURE_RESULT " + captured + " SAN=" + rules.San);
                    onCaptured.Invoke();
                }
                SyncViews();
            } else { CurrentTargetId = null; mask.SetActive(false); }
            if (rules.Phase == GamePhase.Return) {
                float headDistance = Vector2.Distance(new Vector2(Head.position.x, Head.position.z), new Vector2(StationPosition.x, StationPosition.z));
                var tip = trackedTip != null ? trackedTip.position : useExternalPose
                    ? AimPosition + AimForward * .55f : Head.position + Vector3.down * .55f + Head.forward * .55f;
                bool tipInside = stationZone == null ? Vector3.Distance(tip, StationPosition) <= .4f
                    : Vector3.Distance(stationZone.ClosestPoint(tip), tip) <= .001f;
                rules.AuthorizeStation(poseValid, Inside, headDistance <= 1.1f, tipInside);
            }
        }

        private string FindTargetAndMoveMask()
        {
            if (uiBusy) { mask.SetActive(false); return null; }
            var ray = new Ray(AimPosition, AimForward);
            bool hitSurface = Physics.Raycast(ray, out var surface, 20f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            mask.SetActive(hitSurface);
            if (hitSurface) mask.transform.SetPositionAndRotation(surface.point + surface.normal * .01f, Quaternion.LookRotation(surface.normal));
            float nearest = hitSurface ? surface.distance : 20f;
            string selected = null;
            foreach (var hit in Physics.RaycastAll(ray, 20f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide)) {
                if (hit.distance > nearest || !targets.TryGetValue(hit.collider, out var id)) continue;
                selected = id;
                nearest = hit.distance;
            }
            return selected;
        }

        public void TriggerVoiceForDevelopment()
        {
            if (!poseValid || !Inside || rules.Paused || rules.Phase != GamePhase.Active) return;
            var visibleIds = rules.Groups.Where(g => {
                var direction = points[g.Point] - Head.position;
                return direction.magnitude <= 8f && Vector3.Angle(Head.forward, direction) <= 60f;
            }).Select(g => g.Id).ToArray();
            rules.Reveal(visibleIds);
        }

        private void SyncViews()
        {
            var activeIds = new HashSet<string>(rules.Groups.Select(g => g.Id));
            foreach (var id in views.Keys.Where(id => !activeIds.Contains(id)).ToArray()) {
                var go = views[id];
                targets.Remove(go.GetComponent<Collider>());
                Destroy(go);
                views.Remove(id);
            }
            foreach (var group in rules.Groups) {
                if (!views.TryGetValue(group.Id, out var go)) {
                    var selectedPrefab = variantPrefabs.Length == 9 && variantPrefabs[group.Variant] != null
                        ? variantPrefabs[group.Variant] : groupPrefab;
                    if (selectedPrefab != null) go = Instantiate(selectedPrefab).gameObject;
                    else {
                        go = new GameObject("BugGroup");
                        var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        visual.transform.SetParent(go.transform, false);
                        visual.transform.localScale = Vector3.one * .7f;
                        visual.GetComponent<Collider>().enabled = false;
                        go.AddComponent<BugView>().visual = visual.transform;
                    }
                    go.name = "Bug_" + group.Variant;
                    go.transform.position = points[group.Point];
                    var clip = group.Variant < sourceClips.Length ? sourceClips[group.Variant] : null;
                    go.GetComponent<BugView>().Initialize(revealShader, clip == null ? MakeTestTone(group.Variant) : clip,
                        Color.HSVToRGB(group.Variant / 9f, .7f, 1f));
                    views[group.Id] = go;
                    targets[go.GetComponent<Collider>()] = group.Id;
                }
                go.GetComponent<BugView>().Present(group, !CanInteract);
            }
        }

        private AudioClip MakeTestTone(int variant)
        {
            const int rate = 24000;
            var samples = new float[rate];
            for (int i = 0; i < samples.Length; i++)
                samples[i] = Mathf.Sin(2f * Mathf.PI * (220f + variant * 55f) * i / rate) * .15f;
            var clip = AudioClip.Create("TEST_TONE_" + variant, rate, 1, rate, false);
            clip.SetData(samples, 0);
            generatedClips.Add(clip);
            return clip;
        }

        private static GameObject MakeSolid(string name, Vector3 position, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;
            return go;
        }

        public void BeginSession() { if (CanInteract && calibrationValid && rules.Begin()) phaseSeconds = 0f; }
        public void ConfirmRecycle() { rules.ConfirmRecycle(poseValid, Inside); }
        public void CompleteRepair() { rules.CompleteRepair(); }
        public void SetPaused(bool paused) { if (!paused && (!poseValid || !Inside || !calibrationValid)) return; rules.SetPaused(paused); if (mask != null) SyncViews(); }
        public bool RestoreEnding(int[] variants) {
            if (rules.Phase != GamePhase.Ready || !rules.RestoreCompleted(variants)) return false;
            SyncViews(); return true;
        }
        public void AbortSession() {
            if (rules.Phase == GamePhase.Ending) return;
            rules = new GameRules(); CurrentTargetId = null; phaseSeconds = 0; uiBusy = false;
            if (mask != null) mask.SetActive(false); SyncViews();
        }
        // 僅由 I2 在「本機保存＋結尾＋交票」全部確認後呼叫。
        public bool ResetForNextSession()
        {
            if (rules.Phase != GamePhase.Ending) return false;
            foreach (var go in views.Values) go.GetComponent<AudioSource>()?.Stop();
            rules = new GameRules();
            CurrentTargetId = null; phaseSeconds = 0f; uiBusy = false;
            if (mask != null) mask.SetActive(false);
            SyncViews();
            foreach (var clip in generatedClips) Destroy(clip);
            generatedClips.Clear();
            return true;
        }
        public void SaveLabResult()
        {
            if (rules.Phase != GamePhase.Ending) return;
            var data = new LabResult { variants = rules.Collected.ToArray(), san = rules.San };
            var path = Path.Combine(Application.persistentDataPath, "lab-result.json");
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
            Debug.Log("Lab JSON only; not RESULT_SAVED: " + path);
        }

        private void OnGUI()
        {
            if (!showDeveloperPanel) return;
            GUILayout.BeginArea(new Rect(12, 12, 560, 350), GUI.skin.box);
            GUILayout.Label("DESKTOP LAB | Hold RMB: look | V: developer voice event");
            GUILayout.Label($"Phase: {rules.Phase} | SAN: {rules.San} | Collected: {rules.Collected.Count}/5");
            if (rules.Phase == GamePhase.Ready && GUILayout.Button("Start intro")) BeginSession();
            if ((rules.Phase == GamePhase.Intro || rules.Phase == GamePhase.Tutorial) && GUILayout.Button("Developer skip current demonstration")) {
                if (rules.Phase == GamePhase.Intro) rules.CompleteIntro(); else rules.CompleteTutorial();
                phaseSeconds = 0;
            }
            if (rules.Phase == GamePhase.Tutorial) GUILayout.Label("Listen > speak to reveal > aim for 3s > turn to station. Demo only; no practice data.");
            foreach (var group in rules.Groups) GUILayout.Label($"Variant {group.Variant + 1}: {group.Progress:P0} | gain {group.Gain:F2}");
            if (GUILayout.Button(rules.Paused ? "Resume lab" : "Pause lab")) {
                SetPaused(!rules.Paused);
            }
            if (rules.Phase == GamePhase.Return) {
                GUILayout.Label("Turn to the station behind you. Put the catcher tip near its face.");
                if (rules.StationVisited && GUILayout.Button("Confirm recycle")) ConfirmRecycle();
            }
            if (rules.Phase == GamePhase.Recycling && GUILayout.Button("Complete placeholder repair")) CompleteRepair();
            if (rules.Phase == GamePhase.Ending && GUILayout.Button("Write lab result JSON")) SaveLabResult();
            GUILayout.EndArea();
        }

        [Serializable] private sealed class LabResult { public int[] variants; public int san; }

        private void OnDestroy()
        {
            foreach (var clip in generatedClips) Destroy(clip);
        }
    }
}
