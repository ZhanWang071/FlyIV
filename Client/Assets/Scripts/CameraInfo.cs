using UnityEngine;

public class CameraInfo : MonoBehaviour
{
    [Header("User Settings")]
    public string userName = "User";
    public Vector3 userBodySize = new Vector3(0.5f, 1.7f, 0.3f); // 宽, 高, 深

    public RelationDetection.ObjectNode GetCameraNode()
    {
        Camera cam = Camera.main;
        if (cam == null) return null;
        Transform t = cam.transform;

        // 1. 计算关键点
        // 假设摄像机位置 (t.position) 就是用户的 "头顶/眼睛" 位置
        // Position (Top Center Anchor): 直接就是摄像机位置
        Vector3 topPosition = t.position;

        // Boundary Center (几何中心): 从头顶向下移动身高的一半
        // 注意：这里简单沿 Y 轴向下，如果用户躺着可能需要改为沿 -t.up
        Vector3 bodyCenter = t.position - (Vector3.up * userBodySize.y * 0.5f);

        // 2. 构建节点 (显式传入 x, y, z 以避免报错)
        RelationDetection.ObjectNode userNode = new RelationDetection.ObjectNode
        {
            name = userName,

            // 手动拆解 x, y, z，不直接传 Vector3
            position = new RelationDetection.Vector3Data(
                topPosition.x,
                topPosition.y,
                topPosition.z
            ),

            scale = new RelationDetection.Vector3Data(1f, 1f, 1f),

            boundary = new RelationDetection.BoundaryData
            {
                // 几何中心
                center = new RelationDetection.Vector3Data(
                    bodyCenter.x,
                    bodyCenter.y,
                    bodyCenter.z
                ),

                // 身体尺寸
                size = new RelationDetection.Vector3Data(
                    userBodySize.x,
                    userBodySize.y,
                    userBodySize.z
                ),

                // 方向向量
                forward = new RelationDetection.Vector3Data(t.forward.x, t.forward.y, t.forward.z),
                right = new RelationDetection.Vector3Data(t.right.x, t.right.y, t.right.z),
                up = new RelationDetection.Vector3Data(t.up.x, t.up.y, t.up.z)
            }
        };

        return userNode;
    }
}