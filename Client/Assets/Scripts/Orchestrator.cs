using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using UnityEngine.InputSystem;
using System.Diagnostics;

public class Orchestrator : MonoBehaviour
{
    [Header("Reference Settings")]
    public SpeechToText sttHandler;
    public VLMFocus vlmHandler;
    public RelationDetection relationDetector;
    public InteractionTracker interactionTracker;
    public SkillController skillController;
    public ActionExecutor actionExecutor;
    public Transform playerCamera;
    public GameObject Environment;

    [Header("Independent Module Test")]
    [SerializeField] private bool voiceSendtoVLM = false;
    [SerializeField] private bool generateSequence = false;
    [SerializeField] private bool sequenceToExecutor = false;

    [Header("Workflow Debug")]
    [TextArea(3, 5)]
    [SerializeField] private string userRequest = "";
    [TextArea(2, 5)]
    [SerializeField] private string identifiedObjects = "";
    [TextArea(2, 5)]
    [SerializeField] private string pointingObject = "";
    [TextArea(5, 10)]
    [SerializeField] private string userPrompt = "";
    [TextArea(5, 10)]
    [SerializeField] private string APICalls = "";

    private Task _vlmTask;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        if (playerCamera == null) playerCamera = Camera.main.transform;
        
        // 语音开始时执行VLM识别，充分利用语音录制的时间
        if (sttHandler != null)
        {
            sttHandler.OnSpeechStarted += () => {
                _vlmTask = vlmHandler.IdentifyFocusedObject();
            };
        }
        
        // 语音结束时处理结果（VLM可能已完成）
        if (voiceSendtoVLM) sttHandler.OnTranscribeFinished += (speechText) => {
            _ = ProcessWorkflow(speechText);
            userRequest = speechText; // 显示在Inspector中
        };
    }

    private void Update()
    {
        HandleInputAndLook();
        pointingObject = interactionTracker.GetCurrentPointingObjectName();
    }

    [ContextMenu("Test Workflow")]
    private async Task Test()
    {
        await vlmHandler.IdentifyFocusedObject();
        _ = ProcessWorkflow(userRequest);
    }

    /// <summary>
    /// 核心工作流：STT -> VLM(等待) -> 整合数据 -> LLM
    /// </summary>
    private async Task ProcessWorkflow(string speechText)
    {
        Stopwatch totalSw = Stopwatch.StartNew();

        UnityEngine.Debug.Log("<color=cyan>[Orchestrator] 收到语音，开始执行 VLM 识别...</color>");

        // 等待VLM识别完成（可能在语音开始时已触发）
        // if (_vlmTask != null)
        // {
        //     await _vlmTask;
        // }

        UnityEngine.Debug.Log("<color=cyan>[Orchestrator] VLM 识别完成，开始发送user Prompt...</color>");

        identifiedObjects = vlmHandler.identifiedObjects != null ?
            string.Join(", ", vlmHandler.identifiedObjects) :
            "None"; 
            
        // 获取新增的数据文件信息
        string newDataFilesInfo = skillController.GetNewDataFilesInformation();

        object dataInfoObj = null;

        if (!string.IsNullOrEmpty(newDataFilesInfo))
        {
            try
            {
                // 将字符串解析为 JToken，这样它在序列化时会保持对象格式，而不是转义字符串
                dataInfoObj = JToken.Parse(newDataFilesInfo);
                UnityEngine.Debug.Log("<color=cyan>[Orchestrator] 检测到新增数据文件，已作为结构化 JSON 对象添加到 userPrompt</color>");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogWarning($"[Orchestrator] 解析新增数据 JSON 失败: {e.Message}");
            }
        }

        // 构建 user prompt JSON 数据
        var userPromptJson = new
        {
            user_status = new
            {
                position = playerCamera.position,
                forward = playerCamera.forward,
                right = playerCamera.right
            },
            focused_objects = vlmHandler.GetFocusedObjectsData(),
            hit_points = interactionTracker.GetHitPointsData(),
            user_request = speechText,
            data_info = dataInfoObj
        };
        
        // 将JSON转换为字符串
        userPrompt = JsonConvert.SerializeObject(userPromptJson, Formatting.Indented);


        UnityEngine.Debug.Log("<color=cyan>[Orchestrator] promp构建完成</color>");


        // 输入LLM得到skill sequence
        if (generateSequence)
        {
            Stopwatch llmSw = Stopwatch.StartNew();
            APICalls = await skillController.GenerateSkills(speechText, userPrompt);
            llmSw.Stop();
            UnityEngine.Debug.Log($"<color=yellow>[Timer] GenerateSkillSequence 耗时: {llmSw.Elapsed.TotalSeconds:F2} s</color>");
        }

        // 执行skill sequence codes
        if (sequenceToExecutor && !string.IsNullOrEmpty(APICalls))
        {
            Stopwatch executorSw = Stopwatch.StartNew();
            await actionExecutor.ExecuteSkillSequence(APICalls);
            executorSw.Stop();
            UnityEngine.Debug.Log($"<color=yellow>[Timer] ExecuteSkillSequence 耗时: {executorSw.Elapsed.TotalSeconds:F2} s</color>");
        }

        totalSw.Stop();
        UnityEngine.Debug.Log($"<color=yellow>[Timer] 总耗时: {totalSw.Elapsed.TotalSeconds:F2} s</color>");
    }

    private void HandleInputAndLook()
    {
        if (Keyboard.current?.escapeKey.wasPressedThisFrame ?? false)
            Cursor.lockState = (Cursor.lockState == CursorLockMode.None) ? CursorLockMode.Locked : CursorLockMode.None;

        if (Cursor.lockState != CursorLockMode.Locked) return;

        // 简单的移动控制
        if (Mouse.current != null)
        {
            Vector2 d = Mouse.current.delta.ReadValue() * 0.1f;
            playerCamera.Rotate(0, d.x, 0, Space.World);
            playerCamera.Rotate(-d.y, 0, 0, Space.Self);
        }
        if (Keyboard.current != null)
        {
            var k = Keyboard.current;
            Vector3 dir = (playerCamera.forward * (k.wKey.ReadValue() - k.sKey.ReadValue()) +
                           playerCamera.right * (k.dKey.ReadValue() - k.aKey.ReadValue()));
            playerCamera.position += dir * 3.0f * Time.deltaTime;
        }
    }

    /// ---------- Debug Buttons --------------
    public void ResetConversation()
    {
        skillController.ResetConversation();
        ClearAllVisualizations();
    }

    private void ClearAllVisualizations()
    {
        GameObject parentContainer = GameObject.Find("VisObject");

        if (parentContainer == null)
        {
            UnityEngine.Debug.LogWarning("[FlyIV] 找不到名为 'VisObject' 的父容器，无法清理。");
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
        UnityEngine.Debug.Log("[FlyIV] 已清理旧场景的可视化图表");
    }

}