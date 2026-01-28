public class Scale
{
    public static void Execute(string object_name, float x, float y, float z)
    {
        GameObject go = GameObject.Find(object_name);
        if (go != null)
        {
            Vector3 current = go.transform.localScale;
            // 如果传入值为 null，则保留当前缩放
            float finalX = x ?? current.x;
            float finalY = y ?? current.y;
            float finalZ = z ?? current.z;

            go.transform.localScale = new Vector3(finalX, finalY, finalZ);
            Debug.Log($"[Skill] Scale 完成: {view_id} 缩放更新为 ({finalX},{finalY},{finalZ})");
        }
    }
}