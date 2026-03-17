public class Embed
{
    public static void Execute(string view_canvas_id, string object_id)
    {
        if (string.IsNullOrEmpty(view_canvas_id) || string.IsNullOrEmpty(object_id)) return;

        GameObject chartCanvas = GameObject.Find(view_canvas_id);
        GameObject targetObject = GameObject.Find(object_id);

        if (chartCanvas == null || targetObject == null) return;
        
        Canvas canvasComponent = chartCanvas.GetComponent<Canvas>();
        if (canvasComponent != null)
        {
            canvasComponent.renderMode = RenderMode.WorldSpace;
        }

        CalculatePlanarMapping(chartCanvas, targetObject);
        
        Debug.Log($"[Skill] Embed 完成: {view_canvas_id} 已嵌入到 {object_id} 表面");
    }

    /// <summary>
    /// 平面映射 (Planar)
    /// 逻辑：从用户视角发射射线，获取碰撞点和表面法线，使 UI 平面完全平行于该面。
    /// </summary>
    private static void CalculatePlanarMapping(GameObject chartCanvas, GameObject targetObject)
    {
        Transform userCamera = Camera.main.transform;
        float pixelsPerUnit = 100f; // 1米对应多少像素如果缩小的时候字体过大，可增大该值至400-600

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

            Debug.Log("[BlendCanvas] Blend ChartCanvas on " + targetObject.name);
            Vector3 localNormal = targetObject.transform.InverseTransformDirection(hitNormal);

            // 位置与旋转对齐
            // 如果物体有子物体，这里计算到的是整个对象（包含所有子物体）的包围盒在法线方向上的那一面中心
            // 计算物体面向摄像机一侧的面中心
            Vector3 toCamera = (userCamera.position - targetObject.transform.position).normalized;
            Vector3 toCameraFlat = new Vector3(
    userCamera.position.x - targetObject.transform.position.x,
    0f,
    userCamera.position.z - targetObject.transform.position.z
).normalized;
            Vector3 faceNormal = Vector3.zero;
            // 找到哪个轴方向分量最大（即最朝向摄像机的局部轴）
            // Vector3[] axes = { targetObject.transform.right, targetObject.transform.up, targetObject.transform.forward };
            Vector3[] axes =
                {
                    Vector3.right,
                    -Vector3.right,
                    Vector3.up,
                    -Vector3.up,
                    Vector3.forward,
                    -Vector3.forward
                };
            float maxDot = -Mathf.Infinity;
            Vector3 bestAxis = Vector3.forward; // 默认使用forward

            string[] axisNames = { "+right", "-right", "+up", "-up", "+forward", "-forward" };
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


            // for (int i = 0; i < axes.Length; i++)
            // {
            //     // Flatten each axis to horizontal before comparing
            //     float dot = Vector3.Dot(axes[i], toCameraFlat);
            //     Debug.Log($"[BlendCanvas] Axis {axisNames[i]} dot={dot:F3}");

            //     if (dot > maxDot)
            //     {
            //         maxDot = dot;
            //         faceNormal = axes[i];
            //         Debug.Log($"[BlendCanvas] New best face: {axisNames[i]}");
            //     }
            
            // }

            Debug.Log($"[BlendCanvas] Selected faceNormal={faceNormal}, toCamera={toCamera}");


            float surfaceDistance = Vector3.Dot(b.extents, AbsVec(faceNormal.normalized)); // component-wise abs
            Vector3 faceCenter = b.center + faceNormal.normalized * surfaceDistance;
            canvasRect.position = faceCenter - (hitNormal * 0.02f); // 防止 Z-Fighting 的微小偏移
            canvasRect.rotation = Quaternion.LookRotation(-faceNormal);

            RectTransform chartRect = chartCanvas.transform.GetChild(0) as RectTransform;

            // 获取图表原始尺寸（保持长宽比）
            Vector2 chartOriginalSize = canvasRect.sizeDelta;
            float chartAspectRatio = chartOriginalSize.x / Mathf.Max(chartOriginalSize.y, 1f);

            // 自适应平面大小
            // 这里可以直接根据法线方向排除掉不需要的轴
            Vector3 worldSize = b.size;
            float worldW = 0, worldH = 0;

            // 如果法线主要指向 Z 轴或 -Z 轴，那么宽是 X，高是 Y
            if (Mathf.Abs(faceNormal.z) > 0.5f) { worldW = worldSize.x; worldH = worldSize.y; }
            // 如果法线指向 X 轴，那么宽是 Z，高是 Y
            else if (Mathf.Abs(faceNormal.x) > 0.5f) { worldW = worldSize.z; worldH = worldSize.y; }
            // 如果法线指向 Y 轴（顶面），那么宽是 X，高是 Z
            else { worldW = worldSize.x; worldH = worldSize.z; }

            // 缩小可视化尺寸到物体面的90%，同时保持图表原始长宽比
            float scaleFactor = 0.9f;
            float availableW = worldW * scaleFactor;
            float availableH = worldH * scaleFactor;

            // Blend between the chart's native ratio and the face's natural ratio,
            // allowing the chart to flex up to ±40% from its original proportions.
            float faceAspectRatio = availableW / Mathf.Max(availableH, 0.0001f);
            float blendFactor = 0.9f;   // 0 = strict original ratio, 1 = match face exactly
            float maxStretch = 0.6f;   // allow up to 40% deviation from original ratio

            float blendedRatio = Mathf.Lerp(chartAspectRatio, faceAspectRatio, blendFactor);
            float clampedRatio = Mathf.Clamp(blendedRatio, chartAspectRatio * (1f - maxStretch), chartAspectRatio * (1f + maxStretch));

            float targetPixelW, targetPixelH;

            if (availableW / availableH > clampedRatio)
            {
                targetPixelH = availableH * pixelsPerUnit;
                targetPixelW = targetPixelH * clampedRatio;
            }
            else
            {
                targetPixelW = availableW * pixelsPerUnit;
                targetPixelH = targetPixelW / clampedRatio;
            }

            // 设置chart object铺满canvas

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

    private static Vector3 AbsVec(Vector3 v) =>
    new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

    private static Bounds? GetCombinedBounds(GameObject go)
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
}