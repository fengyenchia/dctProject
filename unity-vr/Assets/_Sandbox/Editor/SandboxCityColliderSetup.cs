// 只用於 _Sandbox 的城市測試，不提交到 repo（_Sandbox 已列在本機 .git/info/exclude）。
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Sandbox.EditorTools
{
    /// <summary>
    /// 匯入 Assets/_Sandbox/City/ 底下、檔名含 "Collision" 的城市 FBX 時自動處理：
    /// 1. 固定匯入設定：公尺單位、Bake Axis Conversion、不自動替每個零件產生碰撞。
    /// 2. 名稱含 "_COL_" 的物件：加 MeshCollider，移除 MeshRenderer（變成隱形碰撞）。
    /// 3. 地面與各棟一樓地板：加 MeshCollider（保持可見），補上美術碰撞模型缺少的地板。
    /// 處理結果會印在 Console，開頭是 [SandboxCity]。
    /// </summary>
    public class SandboxCityColliderSetup : AssetPostprocessor
    {
        const string TargetFolder = "Assets/_Sandbox/City/";
        const string CollisionTag = "_COL_";

        // 補地板碰撞的物件：整片地面，以及 A～E 棟的一樓地板。
        static readonly Regex FloorName =
            new Regex(@"^(V02_Material_01_Ground|MCP_[A-E]_Ground_Slab(_Main|_Wing)?)$");

        // 修改這個腳本的規則時，把數字加 1，Unity 會重新匯入這些 FBX。
        public override uint GetVersion() => 1;

        bool IsTarget()
        {
            return assetPath.StartsWith(TargetFolder)
                && assetPath.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)
                && Path.GetFileName(assetPath).Contains("Collision");
        }

        void OnPreprocessModel()
        {
            if (!IsTarget()) return;

            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.useFileUnits = true;
            importer.bakeAxisConversion = true;
            importer.addCollider = false;
        }

        void OnPostprocessModel(GameObject root)
        {
            if (!IsTarget()) return;

            int hiddenColliders = 0;
            int floorColliders = 0;
            var skipped = new List<string>();

            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                bool isCollisionMesh = t.name.Contains(CollisionTag);
                bool isFloor = FloorName.IsMatch(t.name);
                if (!isCollisionMesh && !isFloor) continue;

                var filter = t.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                {
                    skipped.Add(t.name);
                    continue;
                }

                var meshCollider = t.GetComponent<MeshCollider>();
                if (meshCollider == null) meshCollider = t.gameObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = filter.sharedMesh;

                if (isCollisionMesh)
                {
                    var meshRenderer = t.GetComponent<MeshRenderer>();
                    if (meshRenderer != null) Object.DestroyImmediate(meshRenderer, true);
                    hiddenColliders++;
                }
                else
                {
                    floorColliders++;
                }
            }

            Debug.Log($"[SandboxCity] {Path.GetFileName(assetPath)}：隱形碰撞 {hiddenColliders} 個，地板碰撞 {floorColliders} 個。");
            if (skipped.Count > 0)
                Debug.LogWarning($"[SandboxCity] 找不到網格、已略過：{string.Join(", ", skipped)}");
        }
    }
}
