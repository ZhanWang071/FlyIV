using UnityEngine;
using System.Collections.Generic;

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
        if (currentScene != lastScene)
        {
            UpdateScene(forceReset: true);
            lastScene = currentScene; // 更新快照
        }

        // 2. 监听左手 Y 按钮 (Meta SDK: Button.Two)
        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch))
        {
            CycleTeleportPoint();
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
                Vector3 cameraOffset = Camera.main.transform.localPosition;
                cameraOffset.x = 0;
                cameraOffset.z = 0;
                cameraRig.position = target.position - cameraOffset;

                // 让 Rig 的旋转 = 目标旋转 - 相机的偏航角
                // 这样：Rig转完后 + 相机自带的偏航 = 目标朝向
                float currentYaw = Camera.main.transform.localEulerAngles.y;
                cameraRig.rotation = Quaternion.Euler(0, target.eulerAngles.y - currentYaw, 0);
            }
        }

        Debug.Log($"<color=orange>[Teleport] 切换到场景 {currentScene} 的点位 {config.currentPointIndex}</color>");
    }
}