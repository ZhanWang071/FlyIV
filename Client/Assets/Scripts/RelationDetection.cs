using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class RelationDetection : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Used to get object recognition data from VLM")]
    [SerializeField] private VLMFocus vlmHandler;

    [Header("User Settings")]
    public Vector3 userBodySize = new Vector3(0.5f, 1.7f, 0.3f); // 宽, 高, 深
    
    [Header("Settings - Vertical")]
    [Tooltip("判定垂直重叠的阈值：重叠面积占较小物体底面积的百分比 (0.0 ~ 1.0)")]
    [SerializeField] private float verticalOverlapThreshold = 0.2f;
    [Tooltip("判定为 Above/Below 的最小高度差 (m)")]
    [SerializeField] private float verticalHeightDiff = 0.2f;

    [Header("Settings - Horizontal")]
    [Tooltip("水平关系的有效距离 (对应论文中的 d_max)")]
    [SerializeField] private float maxHorizontalDistance = 4f; // d_max

    [Tooltip("水平视野夹角阈值 (对应论文中的 Theta, 单位: 度)。建议 45~60 度")]
    [Range(10, 90)]
    [SerializeField] private float viewAngleThreshold = 60.0f; // Theta

    [Header("Settings - Proximity")]
    [SerializeField] private float nearDistance = 2f;
    // [SerializeField] private float farDistance = 2f;

    [Header("Result Display")]
    [Tooltip("这里会实时显示最近一次的关系检测结果")]
    [TextArea(10, 20)] public string relationDataJSON;
    [TextArea(10, 20)] public string sceneGraphJSON;
    private SceneGraph _currentSceneGraph;

    [SerializeField] private bool logToFile = false; // 是否将关系检测结果记录到日志文件

    private static readonly object _logLock = new object();
    // --- 数据结构定义 ---

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
        public Vector3Data position; // Top Center Anchor
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

    [Serializable]
    public class SceneGraph
    {
        public List<ObjectNode> objects;   // 原始物体属性数据
        public List<RelationOutput> relations; // 计算出的空间关系
    }

    // --- 生命周期方法 ---

    void Start()
    {
        // 如果没有手动指定VLMFocus引用，尝试在场景中查找
        if (vlmHandler == null)
        {
            vlmHandler = FindFirstObjectByType<VLMFocus>();
        }
        
        // 如果找到了VLMFocus组件，订阅事件
        if (vlmHandler != null)
        {
            vlmHandler.OnVLMFocusFinished += OnVLMFocusFinished;
        }
        else
        {
            Debug.LogWarning("[RelationDetection] 未找到VLMFocus组件，关系检测功能无法使用");
        }
    }

    void OnDestroy()
    {
        if (vlmHandler != null && vlmHandler.OnVLMFocusFinished != null)
        {
            vlmHandler.OnVLMFocusFinished -= OnVLMFocusFinished;
        }
    }

    /// <summary>
    /// VLM检测完成时的回调函数
    /// </summary>
    private void OnVLMFocusFinished(string objectsDataJSON)
    {
        if (string.IsNullOrEmpty(objectsDataJSON))
        {
            Debug.Log("[RelationDetection] 收到VLM检测结果数据为空");
            return;
        }

        string finalInputJson = objectsDataJSON;
        string userNodeJson = null;

        // 加入user node
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
                Debug.Log($"[RelationDetection] 成功加入user node");
            }

            // finalInputJson = JsonConvert.SerializeObject(nodeList, Formatting.Indented);

            // 3. 计算关系 (传入包含 User 的新列表)
            // 我们重构一下 GetRelationData 的内部逻辑，使其返回 List 而不是 JSON 字符串
            List<RelationOutput> relationList = GetRelationData(nodeList);

            // 4. 构建 Scene Graph
            _currentSceneGraph = new SceneGraph
            {
                objects = nodeList,
                relations = relationList
            };

            // 5. 序列化为最终的 JSON
            sceneGraphJSON = JsonConvert.SerializeObject(_currentSceneGraph, Formatting.Indented);

            if (logToFile) LogRelationData(sceneGraphJSON);
        }
        catch (Exception e)
        {
            Debug.LogError($"[RelationDetection] 合并user节点失败: {e.Message}");
        }
        
        // // 自动执行关系检测
        // string result = GetRelationData(objectsDataJSON);

        // Debug.Log($"[RelationDetection] 关系检测完成，记录到Log文件");
        // if (logToFile) LogRelationData(result);  
    }

    // --- User Node Generation ---

    public ObjectNode GetCameraNode()
    {
        Transform t = Camera.main.transform;

        // 1. 计算关键点
        // 假设摄像机位置 (t.position) 就是用户的 "头顶/眼睛" 位置
        // Position (Top Center Anchor): 直接就是摄像机位置
        Vector3 topPosition = t.position;

        // Boundary Center (几何中心): 从头顶向下移动身高的一半
        Vector3 bodyCenter = t.position - (Vector3.up * userBodySize.y * 0.5f);

        // 2. 构建节点
        ObjectNode userNode = new ObjectNode
        {
            name = "User",
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

    public List<RelationOutput> GetRelationData(List<ObjectNode> nodes)
    {
        List<RelationOutput> relations = new List<RelationOutput>();

        if (nodes == null || nodes.Count < 2) return relations;

        // 获取 User 位置和 Forward 方向, add user node
        Vector3? userFwd = null;

        var camNode = GetCameraNode();
        if (camNode != null)
        {
            userFwd = camNode.boundary.forward.ToUnityVec();
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            for (int j = 0; j < nodes.Count; j++)
            {
                if (i == j) continue;

                ObjectNode subject = nodes[i];
                ObjectNode target = nodes[j];

                string rel = ComputeRelation(subject, target, userFwd);

                if (rel != null)
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

        // relationDataJSON = JsonConvert.SerializeObject(relations, Formatting.Indented);  

        return relations;
    }

    public object GetSceneGraphData()
    {
        return _currentSceneGraph;
    }

    // --- 几何计算逻辑 ---


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

    private string ComputeRelation(ObjectNode objA, ObjectNode objB, Vector3? userFwd)
    {
        // 1. 准备数据：基于几何中心 (boundary.center)
        Vector3 posA = objA.boundary.center.ToUnityVec(); // Subject (Target in logic)
        Vector3 posB = objB.boundary.center.ToUnityVec(); // Reference Object

        Vector3 sizeA = objA.boundary.size.ToUnityVec();
        Vector3 sizeB = objB.boundary.size.ToUnityVec();


        Bounds boundsA = new Bounds(objA.boundary.center.ToUnityVec(), objA.boundary.size.ToUnityVec());
        Bounds boundsB = new Bounds(objB.boundary.center.ToUnityVec(), objB.boundary.size.ToUnityVec());

        // --- Step 1: 垂直关系 (Vertical) ---
        // 先检查垂直投影重叠，再看高度差
        if (CheckHorizontalOverlapWithThreshold(boundsA, boundsB))
        {
            float yDiff = posA.y - posB.y;
            // 论文公式: |h(oi) - h(oj)| < dv (这里逻辑反过来，大于dv才算方位关系)
            if (Mathf.Abs(yDiff) > verticalHeightDiff)
            {
                return yDiff > 0 ? "above" : "below";
            }
        }

        // --- Step 2: 水平关系 (Horizontal - Rigorous) ---
        // 如果无垂直关系，进入水平判定。
        // 需要遍历四个方向，检查物体 A 是否落在物体 B 的特定几何区域内。

        // 获取 B 的局部坐标轴
        Vector3 forwardForFB;
        Vector3 rightForFB;

        Vector3 forwardForLR;
        Vector3 rightForLR;

        // 判定物体对中是否包含 User
        bool pairHasUser = (objA.name == "User" || objB.name == "User");

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

        // 检查 "In Front Of" (正前方)
        if (CheckSpatialRelation(flatPosA, flatCenterB, forwardForFB, rightForFB, widthForFB, maxHorizontalDistance, viewAngleThreshold))
            return "in front of";

        if (CheckSpatialRelation(flatPosA, flatCenterB, -forwardForFB, -rightForFB, widthForFB, maxHorizontalDistance, viewAngleThreshold))
            return "behind";

        // --- Check Right / Left ---
        float widthForLR = GetProjectedSize(objB, forwardForLR);

        if (CheckSpatialRelation(flatPosA, flatCenterB, rightForLR, forwardForLR, widthForLR, maxHorizontalDistance, viewAngleThreshold))
            return "right";

        if (CheckSpatialRelation(flatPosA, flatCenterB, -rightForLR, -forwardForLR, widthForLR, maxHorizontalDistance, viewAngleThreshold))
            return "left";

        // --- Step 3: 邻近关系 (Proximity) ---
        // 最后检查距离
        float dist3D = Vector3.Distance(posA, posB);
        if (dist3D < nearDistance) return "near";
        // else if (dist3D < farDistance) return "far";

        return null;
    }

    /// <summary>
    /// 空间关系判定
    /// </summary>
    /// <param name="targetPos">目标物体位置 (A)</param>
    /// <param name="refPos">参考物体中心 (B)</param>
    /// <param name="direction">检测的主方向 (如 Forward)</param>
    /// <param name="baselineDir">定义宽度的方向 (如 Right)</param>
    /// <param name="baselineLength">物体的宽度/深度 (对应 la)</param>
    /// <param name="d_max">最大有效距离</param>
    /// <param name="theta">角度阈值 (度)</param>
    private bool CheckSpatialRelation(Vector3 targetPos, Vector3 refPos, Vector3 direction, Vector3 baselineDir, float baselineLength, float d_max, float theta)
    {
        Vector3 vecToTarget = targetPos - refPos;

        // 1. 距离检查 (Distance Check)
        // 投影距离：目标在主方向上的投影长度
        // 论文公式: Y(Cj) <= d_max + lb/2 
        // 这里简化为：投影距离 <= maxDistance
        float projectedDist = Vector3.Dot(vecToTarget, direction);
        if (projectedDist <= 0 || projectedDist > d_max) return false;

        // 2. 角度/区域检查 (Direction Check - The Trapezoid Logic)
        // 论文公式: <CjBC < Theta 且 <CjCB < Theta
        // 我们需要构建基准线段 BC (即物体面向该方向的两个“肩膀”端点)

        float halfWidth = baselineLength * 0.5f;
        Vector3 PointB = refPos - (baselineDir * halfWidth); // 左端点 (相对于检测方向)
        Vector3 PointC = refPos + (baselineDir * halfWidth); // 右端点

        // 计算目标相对于端点的向量
        Vector3 vecB_Target = targetPos - PointB;
        Vector3 vecC_Target = targetPos - PointC;

        // 计算基准线向量 (用于计算夹角)
        // 角度相对于“前方”的视线夹角。
        // 计算 Vector(B->Target) 与 Vector(B->C) 的夹角
        // 几何意义：目标必须在以 B 和 C 为底、张角为 Theta 的两个圆锥的交集内。

        // 使用 direction 作为主轴。
        // Angle(B->Target, direction) < theta
        // Angle(C->Target, direction) < theta
        // 目标必须同时满足两个端点的“视野圆锥”。

        float angleB = Vector3.Angle(vecB_Target, direction);
        float angleC = Vector3.Angle(vecC_Target, direction);

        // 如果两个端点的视线夹角都小于阈值，说明物体完全在梯形/扇形通道内
        float angleVL = theta / 2 + 90.0f;

        if (angleB >= angleVL) return false;
        if (angleC >= angleVL) return false;

        return true;
    }

    private bool CheckHorizontalOverlapWithThreshold(Bounds a, Bounds b)
    {
        // 保持原有的重叠检测逻辑 (符合论文 S(Intersection) > tc * min(S1, S2))
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

    // --- 独立测试代码 ---

    // [ContextMenu("Get Relation Data")]
    // public void RunTest()
    // {
    //     string inputJson = vlmHandler.objectsDataDisplay;
    //     string outputJson = JsonConvert.SerializeObject(GetRelationData(inputJson));
    //     if (logToFile) LogRelationData(outputJson);
    // }

    /// <summary>
    /// 记录关系检测结果到日志文件
    /// </summary>
    private void LogRelationData(string relationDataJSON)
    {
        if (string.IsNullOrEmpty(relationDataJSON))
        {
            return;
        }

        string logFilePath = vlmHandler.GetCurrentLogFilePath();
        lock (_logLock)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine($"--- Relation Detection Result ---");
                    writer.WriteLine(relationDataJSON);
                    writer.WriteLine();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[RelationDetection] 写入日志失败: {e.Message}");
            }
        }
    }
}