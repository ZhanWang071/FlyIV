using UnityEngine;
using Newtonsoft.Json;
using System.Threading.Tasks;
using UnityEngine.InputSystem;
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
        Debug.Log("<color=cyan>[Orchestrator] 收到语音，开始执行 VLM 识别...</color>");

        // 等待VLM识别完成（可能在语音开始时已触发）
        // if (_vlmTask != null)
        // {
        //     await _vlmTask;
        // }

        Debug.Log("<color=cyan>[Orchestrator] VLM 识别完成，开始发送user Prompt...</color>");

        identifiedObjects = vlmHandler.identifiedObjects != null ?
            string.Join(", ", vlmHandler.identifiedObjects) :
            "None"; 
            
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
            user_request = speechText
        };
        userPrompt = JsonConvert.SerializeObject(userPromptJson, Formatting.Indented);
        Debug.Log("<color=cyan>[Orchestrator] promp构建完成</color>");


        // 输入LLM得到skill sequence
        if (generateSequence) APICalls = await skillController.GenerateSkills(speechText, userPrompt);

        // 执行skill sequence codes
        if (sequenceToExecutor) await actionExecutor.ExecuteSkillSequence(APICalls);
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
    }

}