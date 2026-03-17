public class Delete
{
    public static void Execute(string view_id)
    {
        GameObject go = GameObject.Find(view_id);
        if (go != null)
        {
            GameObject.Destroy(go);
            Debug.Log($"[Skill] Delete 完成: 已删除图表 {view_id}");
        }
    }
}