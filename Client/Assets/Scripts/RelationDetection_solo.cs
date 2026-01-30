using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

public class RelationDetection_solo : MonoBehaviour
{
    [Header("Settings - Vertical")]
    [Tooltip("判定垂直重叠的阈值：重叠面积占较小物体底面积的百分比 (0.0 ~ 1.0)")]
    public float verticalOverlapThreshold = 0.2f;
    [Tooltip("判定为 Above/Below 的最小高度差 (m)")]
    public float verticalHeightDiff = 0.1f;

    [Header("Settings - Horizontal (Paper Strict)")]
    [Tooltip("水平关系的有效距离 (对应论文中的 d_max)")]
    public float maxHorizontalDistance = 4.0f; // d_max

    [Tooltip("水平视野夹角阈值 (对应论文中的 Theta, 单位: 度)。建议 45~60 度")]
    [Range(10, 90)]
    public float viewAngleThreshold = 60.0f; // Theta

    [Header("Settings - Proximity")]
    public float nearDistance = 0.5f;
    public float farDistance = 2f;

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

    // --- 核心接口 ---

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
            Debug.LogError($"[RelationDetection] JSON 解析错误: {e.Message}");
            return "[]";
        }

        if (nodes == null || nodes.Count < 2) return "[]";

        List<RelationOutput> relations = new List<RelationOutput>();

        for (int i = 0; i < nodes.Count; i++)
        {
            for (int j = 0; j < nodes.Count; j++)
            {
                if (i == j) continue;

                ObjectNode subject = nodes[i];
                ObjectNode target = nodes[j];

                string rel = ComputeRelation(subject, target);

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

        return JsonConvert.SerializeObject(relations, Formatting.Indented);
    }

    // --- 几何计算逻辑 ---

    private string ComputeRelation(ObjectNode objA, ObjectNode objB)
    {
        // 1. 准备数据：基于几何中心 (boundary.center)
        Vector3 posA = objA.boundary.center.ToUnityVec(); // Subject (Target in logic)
        Vector3 posB = objB.boundary.center.ToUnityVec(); // Reference Object

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
        Vector3 forwardB = objB.boundary.forward.ToUnityVec().normalized;
        Vector3 rightB = objB.boundary.right.ToUnityVec().normalized;
        Vector3 centerB = posB;
        Vector3 sizeB = objB.boundary.size.ToUnityVec();

        // 拍扁向量到水平面 (XZ Plane) - 论文要求在投影平面计算
        Vector3 flatPosA = new Vector3(posA.x, 0, posA.z);
        Vector3 flatCenterB = new Vector3(centerB.x, 0, centerB.z);
        Vector3 flatForwardB = new Vector3(forwardB.x, 0, forwardB.z).normalized;
        Vector3 flatRightB = new Vector3(rightB.x, 0, rightB.z).normalized;

        // 检查 "In Front Of" (正前方)
        if (CheckSpatialRelation(flatPosA, flatCenterB, flatForwardB, flatRightB, sizeB.x, maxHorizontalDistance, viewAngleThreshold))
            return "in front of";

        // 检查 "Behind" (正后方) -> 方向是 -Forward, 基准线仍然是 Right (Width)
        if (CheckSpatialRelation(flatPosA, flatCenterB, -flatForwardB, flatRightB, sizeB.x, maxHorizontalDistance, viewAngleThreshold))
            return "behind";

        // 检查 "Right" (右侧) -> 方向是 Right, 基准线变成 Forward (Depth)
        // 注意：CheckSpatialRelation 的参数顺序：方向向量，正交向量(基准线方向)，基准线长度
        if (CheckSpatialRelation(flatPosA, flatCenterB, flatRightB, flatForwardB, sizeB.z, maxHorizontalDistance, viewAngleThreshold))
            return "right";

        // 检查 "Left" (左侧) -> 方向是 -Right
        if (CheckSpatialRelation(flatPosA, flatCenterB, -flatRightB, flatForwardB, sizeB.z, maxHorizontalDistance, viewAngleThreshold))
            return "left";

        // --- Step 3: 邻近关系 (Proximity) ---
        // 最后检查距离
        float dist3D = Vector3.Distance(posA, posB);
        if (dist3D < nearDistance) return "near";
         if (dist3D > farDistance) return "far";

        return "unrelated";
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
        return (angleB < theta) && (angleC < theta);
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

    [ContextMenu("Run Independent Test")]
    public void RunIndependentTest()
    {
        Debug.Log("<color=yellow>[Test] 开始运行基于预设 JSON 的独立测试...</color>");

        string inputJson = @"
[
  {
    ""name"": ""Cube red"",
    ""position"": {
      ""x"": -0.5,
      ""y"": 1.62,
      ""z"": 0.84
    },
    ""scale"": {
      ""x"": 0.2,
      ""y"": 0.2,
      ""z"": 0.2
    },
    ""boundary"": {
      ""center"": {
        ""x"": -0.5,
        ""y"": 1.52,
        ""z"": 0.84
      },
      ""size"": {
        ""x"": 0.2,
        ""y"": 0.2,
        ""z"": 0.2
      },
      ""forward"": {
        ""x"": 0.0,
        ""y"": 0.0,
        ""z"": 1.0
      },
      ""right"": {
        ""x"": 1.0,
        ""y"": 0.0,
        ""z"": 0.0
      },
      ""up"": {
        ""x"": 0.0,
        ""y"": 1.0,
        ""z"": 0.0
      }
    }
  },
  {
    ""name"": ""Cube green"",
    ""position"": {
      ""x"": -0.5,
      ""y"": 1.62,
      ""z"": -1.59
    },
    ""scale"": {
      ""x"": 0.2,
      ""y"": 0.2,
      ""z"": 0.2
    },
    ""boundary"": {
      ""center"": {
        ""x"": -0.5,
        ""y"": 1.52,
        ""z"": -1.59
      },
      ""size"": {
        ""x"": 0.2,
        ""y"": 0.2,
        ""z"": 0.2
      },
      ""forward"": {
        ""x"": 0.0,
        ""y"": 0.0,
        ""z"": 1.0
      },
      ""right"": {
        ""x"": 1.0,
        ""y"": 0.0,
        ""z"": 0.0
      },
      ""up"": {
        ""x"": 0.0,
        ""y"": 1.0,
        ""z"": 0.0
      }
    }
  },
  {
    ""name"": ""Cube yellow"",
    ""position"": {
      ""x"": -0.83,
      ""y"": 1.62,
      ""z"": 0.84
    },
    ""scale"": {
      ""x"": 0.2,
      ""y"": 0.2,
      ""z"": 0.2
    },
    ""boundary"": {
      ""center"": {
        ""x"": -0.83,
        ""y"": 1.52,
        ""z"": 0.84
      },
      ""size"": {
        ""x"": 0.2,
        ""y"": 0.2,
        ""z"": 0.2
      },
      ""forward"": {
        ""x"": 0.0,
        ""y"": 0.0,
        ""z"": 1.0
      },
      ""right"": {
        ""x"": 1.0,
        ""y"": 0.0,
        ""z"": 0.0
      },
      ""up"": {
        ""x"": 0.0,
        ""y"": 1.0,
        ""z"": 0.0
      }
    }
  },
  {
    ""name"": ""Cube blue"",
    ""position"": {
      ""x"": -0.5,
      ""y"": 1.92,
      ""z"": 0.16
    },
    ""scale"": {
      ""x"": 0.2,
      ""y"": 0.2,
      ""z"": 0.2
    },
    ""boundary"": {
      ""center"": {
        ""x"": -0.5,
        ""y"": 1.82,
        ""z"": 0.16
      },
      ""size"": {
        ""x"": 0.2,
        ""y"": 0.2,
        ""z"": 0.2
      },
      ""forward"": {
        ""x"": 0.0,
        ""y"": 0.0,
        ""z"": 1.0
      },
      ""right"": {
        ""x"": 1.0,
        ""y"": 0.0,
        ""z"": 0.0
      },
      ""up"": {
        ""x"": 0.0,
        ""y"": 1.0,
        ""z"": 0.0
      }
    }
  },
  {
    ""name"": ""Cube purple"",
    ""position"": {
      ""x"": 0.7,
      ""y"": 1.67,
      ""z"": -0.02
    },
    ""scale"": {
      ""x"": 0.2,
      ""y"": 0.2,
      ""z"": 0.2
    },
    ""boundary"": {
      ""center"": {
        ""x"": 0.7,
        ""y"": 1.57,
        ""z"": -0.02
      },
      ""size"": {
        ""x"": 0.21,
        ""y"": 0.21,
        ""z"": 0.21
      },
      ""forward"": {
        ""x"": -1.0,
        ""y"": -0.01,
        ""z"": -0.03
      },
      ""right"": {
        ""x"": -0.03,
        ""y"": -0.04,
        ""z"": 1.0
      },
      ""up"": {
        ""x"": -0.01,
        ""y"": 1.0,
        ""z"": 0.04
      }
    }
  }
]";


        string outputJson = GetRelationData(inputJson);
        Debug.Log($"<color=cyan>[Test Output]</color> 结果如下:\n{outputJson}");

    }
}