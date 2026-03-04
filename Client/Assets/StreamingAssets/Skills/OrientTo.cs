public class OrientTo
{
    // 定义最大倾斜角度（例如 30 度）
    private static float maxPitchAngle = 30f;

    public static void Execute(string object_name, string target_name)
    {
        GameObject actor = GameObject.Find(object_name);
        if (actor == null) return;

        Vector3 targetPos = (target_name.ToLower() == "user") 
            ? Camera.main.transform.position 
            : GameObject.Find(target_name)?.transform.position ?? actor.transform.position;

        if (targetPos == actor.transform.position) return;

        // 1. 获取目标方向的完整旋转
        Vector3 direction = targetPos - actor.transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // 2. 提取欧拉角进行限制
        Vector3 angles = targetRotation.eulerAngles;

        // Unity 的欧拉角范围是 0-360。我们需要将其转换为 -180 到 180 来进行 Clamp
        float pitch = angles.x;
        if (pitch > 180) pitch -= 360;

        // 限制倾斜幅度
        pitch = Mathf.Clamp(pitch, -maxPitchAngle, maxPitchAngle);

        // 3. 应用新的旋转：保留计算出的 Y 轴（左右转），限制 X 轴（上下偏）
        float yaw = angles.y;
        // 如果 actor 是 Canvas 类型（UI），则需要在 Y 轴上额外旋转 180° 以保持正面对目标
        // if (actor.GetComponent<Canvas>() != null)
        // {
        //     yaw += 180f;
        // }
        yaw += 180f;

        actor.transform.rotation = Quaternion.Euler(pitch, yaw, 0);

        Debug.Log($"[Skill] OrientTo 完成: {object_name} 俯仰角限制在 {pitch:F1}°，yaw={yaw:F1}°");
    }
}