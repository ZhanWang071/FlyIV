using UnityEngine;

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
}