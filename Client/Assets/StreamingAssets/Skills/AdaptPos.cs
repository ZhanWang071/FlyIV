public class AdaptPos
{
    public static void Execute(string view_id, string object_id, float distance, float height_offset)
    {
        GameObject view = GameObject.Find(view_id);
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
            finalPos.y += height_offset;

            view.transform.position = finalPos;

            Debug.Log($"[Skill] AdaptPos 完成: {view_id} 已对齐到 {object_id}");
        }
    }
}