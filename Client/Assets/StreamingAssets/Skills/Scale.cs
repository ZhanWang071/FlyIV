public class Scale
{
    public static void Execute(string object_name, float x, float y, float z)
    {
        GameObject go = GameObject.Find(object_name);
        if (go != null)
        {
            Vector3 current = go.transform.localScale;
            // 如果传入值为 null，则保留当前缩放
            float finalX = (x != 0 && x > current.x) ? x : current.x;
            float finalY = (y != 0 && y > current.y) ? y : current.y;
            float finalZ = (z != 0 && z > current.z) ? z : current.z;

            go.transform.localScale = new Vector3(finalX, finalY, finalZ);
            Debug.Log($"[Skill] Scale 完成: {object_name} 缩放更新为 ({finalX},{finalY},{finalZ})");
        }
    }
}