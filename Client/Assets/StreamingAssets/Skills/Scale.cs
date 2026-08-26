public class Scale
{
    public static void Execute(string object_name, float x, float y, float z)
    {
        GameObject go = GameObject.Find(object_name);
        if (go != null)
        {
            Vector3 current = go.transform.localScale;
            // SCALE 的参数是图表的【绝对目标 localScale】（0 = 该轴保持不变）。
            // 例如当前 localScale 为 (3,3,3)，要放大 50% 应传 (4.5,4.5,4.5)。
            float finalX = (x != 0) ? x : current.x;
            float finalY = (y != 0) ? y : current.y;
            float finalZ = (z != 0) ? z : current.z;

            go.transform.localScale = new Vector3(finalX, finalY, finalZ);
            Debug.Log($"[Skill] Scale 完成: {object_name} localScale 设为绝对目标 ({finalX},{finalY},{finalZ})");
        }
    }
}
