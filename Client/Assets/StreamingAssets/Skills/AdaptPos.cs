public class AdaptPos
{
    public static void Execute(string view_id, string object_id, float distance, float height_offset)
    {
        GameObject view = GameObject.Find(view_id);
        GameObject target = GameObject.Find(object_id);

        if (view != null && target != null)
        {
            // 将图表放置在目标物体的前方指定距离，并加上高度偏移
            Vector3 targetPos = target.transform.position + target.transform.forward * distance;
            targetPos.y += height_offset;
            
            view.transform.position = targetPos;

            Debug.Log($"[Skill] AdaptPos 完成: {view_id} 已对齐到 {object_id}");
        }
    }
}