using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;


public class VLMFocus : MonoBehaviour
{
    [Header("API Configuration")]
    [SerializeField] private string customApiUrl; 
    [SerializeField] private string apiKey;
    [SerializeField] private string model;

    [Header("Capture Settings")]
    public Camera targetCamera;
    public int resolution = 512;
    [Range(1, 100)] public int jpgQuality = 75;

    [Header("Debug")]
    [Tooltip("这里会实时显示最近一次的VLM检测结果")]
    public List<string> identifiedObjects = new List<string>();

    public class OpenAIResponse
    {
        public Choice[] choices;
        public class Choice { public Message message; }
        public class Message { public string content; }
    }

    public class VLMResult { public string focused_object; }

    void Start() 
    {
        customApiUrl = ApiConfig.Instance.vlmUrl; 
        apiKey = ApiConfig.Instance.apiKey;
        model = ApiConfig.Instance.vlmModel; 
    }

    [ContextMenu("Test Identify Now")]
    public async Task IdentifyFocusedObject()
    {
        // 1. 扫描场景 
        List<string> candidateNames = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
            .Where(go => go.GetComponent<Renderer>() != null && go.activeInHierarchy)
            .Select(go => go.name).Distinct().ToList();

        if (candidateNames.Count == 0) return;

        // 2. 截图
        byte[] imageBytes = CaptureAsJpg();
        
        // 3. 请求 API
        string resultJsonText = await CallOpenAICompatibleAPI(imageBytes, string.Join(", ", candidateNames));

        if (!string.IsNullOrEmpty(resultJsonText))
        {
            try 
            {
                // 清理 Markdown
                string cleanJson = resultJsonText.Replace("```json", "").Replace("```", "").Trim();
                
                // 如果模型返回的是 {"objects": ["name1"]} 这种包装格式，
                // 或者直接返回 ["name1", "name2"]，我们需要兼容处理。

                if (cleanJson.StartsWith("[")) {
                    identifiedObjects = JsonConvert.DeserializeObject<List<string>>(cleanJson);
                } else {
                    // 有些模型即便要求返回数组，也会固执地返回对象，这里做个兜底
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(cleanJson);
                    if (dict != null && dict.Count > 0) identifiedObjects = dict.Values.First();
                }

                // 4. 处理识别到的所有物体
                if (identifiedObjects != null && identifiedObjects.Count > 0)
                {

                    foreach (string name in identifiedObjects)
                    {
                        GameObject foundObj = GameObject.Find(name);
                        if (foundObj != null)
                        {
                            Debug.Log($"<color=green>[VLMFocus] 识别到物体: {foundObj.name}</color>");
                            // 测试：选中列表中的第一个
                            // #if UNITY_EDITOR
                            // if (name == identifiedObjects[0]) UnityEditor.Selection.activeGameObject = foundObj;
                            // #endif
                        }
                    }
                }
            } 
            catch (Exception e) 
            { 
                Debug.LogError($"[VLMFocus] 解析失败: {e.Message} | 原始返回: {resultJsonText}"); 
            }
        }

        // await Task.Yield();
    }

    private async Task<string> CallOpenAICompatibleAPI(byte[] imageBytes, string candidates)
    {
        string base64Image = Convert.ToBase64String(imageBytes);

        // 构建 OpenAI 视觉格式 Payload
        var payload = new {
        model = this.model,
        messages = new[] {
            new {
                role = "user",
                content = new object[] {
                    new { 
                        type = "text", 
                        // 修改后的 Prompt：要求返回数组格式
                        text = $"Identify the object(s) the user is looking at in this image. " + $"Choose ONLY from this object candidates list: [{candidates}]. " + $"Return a JSON array of object names string like: [\"name1\", \"name2\"]" 
                    },
                    new { 
                        type = "image_url", 
                        image_url = new { url = $"data:image/jpeg;base64,{base64Image}" } 
                    }
                }
            }
        },
            response_format = new { type = "json_object" },
            temperature = 0.1
        };

        string jsonPayload = JsonConvert.SerializeObject(payload);

        using (UnityWebRequest request = new UnityWebRequest(customApiUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            
            // OpenAI 必须在 Header 中携带 Authorization
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            var operation = request.SendWebRequest();
            while (!operation.isDone) await Task.Yield();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonConvert.DeserializeObject<OpenAIResponse>(request.downloadHandler.text);
                return response.choices[0].message.content;
            }
            
            Debug.LogError($"[VLMFocus] API Error: {request.error}\n{request.downloadHandler.text}");
            return null;
        }
    }

    private byte[] CaptureAsJpg()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        RenderTexture rt = RenderTexture.GetTemporary(resolution, resolution, 24);
        targetCamera.targetTexture = rt;
        targetCamera.Render();
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
        tex.Apply();
        byte[] bytes = tex.EncodeToJPG(jpgQuality);
        targetCamera.targetTexture = null;
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        Destroy(tex);
        return bytes;
    }

    /// <summary>
    /// 获取focused物体的geometry信息，用于user prompt
    /// </summary>
    public List<object> GetFocusedObjectsData()
    {
        List<object> objectsData = new List<object>();
        foreach (string objName in identifiedObjects)
        {
            GameObject go = GameObject.Find(objName);
            if (go == null) continue;

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer == null) continue;

            Bounds bounds = renderer.bounds;

            // Helper to round Vector3 to two decimal places
            Vector3 RoundVec3(Vector3 v) => new Vector3(
                (float)Math.Round(v.x, 2), 
                (float)Math.Round(v.y, 2), 
                (float)Math.Round(v.z, 2)
            );

            objectsData.Add(new
            {
                name = go.name,
                position = RoundVec3(new Vector3(bounds.center.x, bounds.max.y, bounds.center.z)),
                scale = RoundVec3(go.transform.localScale),
                boundary = new {
                    center = RoundVec3(bounds.center),
                    size = RoundVec3(bounds.size),
                    forward = RoundVec3(go.transform.forward),
                    right = RoundVec3(go.transform.right),
                    up = RoundVec3(go.transform.up)
                }
            });
        }
        return objectsData;
    }
}