using UnityEngine;

public partial class UserStudyController : MonoBehaviour
{
    public enum SceneType { Classroom, City }

    [Header("Scenarios")]
    public SceneType currentScene;
    public GameObject classroom;
    public GameObject city;

    // [Header("天空盒设置 (可选)")]
    // public Material classroomSky;
    // public Material citySky;

    void Start()
    {
        // 初始状态：只显示教室，隐藏城市
        UpdateScene();
    }

    private void Update()
    {
        UpdateScene();
    }

    private void OnValidate()
    {
        // 确保在编辑器模式下也能预览切换效果
        UpdateScene();
    }

    public void UpdateScene()
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

            case SceneType.City:
                classroom.SetActive(false);
                city.SetActive(true);
                // if (citySky != null) RenderSettings.skybox = citySky;
                break;
        }

        // 刷新场景环境，确保天空盒即时生效
        // DynamicGI.UpdateEnvironment();
    }
}