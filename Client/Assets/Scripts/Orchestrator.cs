using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using UnityEngine.InputSystem;
using System.Diagnostics;
using System.IO;
using System;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using TMPro;

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
    public Transform cameraRig;
    public GameObject Environment;
    public bool mouseControl = true;
    private float moveSpeed = 1.0f;

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
    [TextArea(2, 5)]
    [SerializeField] private string pointingObjectDuringSpeech = "";
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

    [Header("UI Feedback Settings")]
    // public float uiDistance = 0.2f; // 文字离相机的距离
    private GameObject _feedbackPanel;
    private TextMeshProUGUI _feedbackText;
    private CanvasGroup _panelCanvasGroup;
    

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        if (playerCamera == null) playerCamera = Camera.main.transform;

        participantID = userStudyController.participantID;

        InitLogFile();

        CreateFeedbackUI(); // 初始化UI

        // 语音开始时执行VLM识别，充分利用语音录制的时间
        if (sttHandler != null)
        {
            sttHandler.OnSpeechStarted += () => {
                _speechToFinishSw.Restart(); // begin speech-to-finish timing
                ShowFeedback("Recording...", true); // 展示正在翻译
                _vlmTask = vlmHandler.IdentifyFocusedObject();
            };

            sttHandler.OnSpeechFinished += () =>
            {
                ShowFeedback("Stop Recording. Translating...", true);
            };
        }

        // 语音结束时处理结果（VLM可能已完成）
        if (voiceSendtoVLM) sttHandler.OnTranscribeFinished += (speechText) => {
            string displayText = string.IsNullOrEmpty(speechText) ? "请重新输入" : speechText;
            ShowFeedback(displayText, true); // 展示识别结果

            _ = ProcessWorkflow(speechText);
            userRequest = speechText;
        };
    }

    private void Update()
    {
        if (mouseControl) HandleInputAndLook();
        ControllerInput();
        pointingObject = interactionTracker.GetCurrentPointingObjectName();

        // Detect scene changes driven by UserStudyController
        if (userStudyController != null &&
            (_logInitialized == false || userStudyController.currentScene != _lastSceneType))
        {
            InitLogFile();
        }

        if (_feedbackPanel != null && _feedbackPanel.activeSelf)
        {
            UpdateFeedbackPosition();
        }
    }

    private void ControllerInput()
    {
        // 1. 获取左手摇杆输入 (PrimaryThumbstick 对应左手)
        // 返回的是 Vector2 (x 为左右, y 为前后)
        Vector2 thumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

        if (thumbstick.sqrMagnitude > 0.01f)
        {
            // 2. 获取相机的方向，并抹平 Y 轴（防止抬头时往天上飞）
            Vector3 forward = playerCamera.forward;
            Vector3 right = playerCamera.right;
            // forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            // 3. 计算最终移动向量
            // 摇杆 y 对应相机的 forward，摇杆 x 对应相机的 right
            Vector3 moveDirection = (forward * thumbstick.y + right * thumbstick.x).normalized;

            // 4. 移动整个 Camera Rig 
            cameraRig.position += moveDirection * moveSpeed * Time.deltaTime;
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
        if (string.IsNullOrEmpty(speechText))
        {
            await Task.Delay(2000); // 让“请重新输入”显示一会儿
            HideFeedback();
            return;
        }

        ShowFeedback(speechText+"\nThinking..."); // 进入思考状态

        Stopwatch totalSw = Stopwatch.StartNew();

        UnityEngine.Debug.Log("<color=cyan>[Orchestrator] 收到语音，开始执行 VLM 识别...</color>");

        UnityEngine.Debug.Log("<color=cyan>[Orchestrator] VLM 识别完成，开始发送user Prompt...</color>");

        identifiedObjects = vlmHandler.identifiedObjects != null ?
            string.Join(", ", vlmHandler.identifiedObjects) :
            "None";
        pointingObjectDuringSpeech = interactionTracker.GetCurrentPointingObjectName();

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

        UnityEngine.Debug.Log("<color=cyan>[Orchestrator] 构建Prompt");

        // 构建 user prompt JSON 数据
        var userPromptJson = new
        {
            user_status = new
            {
                position = playerCamera.position,
                forward = playerCamera.forward,
                right = playerCamera.right
            },
            scene_graph = relationDetector.GetSceneGraphData(),
            // focused_objects = vlmHandler.GetFocusedObjectsData(),
            hit_points = interactionTracker.GetHitPointsData(),
            user_request = speechText,
            data_info = dataInfoObj
        };

        UnityEngine.Debug.Log("<color=cyan>[Orchestrator] promp构建完成</color>");
        var settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            MaxDepth = 10 // 限制深度，防止无限递归
        };
        userPrompt = JsonConvert.SerializeObject(userPromptJson, Formatting.Indented, settings);

        UnityEngine.Debug.Log("<color=cyan>[Orchestrator] promp构建完成</color>");

        // 输入LLM得到skill sequence
        double llmElapsed = -1;
        bool message = false;
        if (generateSequence)
        {
            Stopwatch llmSw = Stopwatch.StartNew();
            APICalls = await skillController.GenerateSkills(speechText, userPrompt);
            llmSw.Stop();
            llmElapsed = llmSw.Elapsed.TotalSeconds;

            message = TryShowMessageFromAPI(APICalls);
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

        if (!message) ShowFeedback("Task done. Input next command...");

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
            cameraRig.Rotate(0, d.x, 0, Space.World);
            cameraRig.Rotate(-d.y, 0, 0, Space.Self);
        }
        if (Keyboard.current != null)
        {
            var k = Keyboard.current;
            Vector3 dir = (cameraRig.forward * (k.wKey.ReadValue() - k.sKey.ReadValue()) +
                           cameraRig.right * (k.dKey.ReadValue() - k.aKey.ReadValue()));
            cameraRig.position += dir * 3.0f * Time.deltaTime;
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

    // =====================================================================
    //  UI Feedback Logic
    // =====================================================================

    private void CreateFeedbackUI()
    {
        // 创建 Canvas
        GameObject canvasGO = new GameObject("FeedbackCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 100;
        _panelCanvasGroup = canvasGO.AddComponent<CanvasGroup>();

        // 创建背景 Image
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        Image bgImage = bgGO.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.8f); // 黑色透明度50%
        bgImage.raycastTarget = false;
        bgImage.sprite = null;
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(1000, 160);
        bgRect.pivot = new Vector2(0.5f, 0f);
        bgRect.anchoredPosition = Vector2.zero;

        var fitter = bgGO.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var layout = bgGO.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(40, 40, 20, 20); // 左右边距40，上下20
        layout.childAlignment = TextAnchor.MiddleCenter;
        // 强制子物体扩展，这样 LayoutElement 才会生效
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;

        // 创建文本
        GameObject textGO = new GameObject("FeedbackText");
        textGO.transform.SetParent(bgGO.transform, false);
        _feedbackText = textGO.AddComponent<TextMeshProUGUI>();
        _feedbackText.alignment = TextAlignmentOptions.Left;
        _feedbackText.fontSize = 30;
        _feedbackText.color = Color.white;
        _feedbackText.text = "";
        _feedbackText.raycastTarget = false;
        _feedbackText.enableWordWrapping = true;
        _feedbackText.overflowMode = TextOverflowModes.Overflow;
        // _feedbackText.rectTransform.sizeDelta = new Vector2(1000, 0);
        var le = textGO.AddComponent<LayoutElement>();
        le.preferredWidth = 800;

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.sizeDelta = new Vector2(-60, -20);
        textRect.anchoredPosition = new Vector2(0, 10);

        _feedbackPanel = canvasGO;
        _feedbackPanel.transform.localScale = Vector3.one * 0.001f; // 缩放到合适大小
        _feedbackPanel.SetActive(true);

        ShowFeedback("Please input voice command...");
        DontDestroyOnLoad(_feedbackPanel);
    }

    private void ShowFeedback(string message, bool keepVisible = true)
    {
        if (_feedbackPanel == null) return;
        if (!_feedbackPanel.activeSelf) _feedbackPanel.SetActive(true);
        _feedbackText.text = message;
        _panelCanvasGroup.alpha = 1f;

        UpdateFeedbackPosition();

        // // 将面板放置在相机正前方
        // Vector3 targetPos = playerCamera.position + playerCamera.forward * uiDistance;
        // _feedbackPanel.transform.position = targetPos;
        // _feedbackPanel.transform.LookAt(playerCamera.position);
        // _feedbackPanel.transform.Rotate(0, 180, 0); // 让文字正对用户
    }

    private void HideFeedback()
    {
        if (_feedbackPanel != null) _feedbackPanel.SetActive(false);
    }

    private bool TryShowMessageFromAPI(string apiCalls)
    {
        if (string.IsNullOrEmpty(apiCalls)) return false;

        var match = Regex.Match(apiCalls, @"MESSAGE\s*\(\s*""([^""]*)""\s*\)");

        if (match.Success)
        {
            string extractedText = match.Groups[1].Value;
            UnityEngine.Debug.Log($"<color=green>[Orchestrator] 解析到 MESSAGE 指令: {extractedText}</color>");

            ShowFeedback(extractedText);

            return true;
        }

        return false;
    }

    private void UpdateFeedbackPosition()
    {
        // 1. 计算目标位置
        Vector3 forwardDir = playerCamera.forward;
        forwardDir.y = 0; // 过滤掉俯仰角，只用水平前方

        // 核心位置参数：
        float distance = 0.5f; // 距离用户 1米 (太远看不清，太近有眩晕感)
        float heightOffset = -0.35f; // 向下偏移 35 厘米 (避开屏幕中心，接近你的截图下方)

        // 目标位置：相机在水平面上的前方 1米，再向下移
        Vector3 targetPos = playerCamera.position + forwardDir.normalized * distance + Vector3.up * heightOffset;

        // 平滑跟随
        _feedbackPanel.transform.position = Vector3.Lerp(_feedbackPanel.transform.position, targetPos, Time.deltaTime * 8.0f);

        // 2. 计算朝向 (Billboard)
        // 方案 A：完全正对用户相机 (如果只是平移，可能会卡在视线下方看不到)
        // 方案 B：让面板“微仰” (像放在桌子上的名牌一样，面向用户眼睛)

        // 我们使用方案 B：
        // 计算从面板位置指向用户眼睛（playerCamera）的向量
        Vector3 lookAtDir = playerCamera.position - _feedbackPanel.transform.position;
        lookAtDir.y += 0.1f; // 核心：增加一点 Y 轴偏移，让它稍微垂直一点，更好看

        if (lookAtDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookAtDir);
            // 如果文字反了，可能需要在此基础上再旋转 180 度。
            _feedbackPanel.transform.rotation = targetRotation * Quaternion.Euler(0, 180, 0); 
            // _feedbackPanel.transform.rotation = targetRotation;
        }
    }
}