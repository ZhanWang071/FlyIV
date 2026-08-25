public class AdaptPos
{
    public static void Execute(string view_id, string object_id, float distance, float height_offset)
    {
        GameObject view = GameObject.Find(view_id);
        if (object_id.ToLower() == "user")
        {
            Transform cam = Camera.main.transform;
            float actualDist = distance > 0.2f ? distance : 0.2f;
            
            Vector3 targetBasePos = cam.position + (cam.forward * actualDist);
            float actualHeight = height_offset < 0.1f ? height_offset : 0.1f;
            // float offsetToBottom = GetViewOffsetToBottom(view);
            Vector3 desiredPos = targetBasePos + (cam.up * actualHeight);
            view.transform.position = desiredPos;

            if (view.CompareTag("Visualization_3D"))
            {
                BoxCollider bc = view.GetComponent<BoxCollider>();
                if (bc != null)
                {
                    // DxR 图表默认以数据原点（左下角）为锚点，
                    // 这里额外平移，让整张图表的中心（包围盒中心）对准目标点
                    Vector3 worldCenter = view.transform.TransformPoint(bc.center);
                    view.transform.position += desiredPos - worldCenter;
                }
            }

                Debug.Log($"[Skill] AdaptPos 完成: {view_id} 已放置在用户正前方 {actualDist}m 处");
            return;
        }


        GameObject target = GameObject.Find(object_id);
        

        if (view != null && target != null)
        {
            // 1. 获取目标物体的包围盒 (优先从 Renderer 获取，如果没有则尝试 Collider)
            Bounds bounds = new Bounds(target.transform.position, Vector3.zero);
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                bounds = renderer.bounds;
            }
            else
            {
                Collider collider = target.GetComponent<Collider>();
                if (collider != null) bounds = collider.bounds;
            }

            // 2. 计算上表面中心位置 (Top Center)
            // bounds.center 是几何中心，bounds.extents.y 是高度的一半
            Vector3 topCenter = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);

            // 3. 计算相机朝向的位移
            // 既然要向相机方向移动，通常是指从物体位置向用户“拉近”
            Vector3 camPosition = Camera.main.transform.position;
            Vector3 dirToCamera = (camPosition - topCenter).normalized;

            // 4. 应用最终位置
            // 最终位置 = 顶部中心 + 向相机方向的偏移 + 垂直高度修正
            Vector3 finalPos = topCenter + (dirToCamera * distance);
            float offsetToBottom = GetViewOffsetToBottom(view);
            finalPos.y += height_offset + offsetToBottom;

            view.transform.position = finalPos;

            Debug.Log($"[Skill] AdaptPos 完成: {view_id} 已对齐到 {object_id}");
        }
    }

    private static float GetViewOffsetToBottom(GameObject view)
    {
        RectTransform rectTransform = view.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // 对于 Canvas，高度 = rect.height * scale.y
            // 补偿值 = 高度 * (Pivot 的 Y 轴占比)
            // 如果 Pivot 在中心 (0.5)，则补偿 0.5 * height；如果在底部 (0)，则补偿 0
            float worldHeight = rectTransform.rect.height * view.transform.localScale.y;
            return worldHeight * rectTransform.pivot.y;
        }

        // 如果不是 Canvas，回退到 Renderer 逻辑
        Renderer r = view.GetComponentInChildren<Renderer>();
        if (r != null) return r.bounds.extents.y;

        return 0f;
    }

    private static Bounds GetTargetBounds(GameObject obj)
    {
        Renderer r = obj.GetComponentInChildren<Renderer>();
        if (r != null) return r.bounds;

        Collider c = obj.GetComponentInChildren<Collider>();
        if (c != null) return c.bounds;

        return new Bounds(obj.transform.position, Vector3.zero);
    }
}
