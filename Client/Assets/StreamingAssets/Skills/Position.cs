public class Position
{
    public static void Execute(string view_id, float x, float y, float z)
    {
        GameObject go = GameObject.Find(view_id);
        if (go != null)
        {
            go.transform.position = new Vector3(x, y, z);
            Debug.Log($"[Skill] Position 完成: {view_id} 坐标设为 ({x},{y},{z})");
        }
    }
}