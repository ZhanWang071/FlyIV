public class Layout
{
    public static void Execute(List<string> views_id, float distance, float height_offset, string layout_type)
    {
        float currentX = 0;
        
        foreach (string id in views_id)
        {
            GameObject view = GameObject.Find(id);
            if (view == null) continue;

            // 简单的水平布局示例
            if (layout_type.ToLower() == "horizontal")
            {
                view.transform.position += new Vector3(currentX, height_offset, distance);
                currentX += 2.0f; // 间隔
            }
        }
        Debug.Log($"[Skill] Layout 完成: 执行了 {layout_type} 布局");
    }
}