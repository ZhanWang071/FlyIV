using UnityEngine;

public partial class UserStudyController : MonoBehaviour
{
    public enum SceneType { Classroom, City }

    [Header("Scenarios")]
    public SceneType currentScene;
    private SceneType lastScene;
    public GameObject classroom;
    public GameObject city;

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
                
                // 建议：在此处同时清理掉场景中旧的图表
                ClearAllVisualizations();
            }
        }


    }

    private void ClearAllVisualizations()
    {
        GameObject parentContainer = GameObject.Find("VisObject");

        if (parentContainer == null)
        {
            Debug.LogWarning("[FlyIV] 找不到名为 'VisObject' 的父容器，无法清理。");
            return;
        }

        int count = 0;
        // 注意：必须从后往前遍历，或者使用 List 存储后统一销毁
        // 否则在遍历过程中销毁物体会导致索引崩溃
        for (int i = parentContainer.transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = parentContainer.transform.GetChild(i).gameObject;

            // 核心判断：只清除当前处于 Active 状态的物体
            if (child.activeSelf)
            {
                Destroy(child);
                count++;
            }
        }
        Debug.Log("[FlyIV] 已清理旧场景的可视化图表");
    }
}