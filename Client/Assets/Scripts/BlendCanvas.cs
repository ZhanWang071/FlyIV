using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;

public class BlendCanvas : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] private string targetObjectName;    // 目标物体（如建筑、地形）
    [SerializeField] private string chartCanvasName;    // 要嵌入的 UI Canvas (World Space)
    
    [SerializeField] private Transform userCamera;      // 用户视角 (VR Camera)

    [Header("Settings")]
    [SerializeField] private float surfaceOffset = 0.02f; // 防止 Z-Fighting 的微小偏移
    [SerializeField] private float scaleRatio = 0.9f; // 占平面多大
    [SerializeField] private float pixelsPerUnit = 100f; // 1米对应多少像素如果缩小的时候字体过大，可增大该值至400-600
    [SerializeField] private bool autoResize = true;     // 是否开启自适应大小

    void Start()
    {
        userCamera = Camera.main.transform;
    }

    [ContextMenu("TestBlendCanvas")]
    private void TestEmbedVis()
    {
        EmbeddedVis(chartCanvasName, targetObjectName);
    }

    public void EmbeddedVis(string viewCanvas_id, string target_id)
    {
        if (string.IsNullOrEmpty(target_id) || string.IsNullOrEmpty(viewCanvas_id) || userCamera == null) return;

        GameObject chartCanvas = GameObject.Find(viewCanvas_id);
        GameObject targetObject = GameObject.Find(target_id);

        if (chartCanvas == null || targetObject == null)
        {
            Debug.LogWarning("[BlendCanvas] ChartCanvas or TargetObject not found in the scene.");
            return;
        }

        Canvas canvasComponent = chartCanvas.GetComponent<Canvas>();
        if (canvasComponent != null)
        {
            canvasComponent.renderMode = RenderMode.WorldSpace;
        }

        CalculatePlanarMapping(chartCanvas, targetObject);
    }

    /// <summary>
    /// 方案一：平面映射 (Planar)
    /// 逻辑：从用户视角发射射线，获取碰撞点和表面法线，使 UI 平面完全平行于该面。
    /// </summary>
    private void CalculatePlanarMapping(GameObject chartCanvas, GameObject targetObject)
    {
        CalculatePlanarMapping(chartCanvas, targetObject, scaleRatio);
    }
    private void CalculatePlanarMapping(GameObject chartCanvas, GameObject targetObject, float ratio)
    {
        chartCanvasName = chartCanvas.name;
        targetObjectName = targetObject.name;
        RectTransform canvasRect = chartCanvas.transform as RectTransform;

        Ray ray = new Ray(userCamera.position, targetObject.transform.position - userCamera.position);
        RaycastHit hit;

        // 获取 targetObject 的 bounds（即使没有 collider）
        Bounds? targetBounds = GetCombinedBounds(targetObject);
        if (targetBounds == null)
        {
            Debug.LogWarning("[BlendCanvas] TargetObject 没有 Renderer 组件，无法计算 bounds");
            return;
        }
        Bounds b = targetBounds.Value;

        if (Physics.Raycast(ray, out hit))
        {
            Vector3 hitNormal = hit.normal;

            Debug.Log("[BlendCanvas] Blend ChartCanvas on " + targetObjectName);
            Vector3 localNormal = targetObject.transform.InverseTransformDirection(hitNormal);

            // 位置与旋转对齐
            // 如果物体有子物体，这里计算到的是整个对象（包含所有子物体）的包围盒在法线方向上的那一面中心
            // 计算物体面向摄像机一侧的面中心
            Vector3 toCamera = (userCamera.position - targetObject.transform.position).normalized;
            Vector3 faceNormal = Vector3.zero;
            // 找到哪个轴方向分量最大（即最朝向摄像机的局部轴）
            Vector3[] axes = { targetObject.transform.right, targetObject.transform.up, targetObject.transform.forward };
            float maxDot = -Mathf.Infinity;
            Vector3 bestAxis = Vector3.forward; // 默认使用forward
            foreach (var axis in axes)
            {
                float dot = Vector3.Dot(axis, toCamera);
                Debug.Log(dot);
                if (Mathf.Abs(dot) > Mathf.Abs(maxDot))
                {
                    maxDot = dot;
                    faceNormal = axis * Mathf.Sign(dot);
                    bestAxis = axis;
                }
            }
            if (faceNormal == Vector3.zero)
            {
                faceNormal = bestAxis;
            }
            Vector3 faceCenter = b.center + Vector3.Scale(faceNormal.normalized, b.extents);
            canvasRect.position = faceCenter + (hitNormal * surfaceOffset);
            canvasRect.rotation = Quaternion.LookRotation(-faceNormal);

            // 2. 尺寸自适应逻辑
            if (autoResize)
            {
                    
                // 这里可以直接根据法线方向排除掉不需要的轴
                Vector3 worldSize = b.size;
                float worldW = 0, worldH = 0;

                // 如果法线主要指向 Z 轴或 -Z 轴，那么宽是 X，高是 Y
                if (Mathf.Abs(faceNormal.z) > 0.5f) { worldW = worldSize.x; worldH = worldSize.y; }
                // 如果法线指向 X 轴，那么宽是 Z，高是 Y
                else if (Mathf.Abs(faceNormal.x) > 0.5f) { worldW = worldSize.z; worldH = worldSize.y; }
                // 如果法线指向 Y 轴（顶面），那么宽是 X，高是 Z
                else { worldW = worldSize.x; worldH = worldSize.z; }

                // 计算目标像素大小
                float targetPixelW = ratio * worldW * pixelsPerUnit;
                float targetPixelH = ratio * worldH * pixelsPerUnit;
                // 设置chart object铺满canvas
                RectTransform chartRect = chartCanvas.transform.GetChild(0) as RectTransform;

                chartRect.anchorMin = new Vector2(0, 0);
                chartRect.anchorMax = new Vector2(1, 1);
                chartRect.offsetMin = Vector2.zero; // Left: 0, Bottom: 0
                chartRect.offsetMax = Vector2.zero; // Right: 0, Top: 0
                chartRect.anchoredPosition = Vector2.zero;
                chartRect.localScale = Vector3.one;

                // 设置 Canvas大小自适应平面大小
                canvasRect.sizeDelta = new Vector2(targetPixelW, targetPixelH);
                canvasRect.localScale = Vector3.one / pixelsPerUnit;
            }
        }
    }

    /// <summary>
    /// 获取 GameObject 的合并边界，包括自身和所有子对象的 Renderer
    /// </summary>
    private Bounds? GetCombinedBounds(GameObject go)
    {
        List<Renderer> renderers = new List<Renderer>();

        // 检查自身是否有 Renderer
        Renderer selfRenderer = go.GetComponent<Renderer>();
        if (selfRenderer != null)
        {
            renderers.Add(selfRenderer);
        }

        // 查找所有子对象中的 Renderer
        Renderer[] childRenderers = go.GetComponentsInChildren<Renderer>();
        foreach (Renderer childRenderer in childRenderers)
        {
            // 排除自身（如果自身有 Renderer，已经在上面添加了）
            if (childRenderer != selfRenderer)
            {
                renderers.Add(childRenderer);
            }
        }

        // 如果没有找到任何 Renderer，返回 null
        if (renderers.Count == 0)
        {
            return null;
        }

        // 合并所有 Renderer 的边界
        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Count; i++)
        {
            combinedBounds.Encapsulate(renderers[i].bounds);
        }

        return combinedBounds;
    }

    /// <summary>
    /// TODO: 方案二：非平面/贴合计算 (Non-Planar)
    /// 逻辑：通过采样 Canvas 的四个角，分别计算它们到目标表面的投影，
    /// 这样即使表面有弧度，UI 的位置也会根据局部几何结构调整。
    /// </summary>
    // void CalculateNonPlanarMapping()
    // {
    //     // 首先执行基础对齐
    //     CalculatePlanarMapping();

    //     // 获取 Canvas 在世界空间下的四个角
    //     Vector3[] corners = new Vector3[4];
    //     canvasRect.GetWorldCorners(corners);

    //     Vector3 centerShift = Vector3.zero;

    //     for (int i = 0; i < 4; i++)
    //     {
    //         // 从每个角向物体中心方向/或射线方向发射检测
    //         Ray cornerRay = new Ray(userCamera.position, corners[i] - userCamera.position);
    //         RaycastHit cornerHit;

    //         if (Physics.Raycast(cornerRay, out cornerHit))
    //         {
    //             if (cornerHit.collider.gameObject == targetObject)
    //             {
    //                 // 计算每个角需要修正的位移量，使其紧贴表面
    //                 Vector3 targetPos = cornerHit.point + (cornerHit.normal * surfaceOffset);
    //                 centerShift += (targetPos - corners[i]);
    //             }
    //         }
    //     }

    //     // 应用平均修正（简单的非平面贴合，若要UI完全弯曲，则需要修改Mesh顶点）
    //     canvasRect.position += centerShift / 4f;
    // }
}