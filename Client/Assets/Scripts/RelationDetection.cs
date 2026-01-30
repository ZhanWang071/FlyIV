using UnityEngine;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;

public class RelationDetection : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Used to get object recognition data from VLM")]
    [SerializeField] private VLMFocus vlmHandler;
    // [Removed] CameraInfo reference is no longer needed

    [Header("User Settings (Merged from CameraInfo)")]
    public string userName = "User";
    public Vector3 userBodySize = new Vector3(0.5f, 1.7f, 0.3f); // 宽, 高, 深

    [Header("Settings - Vertical")]
    [Tooltip("Threshold for vertical overlap (0.0 ~ 1.0)")]
    [SerializeField] private float verticalOverlapThreshold = 0.2f;
    [Tooltip("Minimum height difference for Above/Below (m)")]
    [SerializeField] private float verticalHeightDiff = 0.1f;

    [Header("Settings - Horizontal (Paper Strict)")]
    [Tooltip("Max effective distance for horizontal relationships (d_max)")]
    [SerializeField] private float maxHorizontalDistance = 4.0f;

    [Tooltip("Horizontal field of view angle threshold (Theta, degrees). Recommended 45~60")]
    [Range(10, 90)]
    [SerializeField] private float viewAngleThreshold = 60.0f;

    [Header("Settings - Proximity")]
    [SerializeField] private float nearDistance = 1.5f;

    [Header("Result Display")]
    [TextArea(10, 20)] public string relationDataJSON;

    private static readonly object _logLock = new object();

    // --- Data Structures ---
    [Serializable]
    public class Vector3Data
    {
        public float x, y, z;
        public Vector3Data() { }
        public Vector3Data(float x, float y, float z)
        {
            this.x = (float)Math.Round(x, 2);
            this.y = (float)Math.Round(y, 2);
            this.z = (float)Math.Round(z, 2);
        }
        public Vector3Data(Vector3 v) : this(v.x, v.y, v.z) { }
        public Vector3 ToUnityVec() => new Vector3(x, y, z);
    }

    [Serializable]
    public class BoundaryData
    {
        public Vector3Data center;
        public Vector3Data size;
        public Vector3Data forward;
        public Vector3Data right;
        public Vector3Data up;
    }

    [Serializable]
    public class ObjectNode
    {
        public string name;
        public Vector3Data position;
        public Vector3Data scale;
        public BoundaryData boundary;
    }

    [Serializable]
    public class RelationOutput
    {
        public string @object;
        public string target;
        public string relation;
    }

    // --- Lifecycle Methods ---

    void Start()
    {
        if (vlmHandler == null) vlmHandler = FindFirstObjectByType<VLMFocus>();

        if (vlmHandler != null)
        {
            vlmHandler.OnVLMFocusFinished += OnVLMFocusFinished;
        }
    }

    void OnDestroy()
    {
        if (vlmHandler != null)
        {
            vlmHandler.OnVLMFocusFinished -= OnVLMFocusFinished;
        }
    }

    private void OnVLMFocusFinished(string objectsDataJSON)
    {
        if (string.IsNullOrEmpty(objectsDataJSON)) return;

        string finalInputJson = objectsDataJSON;
        string userNodeJson = null;

        try
        {
            List<ObjectNode> nodeList = JsonConvert.DeserializeObject<List<ObjectNode>>(objectsDataJSON);
            if (nodeList == null) nodeList = new List<ObjectNode>();

            // Directly call local method
            ObjectNode userNode = GetCameraNode();
            if (userNode != null)
            {
                userNodeJson = JsonConvert.SerializeObject(userNode, Formatting.Indented);
                nodeList.Add(userNode);
                Debug.Log($"[RelationDetection] User node merged: {userNode.name}");
            }

            finalInputJson = JsonConvert.SerializeObject(nodeList, Formatting.Indented);
        }
        catch (Exception e)
        {
            Debug.LogError($"[RelationDetection] Failed to merge User node: {e.Message}");
        }

        string result = GetRelationData(finalInputJson);
        LogRelationData(result, userNodeJson);
    }

    // --- User Node Generation (Merged from CameraInfo) ---

    public ObjectNode GetCameraNode()
    {
        Camera cam = Camera.main;
        if (cam == null) return null;
        Transform t = cam.transform;

        // 1. 计算关键点
        // 假设摄像机位置 (t.position) 就是用户的 "头顶/眼睛" 位置
        // Position (Top Center Anchor): 直接就是摄像机位置
        Vector3 topPosition = t.position;

        // Boundary Center (几何中心): 从头顶向下移动身高的一半
        Vector3 bodyCenter = t.position - (Vector3.up * userBodySize.y * 0.5f);

        // 2. 构建节点
        ObjectNode userNode = new ObjectNode
        {
            name = userName,
            position = new Vector3Data(topPosition.x, topPosition.y, topPosition.z),
            scale = new Vector3Data(1f, 1f, 1f),
            boundary = new BoundaryData
            {
                center = new Vector3Data(bodyCenter.x, bodyCenter.y, bodyCenter.z),
                size = new Vector3Data(userBodySize.x, userBodySize.y, userBodySize.z),
                forward = new Vector3Data(t.forward.x, t.forward.y, t.forward.z),
                right = new Vector3Data(t.right.x, t.right.y, t.right.z),
                up = new Vector3Data(t.up.x, t.up.y, t.up.z)
            }
        };

        return userNode;
    }

    // --- Core Logic ---

    public string GetRelationData(string jsonInput)
    {
        if (string.IsNullOrEmpty(jsonInput)) return "[]";

        List<ObjectNode> nodes;
        try
        {
            nodes = JsonConvert.DeserializeObject<List<ObjectNode>>(jsonInput);
        }
        catch (Exception e)
        {
            Debug.LogError($"[RelationDetection] JSON Parse Error: {e.Message}");
            return "[]";
        }

        if (nodes == null || nodes.Count < 2) return "[]";

        List<RelationOutput> relations = new List<RelationOutput>();

        // 获取 User 位置和 Forward 方向
        Vector3? userPos = null;
        Vector3? userFwd = null;

        var camNode = GetCameraNode();
        if (camNode != null)
        {
            userPos = camNode.position.ToUnityVec();
            userFwd = camNode.boundary.forward.ToUnityVec();
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            for (int j = 0; j < nodes.Count; j++)
            {
                if (i == j) continue;

                ObjectNode subject = nodes[i];
                ObjectNode target = nodes[j];

                // 传入 User Pos 和 User Fwd
                string rel = ComputeRelation(subject, target, userPos, userFwd, this.userName);

                if (rel != "unrelated")
                {
                    relations.Add(new RelationOutput
                    {
                        @object = subject.name,
                        target = target.name,
                        relation = rel
                    });
                }
            }
        }

        relationDataJSON = JsonConvert.SerializeObject(relations, Formatting.Indented);
        return relationDataJSON;
    }

    // --- Geometric Logic ---

    /// <summary>
    /// 根据检测平面的“基准方向” (baselineDir)，判断应该使用物体的 Width(X) 还是 Depth(Z)。
    /// </summary>
    private float GetProjectedSize(ObjectNode obj, Vector3 baselineDir)
    {
        Vector3 objRight = obj.boundary.right.ToUnityVec();
        Vector3 objFwd = obj.boundary.forward.ToUnityVec();

        objRight.y = 0; objRight.Normalize();
        objFwd.y = 0; objFwd.Normalize();
        baselineDir.y = 0; baselineDir.Normalize();

        float dotX = Mathf.Abs(Vector3.Dot(objRight, baselineDir));
        float dotZ = Mathf.Abs(Vector3.Dot(objFwd, baselineDir));

        return (dotX > dotZ) ? obj.boundary.size.x : obj.boundary.size.z;
    }

    // [Logic Updated Here]
    private string ComputeRelation(ObjectNode objA, ObjectNode objB, Vector3? userPos, Vector3? userFwd, string currentUserName)
    {
        Vector3 posA = objA.boundary.center.ToUnityVec();
        Vector3 posB = objB.boundary.center.ToUnityVec();

        Vector3 sizeA = objA.boundary.size.ToUnityVec();
        Vector3 sizeB = objB.boundary.size.ToUnityVec();

        Bounds boundsA = new Bounds(objA.boundary.center.ToUnityVec(), objA.boundary.size.ToUnityVec());
        Bounds boundsB = new Bounds(objB.boundary.center.ToUnityVec(), objB.boundary.size.ToUnityVec());

        // 1. Vertical Logic (Unchanged)
        if (CheckHorizontalOverlapWithThreshold(boundsA, boundsB))
        {
            float yDiff = posA.y - posB.y;

            float yDiff_above = (posA.y - sizeA.y / 2) - (posB.y + sizeB.y / 2);
            if (yDiff > 0 && yDiff_above < verticalHeightDiff)
            {
                return "above";
            }

            float yDiff_below = (posB.y - sizeB.y / 2) - (posA.y + sizeA.y / 2);
            if (yDiff < 0 && yDiff_below < verticalHeightDiff)
            {
                return "below";
            }
        }

        // 2. Horizontal Logic (Refined)
        Vector3 forwardForFB;
        Vector3 rightForFB;

        Vector3 forwardForLR;
        Vector3 rightForLR;

        // 判定物体对中是否包含 User
        bool pairHasUser = (objA.name == currentUserName || objB.name == currentUserName);

        // Case A: 纯物体对 (没有 User) -> 强制使用 User 的视角 (User-Centric)
        // Case B: 包含 User -> 使用物体自身的固有方向 (Intrinsic)，即 target 的 forward
        if (!pairHasUser && userFwd.HasValue)
        {
            // Case A: 纯物体对 (没有 User) -> 强制使用 User 的视角 (User-Centric)
            Vector3 uFwd = userFwd.Value;
            uFwd.y = 0; uFwd.Normalize();

            // --- 规则 1: 前后判定 (相对于 User) ---
            // "In Front Of B" 意味着物体在 B 和 User 之间 (Facing User)
            forwardForFB = -uFwd;
            rightForFB = Vector3.Cross(Vector3.up, forwardForFB).normalized;

            // --- 规则 2: 左右判定 (相对于 User) ---
            // "Right of B" 意味着在 User 视野的右侧
            forwardForLR = uFwd;
            rightForLR = Vector3.Cross(Vector3.up, forwardForLR).normalized;
        }
        else
        {
            // Case B: 包含 User，或者场景中根本没有 User 数据
            // 使用 Target (objB) 自身的固有 Forward
            Vector3 objFwd = objB.boundary.forward.ToUnityVec();
            objFwd.y = 0; objFwd.Normalize();

            forwardForFB = objFwd;
            rightForFB = Vector3.Cross(Vector3.up, forwardForFB).normalized;

            forwardForLR = objFwd;
            rightForLR = rightForFB;
        }

        Vector3 centerB = posB;
        Vector3 flatPosA = new Vector3(posA.x, 0, posA.z);
        Vector3 flatCenterB = new Vector3(centerB.x, 0, centerB.z);

        // --- Check Front / Behind ---
        float widthForFB = GetProjectedSize(objB, rightForFB);

        if (CheckSpatialRelation(flatPosA, flatCenterB, forwardForFB, rightForFB, widthForFB, maxHorizontalDistance, viewAngleThreshold, "front"))
            return "in front of";

        if (CheckSpatialRelation(flatPosA, flatCenterB, -forwardForFB, -rightForFB, widthForFB, maxHorizontalDistance, viewAngleThreshold, "behind"))
            return "behind";

        // --- Check Right / Left ---
        float widthForLR = GetProjectedSize(objB, forwardForLR);

        if (CheckSpatialRelation(flatPosA, flatCenterB, rightForLR, forwardForLR, widthForLR, maxHorizontalDistance, viewAngleThreshold, "right"))
            return "right";

        if (CheckSpatialRelation(flatPosA, flatCenterB, -rightForLR, -forwardForLR, widthForLR, maxHorizontalDistance, viewAngleThreshold, "left"))
            return "left";

        // 3. Proximity
        float dist3D = Vector3.Distance(posA, posB);
        if (dist3D < nearDistance) return "near";

        return "unrelated";
    }

    private bool CheckSpatialRelation(Vector3 targetPos, Vector3 refPos, Vector3 direction, Vector3 baselineDir, float baselineLength, float d_max, float theta, string relation)
    {
        Vector3 vecToTarget = targetPos - refPos;
        float projectedDist = Vector3.Dot(vecToTarget, direction);

        if (projectedDist <= 0) return false;
        if (projectedDist > d_max) return false;

        float halfWidth = baselineLength * 0.5f;
        Vector3 PointB = refPos - (baselineDir * halfWidth);
        Vector3 PointC = refPos + (baselineDir * halfWidth);

        Vector3 vecB_Target = targetPos - PointB;
        Vector3 vecC_Target = targetPos - PointC;

        float angleB = Vector3.Angle(vecB_Target, baselineDir);
        float angleC = Vector3.Angle(vecC_Target, -baselineDir);

        float angleVL = theta / 2 + 90.0f;

        if (angleB >= angleVL) return false;
        if (angleC >= angleVL) return false;

        return true;
    }

    private bool CheckHorizontalOverlapWithThreshold(Bounds a, Bounds b)
    {
        float interMinX = Mathf.Max(a.min.x, b.min.x);
        float interMaxX = Mathf.Min(a.max.x, b.max.x);
        float interMinZ = Mathf.Max(a.min.z, b.min.z);
        float interMaxZ = Mathf.Min(a.max.z, b.max.z);

        float interWidth = interMaxX - interMinX;
        float interDepth = interMaxZ - interMinZ;

        if (interWidth <= 0 || interDepth <= 0) return false;

        float intersectionArea = interWidth * interDepth;
        float areaA = (a.max.x - a.min.x) * (a.max.z - a.min.z);
        float areaB = (b.max.x - b.min.x) * (b.max.z - b.min.z);
        float minArea = Mathf.Min(areaA, areaB);

        if (minArea <= 0) return false;

        return (intersectionArea / minArea) > verticalOverlapThreshold;
    }

    // --- Logging & Testing ---

    [ContextMenu("Run Test From Inspector")]
    public void RunTest()
    {
        if (vlmHandler == null || string.IsNullOrEmpty(vlmHandler.objectsDataDisplay))
        {
            Debug.LogWarning("VLM Data is empty.");
            return;
        }

        string inputToUse = vlmHandler.objectsDataDisplay;
        string userNodeJson = null;

        try
        {
            var nodes = JsonConvert.DeserializeObject<List<ObjectNode>>(inputToUse);
            // Use local method
            var userNode = GetCameraNode();
            userNodeJson = JsonConvert.SerializeObject(userNode, Formatting.Indented);
            nodes.Add(userNode);
            inputToUse = JsonConvert.SerializeObject(nodes, Formatting.Indented);
        }
        catch (Exception e)
        {
            Debug.LogError($"Test Run Error: {e.Message}");
        }

        string outputJson = GetRelationData(inputToUse);
        LogRelationData(outputJson, userNodeJson);
        Debug.Log($"<color=cyan>[Test Output]</color>:\n{outputJson}");
    }

    private void LogRelationData(string resultJson, string userNodeJson = null)
    {
        if (string.IsNullOrEmpty(resultJson) || vlmHandler == null) return;

        string logFilePath = vlmHandler.GetCurrentLogFilePath();

        lock (_logLock)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine($"\n=== Relation Detection Phase [{DateTime.Now:HH:mm:ss}] ===");

                    if (!string.IsNullOrEmpty(userNodeJson))
                    {
                        writer.WriteLine("--- User/Camera Node Info ---");
                        writer.WriteLine(userNodeJson);
                    }

                    writer.WriteLine("--- Calculated Relations ---");
                    writer.WriteLine(resultJson);
                    writer.WriteLine("==================================================");
                }
                Debug.Log($"[RelationDetection] relation构建完成，记录到Log文件");
            }
            catch (Exception e)
            {
                Debug.LogError($"[RelationDetection] Log Write Failed: {e.Message}");
            }
        }
    }
}