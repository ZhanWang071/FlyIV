public class OrientTo
{
    private static float maxPitchAngle = 30f; // 最大允许偏转 30 度

    public static void Execute(string object_name, string target_name)
    {
        GameObject obj = GameObject.Find(object_name);
        if (obj == null)
        {
            Debug.LogWarning($"OrientTo: GameObject '{object_name}' not found.");
            return;
        }

        // --- Resolve target ---
        Vector3 targetPosition;
        Vector3 targetForward;

        if (target_name.ToLower() == "user")
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("OrientTo: Camera.main not found.");
                return;
            }
            targetPosition = cam.transform.position;
            targetForward = cam.transform.forward;
        }
        else
        {
            GameObject target = GameObject.Find(target_name);
            if (target == null)
            {
                Debug.LogWarning($"OrientTo: Target '{target_name}' not found.");
                return;
            }
            targetPosition = target.transform.position;
            targetForward = target.transform.forward;
        }

        // --- Rule 1: Direction from object center to target ---
        Vector3 toTarget = (targetPosition - obj.transform.position).normalized;

        // --- Rule 3: Clamp pitch to ±30 degrees ---
        // Decompose into horizontal and vertical components
        Vector3 toTargetFlat = new Vector3(toTarget.x, 0f, toTarget.z).normalized;
        float pitchAngle = Mathf.Atan2(toTarget.y, toTargetFlat.magnitude) * Mathf.Rad2Deg;
        float clampedPitch = Mathf.Clamp(pitchAngle, -30f, 30f);

        // Reconstruct direction with clamped pitch
        Vector3 clampedDirection = Quaternion.AngleAxis(-clampedPitch, Vector3.right) * toTargetFlat;
        clampedDirection = Quaternion.AngleAxis(
            Mathf.Atan2(toTargetFlat.x, toTargetFlat.z) * Mathf.Rad2Deg,
            Vector3.up
        ) * (Quaternion.AngleAxis(clampedPitch, Vector3.right) * Vector3.forward);

        // --- Rule 2: Blend facing direction with target's forward ---
        // Use target's forward as the "up hint" to align yaw with target orientation
        Vector3 facingDirection = clampedDirection.normalized;

        // Derive up vector from target's forward to align rolls
        Vector3 upHint = Vector3.up;
        if (Mathf.Abs(Vector3.Dot(facingDirection, targetForward)) < 0.99f)
        {
            // Project targetForward onto the plane perpendicular to facingDirection
            Vector3 right = Vector3.Cross(Vector3.up, targetForward).normalized;
            upHint = Vector3.Cross(right, facingDirection).normalized;

            // Fall back to world up if degenerate
            if (upHint == Vector3.zero) upHint = Vector3.up;
        }

        // --- Apply rotation ---
        Quaternion lookRotation = Quaternion.LookRotation(facingDirection, Vector3.up);

        // Step 2: Extract yaw from targetForward and blend it in
        float targetYaw = Mathf.Atan2(targetForward.x, targetForward.z) * Mathf.Rad2Deg;
        float facingYaw = Mathf.Atan2(facingDirection.x, facingDirection.z) * Mathf.Rad2Deg;
        float yawOffset = Mathf.DeltaAngle(facingYaw, targetYaw);

        // Apply yaw alignment on top of the look rotation
        Quaternion targetRotation = Quaternion.AngleAxis(yawOffset, Vector3.up) * lookRotation;
        obj.transform.rotation = targetRotation;

        Debug.Log($"OrientTo: '{object_name}' oriented toward '{target_name}' " +
                  $"(pitch clamped to {clampedPitch:F1}°)");

        // GameObject actor = GameObject.Find(object_name);
        // if (actor == null) return;

        // Vector3 targetPos = (target_name.ToLower() == "user")
        //     ? Camera.main.transform.position
        //     : GameObject.Find(target_name)?.transform.position ?? actor.transform.position;

        // if (targetPos == actor.transform.position) return;



        // // 1. 计算方向向量
        // Vector3 direction = targetPos - actor.transform.position;

        // // 2. 计算水平方向的投影 (只包含 X 和 Z)
        // Vector3 horizontalDir = new Vector3(direction.x, 0, direction.z);

        // // 3. 计算目标向量与水平面的夹角
        // // 如果 targetPos 在 actor 上方，angle 为负；在下方为正（符合 Unity 欧拉角习惯）
        // float targetPitch = Vector3.SignedAngle(horizontalDir, direction, Vector3.Cross(horizontalDir, Vector3.up));

        // // 4. 逻辑控制：如果角度超过限制，则取限制值
        // // 这里的逻辑不是硬性的属性限制，而是“朝向决策”
        // float finalPitch = Mathf.Clamp(targetPitch, -maxPitchAngle, maxPitchAngle);

        // // 5. 计算 Yaw (左右旋转)
        // // 使用水平方向向量来计算旋转，确保左右方向始终精准
        // float yaw = Quaternion.LookRotation(horizontalDir).eulerAngles.y + 180f;

        // // 6. 应用最终旋转
        // // 我们使用水平方向作为基础，叠加受限后的 Pitch
        // actor.transform.rotation = Quaternion.Euler(finalPitch, yaw, 0);

        // Debug.Log($"[Skill] OrientTo: 目标偏角 {targetPitch:F1}°, 实际应用 {finalPitch:F1}° (Limit: {maxPitchAngle}°)");

    }
}