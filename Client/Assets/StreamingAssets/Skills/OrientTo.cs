public class OrientTo
{
    private static float maxPitchAngle = 30f; // 最大允许偏转 30 度

    public static void Execute(string object_name, string target_name)
    {
        GameObject actor = GameObject.Find(object_name);
        if (actor == null) return;

        Vector3 targetPos = (target_name.ToLower() == "user")
            ? Camera.main.transform.position
            : GameObject.Find(target_name)?.transform.position ?? actor.transform.position;

        if (targetPos == actor.transform.position) return;

        // 1. 计算方向向量
        Vector3 direction = targetPos - actor.transform.position;

        // 2. 计算水平方向的投影 (只包含 X 和 Z)
        Vector3 horizontalDir = new Vector3(direction.x, 0, direction.z);

        // 3. 计算目标向量与水平面的夹角
        // 如果 targetPos 在 actor 上方，angle 为负；在下方为正（符合 Unity 欧拉角习惯）
        float targetPitch = Vector3.SignedAngle(horizontalDir, direction, Vector3.Cross(horizontalDir, Vector3.up));

        // 4. 逻辑控制：如果角度超过限制，则取限制值
        // 这里的逻辑不是硬性的属性限制，而是“朝向决策”
        float finalPitch = Mathf.Clamp(targetPitch, -maxPitchAngle, maxPitchAngle);

        // 5. 计算 Yaw (左右旋转)
        // 使用水平方向向量来计算旋转，确保左右方向始终精准
        float yaw = Quaternion.LookRotation(horizontalDir).eulerAngles.y + 180f;

        // 6. 应用最终旋转
        // 我们使用水平方向作为基础，叠加受限后的 Pitch
        actor.transform.rotation = Quaternion.Euler(finalPitch, yaw, 0);

        Debug.Log($"[Skill] OrientTo: 目标偏角 {targetPitch:F1}°, 实际应用 {finalPitch:F1}° (Limit: {maxPitchAngle}°)");
    }
}