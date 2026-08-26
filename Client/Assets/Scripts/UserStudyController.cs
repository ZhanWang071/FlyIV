using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public partial class UserStudyController : MonoBehaviour
{
    public enum SceneType { Reproduction, Classroom, City}

    [Header("Scenarios")]
    public SceneType currentScene;
    private SceneType lastScene;
    public GameObject classroom;
    public GameObject city;

    [Header("Participant Logging")]
    public string participantID = "-1";

    [System.Serializable]
    public class SceneTeleportPoints
    {
        public SceneType scene;
        public List<Transform> points; // 该场景下的所有可选点位
        [HideInInspector] public int currentPointIndex = 0;
    }

    [Header("Teleport Settings")]
    public Transform cameraRig; // 拖入 [BuildingBlock] Camera Rig
    public List<SceneTeleportPoints> teleportConfigs; // 在 Inspector 中配置每个场景的点位

    [Header("Controller Input")]
    [Tooltip("长按 Y/B 键多久触发“重置对话并清空可视化”（秒）")]
    public float longPressResetSeconds = 2f;
    private float _teleportButtonPressTime = -1f;

    // [Header("天空盒设置 (可选)")]
    // public Material classroomSky;
    // public Material citySky;
    
    void Start()
    {
        // 初始状态：只显示教室，隐藏城市
        lastScene = currentScene;
        UpdateScene(forceReset: false);
    }

    private void Update()
    {
        // if (Time.frameCount % 60 == 0)
        // { // 每秒打印一次，避免刷屏
        //     bool isConnected = OVRInput.IsControllerConnected(OVRInput.Controller.LTouch);
        //     Debug.Log($"<color=white>左手手柄连接状态: {isConnected}</color>");
        // }

        if (currentScene != lastScene)
        {
            UpdateScene(forceReset: true);
            lastScene = currentScene; // 更新快照
        }

        // 2. 监听 Y/B 按钮（左右手均可）：单击切换传送点，长按重置对话并清空可视化
        bool yDown = OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch) ||
                     OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch);
        bool yUp = OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.LTouch) ||
                   OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.RTouch);

        if (yDown)
        {
            _teleportButtonPressTime = Time.time;
        }

        if (yUp && _teleportButtonPressTime >= 0f)
        {
            if (Time.time - _teleportButtonPressTime >= longPressResetSeconds)
            {
                ResetSceneConversation();
            }
            else
            {
                CycleTeleportPoint();
            }
            _teleportButtonPressTime = -1f;
        }

        // 键盘 ↓ 等价于单击切换传送点（桌面调试用）
        if (Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            CycleTeleportPoint();
        }
    }

    /// <summary>
    /// 长按 Y/B 触发：重置 LLM 对话并清空当前场景的所有可视化图表。
    /// </summary>
    private void ResetSceneConversation()
    {
        Orchestrator orchestrator = Object.FindAnyObjectByType<Orchestrator>();
        if (orchestrator != null)
        {
            orchestrator.ResetConversation();
            Debug.Log("<color=orange>[UserStudy] 长按 Y/B：已重置对话并清空可视化。</color>");
        }
    }

    private void OnValidate()
    {
        // 确保在编辑器模式下也能预览切换效果
        if (currentScene != lastScene)
        {
            UpdateScene(forceReset: true);
            lastScene = currentScene;
        }
    }

    public void UpdateScene(bool forceReset)
    {
        // 逻辑判断：根据枚举状态显示或隐藏物体
        if (classroom == null || city == null) return;

        TeleportToCurrentPoint();

        switch (currentScene)
        {
            case SceneType.Classroom:
                classroom.SetActive(true);
                city.SetActive(false);
                // if (classroomSky != null) RenderSettings.skybox = classroomSky;
                break;

            case SceneType.Reproduction:
                classroom.SetActive(true);
                city.SetActive(false);
                break;

            case SceneType.City:
                classroom.SetActive(false);
                city.SetActive(true);
                // if (citySky != null) RenderSettings.skybox = citySky;
                break;
        }

        // 刷新场景环境，确保天空盒即时生效
        // DynamicGI.UpdateEnvironment();

        // 2. 条件触发：只有场景真正切换时才重置
        if (forceReset)
        {
            Orchestrator orchestrator = Object.FindAnyObjectByType<Orchestrator>();
            if (orchestrator != null)
            {
                orchestrator.ResetConversation();
            }
        }
    }

    // 循环切换当前场景的点位
    private void CycleTeleportPoint()
    {
        var config = teleportConfigs.Find(c => c.scene == currentScene);
        if (config == null || config.points == null) return;

        // 索引循环
        config.currentPointIndex = (config.currentPointIndex + 1) % config.points.Count;
        TeleportToCurrentPoint();

        // Debug.Log($"<color=orange>[Teleport] 切换到场景 {currentScene} 的点位 {config.currentPointIndex}</color>");
    }

    // 执行位移和旋转
    private void TeleportToCurrentPoint()
    {

        // Debug.Log($"<color=orange>[Teleport] 切换到场景 {currentScene} 的点位 2222</color>");
        if (cameraRig == null) return;

        var config = teleportConfigs.Find(c => c.scene == currentScene);
        if (config != null && config.points != null)
        {

            Transform target = config.points[config.currentPointIndex];
            if (target != null)
            {
                // 1. 先旋转：让 Rig 的旋转 = 目标旋转 - 相机本地偏航角
                //    这样 Rig 转完后 + 相机自带偏航 = 目标朝向
                float currentYaw = Camera.main.transform.localEulerAngles.y;
                cameraRig.rotation = Quaternion.Euler(0, target.eulerAngles.y - currentYaw, 0);

                // 2. 再平移：把"眼睛"（CenterEyeAnchor）精确平移到目标点。
                //    注意 CenterEyeAnchor.localPosition 每帧由 OVRCameraRig 设为头显追踪位姿
                //    （FloorLevel 下 Y 包含离地眼高），所以 Rig 不能直接放到 target.position，
                //    否则会把 Rig 高度与头显高度叠加，导致视角偏高。
                cameraRig.position += target.position - Camera.main.transform.position;
            }
        }

        Debug.Log($"<color=orange>[Teleport] 切换到场景 {currentScene} 的点位 {config.currentPointIndex}</color>");
    }
}
