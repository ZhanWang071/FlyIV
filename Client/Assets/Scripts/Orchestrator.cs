using UnityEngine;
using Newtonsoft.Json;
using System.Threading.Tasks;

public class Orchestrator : MonoBehaviour
{
    [Header("Reference Settings")]
    public SpeechToText sttHandler;
    public VLMFocus vlmHandler;
    public InteractionTracker interactionTracker;
    public SkillController skillController;
    public ActionExecutor actionExecutor;
    public Transform playerCamera;

    private void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main.transform;
        
        sttHandler.OnTranscribeFinished += (speechText) => {
            _ = ProcessWorkflow(speechText);
        };
    }

    /// <summary>
    /// 核心工作流：STT -> VLM(等待) -> 整合数据 -> LLM
    /// </summary>
    private async Task ProcessWorkflow(string speechText)
    {
        Debug.Log("<color=cyan>[Orchestrator] 收到语音，开始执行 VLM 识别...</color>");

        // 语音输入结束时，VLM 开始识别用户视角下当前场景中的物体
        await vlmHandler.IdentifyFocusedObject();

        Debug.Log("<color=cyan>[Orchestrator] VLM 识别完成，开始发送user Prompt...</color>");

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
        string userPrompt = JsonConvert.SerializeObject(userPromptJson, Formatting.Indented); 
        
        // 输入LLM得到skill sequence
        string skillsResponse = await skillController.GenerateSkills(speechText, userPrompt);

        // 执行skill sequence codes
        await actionExecutor.ExecuteSkillSequence(skillsResponse);
    }

}