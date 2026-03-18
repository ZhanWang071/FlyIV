using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using UnityEngine.InputSystem;
using System.Diagnostics;
using System.IO;
using System;

public class Orchestrator : MonoBehaviour
{
    [Header("Reference Settings")]
    public SpeechToText sttHandler;
    public VLMFocus vlmHandler;
    public RelationDetection relationDetector;
    public InteractionTracker interactionTracker;
    public SkillController skillController;
    public ActionExecutor actionExecutor;
    public UserStudyController userStudyController;
    public Transform playerCamera;
    public GameObject Environment;

    [Header("Participant Logging")]
    private string participantID = "-1";

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

    // --- Logging state ---
    private string _currentLogPath = null;
    private UserStudyController.SceneType _lastSceneType;
    private bool _logInitialized = false;
    private int _interactionIndex = 0; // counts ProcessWorkflow calls within this session
    private Stopwatch _speechToFinishSw = new Stopwatch(); // starts at OnSpeechStarted, stopped at end of ProcessWorkflow

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        if (playerCamera == null) playerCamera = Camera.main.transform;

        participantID = userStudyController.participantID;

        InitLogFile();

        // 语音开始时执行VLM识别，充分利用语音录制的时间
        if (sttHandler != null)
        {
            sttHandler.OnSpeechStarted += () => {
                _speechToFinishSw.Restart(); // begin speech-to-finish timing
                _vlmTask = vlmHandler.IdentifyFocusedObject();
            };
        }

        // 语音结束时处理结果（VLM可能已完成）
        if (voiceSendtoVLM) sttHandler.OnTranscribeFinished += (speechText) => {
            _ = ProcessWorkflow(speechText);
            userRequest = speechText;
        };
    }

    private void Update()
    {
        HandleInputAndLook();
        pointingObject = interactionTracker.GetCurrentPointingObjectName();

        // Detect scene changes driven by UserStudyController
        if (userStudyController != null &&
            (_logInitialized == false || userStudyController.currentScene != _lastSceneType))
        {
            InitLogFile();
        }
    }

    // =====================================================================
    //  Logging helpers
    // =====================================================================

    /// <summary>
    /// Creates a new log file for the current participantID + sceneType.
    /// Skipped entirely when participantID == "-1".
    /// </summary>
    private void InitLogFile()
    {
        _logInitialized = true;

        string sceneLabel = userStudyController != null
            ? userStudyController.currentScene.ToString()
            : "UnknownScene";

        _lastSceneType = userStudyController != null
            ? userStudyController.currentScene
            : default;

        _interactionIndex = 0;

        if (participantID == "-1")
        {
            _currentLogPath = null;
            UnityEngine.Debug.Log("[Logger] participantID is -1, logging disabled.");
            return;
        }

        string logDir = Path.Combine(Application.dataPath, "Logs");
        Directory.CreateDirectory(logDir);

        // Find a non-colliding index: {ID}_{SceneType}_0.txt, _1.txt, ...
        int fileIndex = 0;
        string path;
        do
        {
            path = Path.Combine(logDir, $"{participantID}_{sceneLabel}_{fileIndex}.txt");
            fileIndex++;
        } while (File.Exists(path));

        _currentLogPath = path;

        // Write header
        string header =
            $"=== User Study Log ==={System.Environment.NewLine}" +
            $"Participant : {participantID}{System.Environment.NewLine}" +
            $"Scene       : {sceneLabel}{System.Environment.NewLine}" +
            $"Started     : {DateTime.Now:yyyy-MM-dd HH:mm:ss}{System.Environment.NewLine}" +
            $"{"=".PadRight(60, '=')}{System.Environment.NewLine}";

        File.WriteAllText(_currentLogPath, header);
        UnityEngine.Debug.Log($"[Logger] Log file created: {_currentLogPath}");
    }

    /// <summary>
    /// Appends one interaction block (userPrompt + APICalls + timings) to the log file.
    /// Pass -1 for llmSeconds or executorSeconds when the corresponding step was skipped.
    /// </summary>
    private void AppendInteractionLog(string prompt, string apiCalls,
        double llmSeconds, double executorSeconds, double totalSeconds,
        double voiceSeconds, double speechToFinishSeconds)
    {
        if (_currentLogPath == null) return;

        _interactionIndex++;
        string separator = "=".PadRight(60, '=');
        string llmStr      = llmSeconds      >= 0 ? $"{llmSeconds:F2}s"      : "skipped";
        string executorStr = executorSeconds >= 0 ? $"{executorSeconds:F2}s" : "skipped";
        string voiceStr         = voiceSeconds         >= 0 ? $"{voiceSeconds:F2}s"         : "n/a";
        string speechToFinishStr = speechToFinishSeconds >= 0 ? $"{speechToFinishSeconds:F2}s" : "n/a";
        string entry =
            $"{System.Environment.NewLine}{separator}{System.Environment.NewLine}" +
            $"Interaction #{_interactionIndex}  |  {DateTime.Now:HH:mm:ss}{System.Environment.NewLine}" +
            $"[Timing]  Voice→STT: {voiceStr}  |  LLM: {llmStr}  |  Executor: {executorStr}  |  Total(workflow): {totalSeconds:F2}s  |  Total(speech→finish): {speechToFinishStr}{System.Environment.NewLine}" +
            $"{separator}{System.Environment.NewLine}" +
            $"[User Prompt]{System.Environment.NewLine}{prompt}{System.Environment.NewLine}" +
            $"{System.Environment.NewLine}[Generated Sequence]{System.Environment.NewLine}{(string.IsNullOrEmpty(apiCalls) ? "(not generated)" : apiCalls)}{System.Environment.NewLine}";

        try
        {
            File.AppendAllText(_currentLogPath, entry);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"[Logger] Failed to write log: {e.Message}");
        }
    }

    // =====================================================================
    //  Core workflow
    // =====================================================================

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

        userPrompt = JsonConvert.SerializeObject(userPromptJson, Formatting.Indented);

        UnityEngine.Debug.Log("<color=cyan>[Orchestrator] promp构建完成</color>");

        // 输入LLM得到skill sequence
        double llmElapsed = -1;
        if (generateSequence)
        {
            Stopwatch llmSw = Stopwatch.StartNew();
            APICalls = await skillController.GenerateSkills(speechText, userPrompt);
            llmSw.Stop();
            llmElapsed = llmSw.Elapsed.TotalSeconds;
            UnityEngine.Debug.Log($"<color=yellow>[Timer] GenerateSkillSequence 耗时: {llmElapsed:F2} s</color>");
        }

        // 执行skill sequence codes
        double executorElapsed = -1;
        if (sequenceToExecutor && !string.IsNullOrEmpty(APICalls))
        {
            Stopwatch executorSw = Stopwatch.StartNew();
            await actionExecutor.ExecuteSkillSequence(APICalls);
            executorSw.Stop();
            executorElapsed = executorSw.Elapsed.TotalSeconds;
            UnityEngine.Debug.Log($"<color=yellow>[Timer] ExecuteSkillSequence 耗时: {executorElapsed:F2} s</color>");
        }

        totalSw.Stop();
        double totalElapsed = totalSw.Elapsed.TotalSeconds;

        // voice → STT elapsed (only meaningful when voiceSendtoVLM is true)
        double voiceElapsed = -1;
        double speechToFinishElapsed = -1;
        if (voiceSendtoVLM)
        {
            _speechToFinishSw.Stop();
            speechToFinishElapsed = _speechToFinishSw.Elapsed.TotalSeconds;
            // voiceElapsed = speech-to-finish minus the workflow processing time
            voiceElapsed = speechToFinishElapsed - totalElapsed;
            if (voiceElapsed < 0) voiceElapsed = 0;
        }

        // --- Write interaction to log ---
        AppendInteractionLog(userPrompt, APICalls, llmElapsed, executorElapsed, totalElapsed, voiceElapsed, speechToFinishElapsed);

        UnityEngine.Debug.Log($"<color=yellow>[Timer] 总耗时: {totalElapsed:F2} s</color>");
        if (speechToFinishElapsed >= 0)
            UnityEngine.Debug.Log($"<color=yellow>[Timer] Voice→STT: {voiceElapsed:F2} s  |  Speech→Finish: {speechToFinishElapsed:F2} s</color>");
    }

    private void HandleInputAndLook()
    {
        if (Keyboard.current?.escapeKey.wasPressedThisFrame ?? false)
            Cursor.lockState = (Cursor.lockState == CursorLockMode.None) ? CursorLockMode.Locked : CursorLockMode.None;

        if (Cursor.lockState != CursorLockMode.Locked) return;

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
        for (int i = parentContainer.transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = parentContainer.transform.GetChild(i).gameObject;
            if (child.activeSelf)
            {
                Destroy(child);
                count++;
            }
        }
        UnityEngine.Debug.Log("[FlyIV] 已清理旧场景的可视化图表");
    }
}