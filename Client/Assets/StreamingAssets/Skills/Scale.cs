public class Scale
{
    public static void Execute(string object_name, float x, float y, float z)
    {
        GameObject go = GameObject.Find(object_name);
        if(go != null) go.transform.localScale = new Vector3(x, y, z);
        Debug.Log($"[Skill] Scale 完成: {object_name} scale大小为 ({x},{y},{z})");
    }
}