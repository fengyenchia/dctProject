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
        private const int LabRoomPoint = 2;
        public Shader revealShader;
        public AudioClip[] sourceClips = new AudioClip[9];
        public BugView groupPrefab;
        public BugView[] variantPrefabs = new BugView[0];
        public Transform[] spawnPoints = new Transform[0];
        public BoxCollider[] spawnRooms = new BoxCollider[0];
        public bool createLabGeometry = true;
        public Transform stationAnchor;
        public Collider stationZone;
        public Transform exitAnchor;
        public Transform trackingOrigin;
        public Behaviour xrMoveProvider;
        public GameObject rainEffects;
        public UnityEvent onRainStarted = new UnityEvent();
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
        public bool enableVoiceRevealPreview;
        public VariantCatalog catalog;
        public UnityEvent onCalibrationRequested = new UnityEvent();
        public UnityEvent onResetRequested = new UnityEvent();
        public bool ticketHandoffReady;
        public UnityEvent onCaptured = new UnityEvent();
        [Min(.02f)] public float maskDiameter = .25f;
        [Min(0f)] public float capturePreviewSeconds = .35f;

        private GameRules rules = new GameRules();
        private Camera labCamera;
        private Vector3 physicalCentre;
        private readonly Vector3[] points = new Vector3[6];
        private readonly Dictionary<string, GameObject> views = new Dictionary<string, GameObject>();
        private readonly Dictionary<Collider, BugState> targets = new Dictionary<Collider, BugState>();
        private readonly List<AudioClip> generatedClips = new List<AudioClip>();
        private GameObject mask;
        private GameObject exitMarker;
        private BoxCollider labRoom;
        private GameObject capturePreview;
        private float capturePreviewRemaining;
        private float yaw, pitch, phaseSeconds;
        private readonly Vector3 station = new Vector3(0f, .9f, 14f);
        public GameRules Rules => rules;
        public string CurrentTargetId { get; private set; }
        public BugState CurrentTarget => rules.Groups.FirstOrDefault(g => g.Id == CurrentTargetId);
        public bool CanInteract => !rules.Paused && poseValid && Inside;
        private Vector3 StationPosition => stationAnchor == null ? station : stationAnchor.position;
        private Vector3 ExitPosition => exitAnchor == null ? Vector3.zero : exitAnchor.position;

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
            if (spawnRooms.Length != 0 && (spawnPoints.Length != 6 || spawnRooms.Length != 6)) {
                Debug.LogError("Spawn Rooms must be empty or match the six custom Spawn Points."); enabled = false; return;
            }
            if (variantPrefabs.Length != 0 && variantPrefabs.Length != 9) {
                Debug.LogError("Variant Prefabs must be empty or have nine entries; empty entries use Group Prefab."); enabled = false; return;
            }
            if (createLabGeometry) MakeSolid("LabFloor", new Vector3(0, -.1f, 7f), new Vector3(10, .2f, 17));
            for (int i = 0; i < 6; i++) {
                float side = i % 2 == 0 ? -1f : 1f;
                float depth = 2.5f + i * 1.7f;
                points[i] = spawnPoints.Length == 6 ? spawnPoints[i].position : new Vector3(side * 3.2f, 1.3f, depth);
                if (createLabGeometry) {
                    MakeSolid("LabWall_" + i, new Vector3(side * 4.6f, 1.5f, depth), new Vector3(.2f, 3, 2.8f));
                }
            }
            if (createLabGeometry && spawnPoints.Length == 0) labRoom = CreateLabRoom();
            if (stationAnchor == null) MakeSolid("LabStation", station, new Vector3(.6f, .5f, .3f));
            if (createLabGeometry) {
                exitMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                exitMarker.name = "LabExit";
                exitMarker.transform.SetPositionAndRotation(ExitPosition + Vector3.up * .025f, Quaternion.identity);
                exitMarker.transform.localScale = new Vector3(1.5f, .025f, 1.5f);
                exitMarker.GetComponent<Collider>().enabled = false;
                exitMarker.SetActive(false);
            }
            mask = GameObject.CreatePrimitive(PrimitiveType.Quad);
            mask.name = "LabMask";
            mask.GetComponent<Collider>().enabled = false;
            var material = new Material(revealShader);
            material.SetColor("_BaseColor", new Color(.4f, .85f, 1f, .25f));
            material.SetFloat("_Visibility", 1f);
            material.SetFloat("_RoundMask", 1f);
            mask.GetComponent<Renderer>().material = material;
            mask.transform.localScale = Vector3.one * maskDiameter;
            mask.SetActive(false);
            if (rainEffects != null) rainEffects.SetActive(false);
        }

        private Transform Head => useExternalPose ? trackedHead : labCamera.transform;
        // 桌面射線與畫面中心一致；虛擬到站前端使用另一個位置，不能混作瞄準原點。
        private Vector3 AimPosition => useExternalPose ? trackedAim.position : Head.position;
        private Vector3 AimForward => useExternalPose ? trackedAim.forward : Head.forward;
        private bool Inside {
            get {
                if (Head == null) return false;
                if (!useExternalPose || trackingOrigin == null) return true;
                var local = trackingOrigin.InverseTransformPoint(Head.position) - physicalCentre;
                return new Vector2(local.x, local.z).magnitude <= 1.5f;
            }
        }

        public void SetPhysicalTrackingCentre(Transform origin, Vector3 centre)
        {
            trackingOrigin = origin;
            physicalCentre = centre;
        }

        public bool CanMove => CanInteract && !uiBusy && (rules.Phase == GamePhase.Active ||
            rules.Phase == GamePhase.Return || rules.Phase == GamePhase.Recycling || rules.Phase == GamePhase.ExitRoute);

        private void Update()
        {
            if (xrMoveProvider != null) xrMoveProvider.enabled = useExternalPose && CanMove;
            if (useExternalPose && (trackedHead == null || trackedAim == null)) return;
            if (!useExternalPose && Mouse.current != null && Mouse.current.rightButton.isPressed) {
                var delta = Mouse.current.delta.ReadValue();
                yaw += delta.x * .15f;
                pitch = Mathf.Clamp(pitch - delta.y * .15f, -75f, 75f);
                Head.rotation = Quaternion.Euler(pitch, yaw, 0);
            }
            if (!CanInteract) { CurrentTargetId = null; mask.SetActive(false); SyncViews(); return; }
            if (capturePreview != null) {
                capturePreviewRemaining -= Time.deltaTime;
                if (capturePreviewRemaining <= 0f) ClearCapturePreview();
            }
            if (!useExternalPose && CanMove && Keyboard.current != null) MoveDesktop(Keyboard.current);
            if (automaticDemos && (rules.Phase == GamePhase.Intro || rules.Phase == GamePhase.Tutorial)) {
                phaseSeconds += Time.deltaTime;
                if (rules.Phase == GamePhase.Intro && phaseSeconds >= 15f) { rules.CompleteIntro(); phaseSeconds = 0f; }
                else if (rules.Phase == GamePhase.Tutorial && phaseSeconds >= 16f) { CompleteTutorial(); phaseSeconds = 0f; }
            }
            SyncViews();
            if (rules.Phase == GamePhase.Active) {
                if (enableVoiceRevealPreview && Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
                    TriggerVoiceForDevelopment();
                CurrentTargetId = FindTargetAndMoveMask();
                var aimedGroup = CurrentTarget;
                bool wasRaining = rules.IsRaining;
                string captured = rules.Tick(Time.deltaTime, CurrentTargetId);
                if (!wasRaining && rules.IsRaining) {
                    CurrentTargetId = null;
                    if (rainEffects != null) rainEffects.SetActive(true);
                    onRainStarted.Invoke();
                }
                if (captured != null) {
                    ShowCapturePreview(captured, aimedGroup.Variant);
                    CurrentTargetId = null;
                    Debug.Log("CAPTURE_RESULT " + captured + " SAN=" + rules.San);
                    onCaptured.Invoke();
                }
                SyncViews();
            } else { CurrentTargetId = null; mask.SetActive(false); }
            if (rules.CanRecycle) {
                float headDistance = Vector2.Distance(new Vector2(Head.position.x, Head.position.z), new Vector2(StationPosition.x, StationPosition.z));
                var tip = trackedTip != null ? trackedTip.position : useExternalPose
                    ? AimPosition + AimForward * .55f : Head.position + Vector3.down * .55f + Head.forward * .55f;
                bool tipInside = stationZone == null ? Vector3.Distance(tip, StationPosition) <= .4f
                    : Vector3.Distance(stationZone.ClosestPoint(tip), tip) <= .001f;
                rules.AuthorizeStation(poseValid, Inside, headDistance <= 1.1f, tipInside);
            }
            if (rules.Phase == GamePhase.ExitRoute) {
                var offset = Head.position - ExitPosition;
                rules.ArriveAtExit(new Vector2(offset.x, offset.z).magnitude <= .75f);
            }
            if (exitMarker != null) exitMarker.SetActive(rules.Phase == GamePhase.ExitRoute);
        }

        private void MoveDesktop(Keyboard keyboard)
        {
            var input = new Vector3((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0), 0,
                (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
            if (input.sqrMagnitude == 0) return;
            var step = Quaternion.Euler(0, yaw, 0) * input.normalized * (2f * Time.deltaTime);
            if (Physics.CapsuleCast(Head.position - Vector3.up * 1.25f, Head.position - Vector3.up * .2f, .2f,
                step.normalized, step.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return;
            var next = Head.position + step;
            if (createLabGeometry) { next.x = Mathf.Clamp(next.x, -4.2f, 4.2f); next.z = Mathf.Clamp(next.z, -.6f, 15f); }
            Head.position = next;
        }

        private string FindTargetAndMoveMask()
        {
            if (uiBusy) { mask.SetActive(false); return null; }
            var ray = new Ray(AimPosition, AimForward);
            bool hitSurface = Physics.Raycast(ray, out var surface, 20f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float nearest = hitSurface ? surface.distance : 20f;
            string selected = null;
            Vector3 targetPoint = default;
            foreach (var hit in Physics.RaycastAll(ray, 20f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide)) {
                if (hit.distance > nearest || !targets.TryGetValue(hit.collider, out var group)) continue;
                var room = RoomAt(group.Point);
                if (room != null && !RoomContains(room, Head.position)) continue;
                selected = group.Id;
                nearest = hit.distance;
                targetPoint = hit.point;
            }
            mask.SetActive(selected != null || hitSurface);
            if (selected != null) {
                var facing = -ray.direction;
                mask.transform.SetPositionAndRotation(targetPoint + facing * .01f, Quaternion.LookRotation(facing));
            } else if (hitSurface) {
                mask.transform.SetPositionAndRotation(surface.point + surface.normal * .01f,
                    Quaternion.LookRotation(surface.normal));
            }
            return selected;
        }

        private BoxCollider RoomAt(int point)
        {
            if (spawnRooms.Length != 0) return spawnRooms[point];
            return point == LabRoomPoint ? labRoom : null;
        }

        private static bool RoomContains(BoxCollider room, Vector3 position)
        {
            var local = room.transform.InverseTransformPoint(position);
            return new Bounds(room.center, room.size).Contains(local);
        }

        public void TriggerVoiceForDevelopment()
        {
            if (!enableVoiceRevealPreview || !poseValid || !Inside || rules.Paused || rules.Phase != GamePhase.Active) return;
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
                go.SetActive(false);
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
                        var visual = new GameObject("Visual").transform;
                        visual.SetParent(go.transform, false);
                        AddLabBug(visual, Vector3.zero);
                        AddLabBug(visual, new Vector3(-.24f, .10f, .02f));
                        AddLabBug(visual, new Vector3(.24f, -.06f, .12f));
                        AddLabBug(visual, new Vector3(-.07f, -.21f, -.18f));
                        AddLabBug(visual, new Vector3(.10f, .22f, -.17f));
                        go.AddComponent<BugView>().visual = visual;
                    }
                    go.name = "Bug_" + group.Variant;
                    go.transform.position = points[group.Point];
                    var clip = group.Variant < sourceClips.Length ? sourceClips[group.Variant] : null;
                    go.GetComponent<BugView>().Initialize(revealShader, clip == null ? MakeTestTone(group.Variant) : clip,
                        new Color(.55f, .55f, .55f));
                    views[group.Id] = go;
                    targets[go.GetComponent<Collider>()] = group;
                }
                var room = RoomAt(group.Point);
                bool outsideRoom = room != null && !RoomContains(room, Head.position);
                bool wallBetween = outsideRoom && Physics.Linecast(Head.position, points[group.Point],
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                go.GetComponent<BugView>().Present(group, !CanInteract, outsideRoom, wallBetween);
            }
        }

        private void ShowCapturePreview(string id, int variant)
        {
            if (capturePreviewSeconds <= 0f || !views.TryGetValue(id, out var go)) return;
            ClearCapturePreview();
            views.Remove(id);
            targets.Remove(go.GetComponent<Collider>());
            go.GetComponent<BugView>().ShowCaptured(Color.HSVToRGB(variant / 9f, .7f, 1f));
            capturePreview = go;
            capturePreviewRemaining = capturePreviewSeconds;
        }

        private void ClearCapturePreview()
        {
            if (capturePreview == null) return;
            capturePreview.SetActive(false);
            Destroy(capturePreview);
            capturePreview = null;
        }

        private static void AddLabBug(Transform visual, Vector3 offset)
        {
            var bug = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bug.transform.SetParent(visual, false);
            bug.transform.localPosition = offset;
            bug.transform.localScale = Vector3.one * .27f;
            bug.GetComponent<Collider>().enabled = false;
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

        private static BoxCollider CreateLabRoom()
        {
            var house = new GameObject("LabHouse").transform;
            void AddWall(string name, Vector3 position, Vector3 size) =>
                MakeSolid(name, position, size).transform.SetParent(house, true);

            AddWall("Back", new Vector3(-4.1f, 1.5f, 5.9f), new Vector3(.2f, 3f, 2.4f));
            AddWall("SideA", new Vector3(-2.9f, 1.5f, 4.7f), new Vector3(2.4f, 3f, .2f));
            AddWall("SideB", new Vector3(-2.9f, 1.5f, 7.1f), new Vector3(2.4f, 3f, .2f));
            AddWall("DoorSideA", new Vector3(-1.7f, 1.5f, 5f), new Vector3(.2f, 3f, .6f));
            AddWall("DoorSideB", new Vector3(-1.7f, 1.5f, 6.8f), new Vector3(.2f, 3f, .6f));
            var zone = new GameObject("RoomZone").AddComponent<BoxCollider>();
            zone.transform.SetParent(house, false);
            zone.transform.localPosition = new Vector3(-2.95f, 1.5f, 5.9f);
            zone.size = new Vector3(2.5f, 3f, 2.4f);
            zone.isTrigger = true;
            return zone;
        }

        public void BeginSession() {
            if (!CanInteract || !calibrationValid || !rules.Begin()) return;
            phaseSeconds = 0f;
            if (!useExternalPose) {
                yaw = pitch = 0f;
                Head.SetPositionAndRotation(new Vector3(0, 1.6f, 0), Quaternion.identity);
            }
        }
        public bool CompleteTutorial() => rules.CompleteTutorial(labRoom != null ? LabRoomPoint : (int?)null);
        public void ConfirmRecycle() { if (rules.ConfirmRecycle(poseValid, Inside)) ClearCapturePreview(); }
        public void CompleteRepair() { rules.CompleteRepair(); }
        public void CompleteEndingVideo() { if (CanInteract) rules.CompleteEndingVideo(); }
        public void OpenThankYou() { if (CanInteract) rules.OpenThankYou(); }
        public void FinishThankYou() { if (CanInteract) rules.FinishThankYou(); }
        public void SetPaused(bool paused) { if (!paused && (!poseValid || !Inside || !calibrationValid)) return; rules.SetPaused(paused); if (mask != null) SyncViews(); }
        public bool RestoreEnding(int[] variants) {
            if (rules.Phase != GamePhase.Ready || !rules.RestoreCompleted(variants)) return false;
            SyncViews(); return true;
        }
        public void AbortSession() {
            if (rules.Phase == GamePhase.Ending) return;
            ClearCapturePreview();
            rules = new GameRules(); CurrentTargetId = null; phaseSeconds = 0; uiBusy = false;
            if (rainEffects != null) rainEffects.SetActive(false);
            if (exitMarker != null) exitMarker.SetActive(false);
            if (mask != null) mask.SetActive(false); SyncViews();
        }
        // 僅由 I2 在「本機保存＋結尾＋交票」全部確認後呼叫。
        public bool ResetForNextSession()
        {
            if (rules.Phase != GamePhase.Ending) return false;
            ClearCapturePreview();
            foreach (var go in views.Values) go.GetComponent<AudioSource>()?.Stop();
            rules = new GameRules();
            if (rainEffects != null) rainEffects.SetActive(false);
            if (exitMarker != null) exitMarker.SetActive(false);
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
            GUILayout.Label("DESKTOP LAB | RMB: look | WASD: move");
            GUILayout.Label($"Phase: {rules.Phase} | SAN: {rules.San} | Collected: {rules.Collected.Count}");
            if (rules.Phase == GamePhase.Active) GUILayout.Label($"Weather: {(rules.IsRaining ? "rain" : "dry")} | Active: {rules.ExplorationSeconds:F0}s");
            if (rules.Phase == GamePhase.Ready && GUILayout.Button("Start intro")) BeginSession();
            if ((rules.Phase == GamePhase.Intro || rules.Phase == GamePhase.Tutorial) && GUILayout.Button("Developer skip current demonstration")) {
                if (rules.Phase == GamePhase.Intro) rules.CompleteIntro(); else CompleteTutorial();
                phaseSeconds = 0;
            }
            if (rules.Phase == GamePhase.Tutorial) GUILayout.Label("Listen > aim for 3s > walk to station > return to entrance. Demo only.");
            foreach (var group in rules.Groups) GUILayout.Label($"Variant {group.Variant + 1}: {group.Progress:P0} | gain {group.Gain:F2}");
            if (GUILayout.Button(rules.Paused ? "Resume lab" : "Pause lab")) {
                SetPaused(!rules.Paused);
            }
            if (rules.CanRecycle) {
                GUILayout.Label("Reach the far end of the street and aim the tip at the station.");
                if (rules.StationVisited && GUILayout.Button("Confirm recycle")) ConfirmRecycle();
            }
            if (rules.Phase == GamePhase.Recycling && GUILayout.Button("Complete placeholder repair")) CompleteRepair();
            if (rules.Phase == GamePhase.ExitRoute) GUILayout.Label("Return to the entrance (start of the street).");
            if (rules.Phase == GamePhase.EndingVideo && GUILayout.Button("Complete placeholder ending video")) CompleteEndingVideo();
            if (rules.Phase == GamePhase.ThankYou) {
                if (!rules.LetterOpened && GUILayout.Button("Open thank-you letter")) OpenThankYou();
                if (rules.LetterOpened) {
                    GUILayout.Label($"Thank-you letter version: {rules.LetterTier} | Collected: {rules.Collected.Count}");
                    if (GUILayout.Button("Finish experience")) FinishThankYou();
                }
            }
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
