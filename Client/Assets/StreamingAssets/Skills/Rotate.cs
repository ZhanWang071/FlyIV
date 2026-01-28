public class Rotate
{
    public static void Execute(string view_id, float x, float y, float z)
    {
        GameObject go = GameObject.Find(view_id);
        if (go != null)
        {
            go.transform.localEulerAngles = new Vector3(x, y, z);
            Debug.Log($"[Skill] Rotate 完成: {view_id} 旋转设为 ({x},{y},{z})");
        }
    }
}