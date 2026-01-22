using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
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
    public List<string> identifiedObjects = new List<string> { "TestCube" };
    
    [TextArea(10, 20)]
    public string objectsDataDisplay;
    
    public System.Action<string> OnVLMFocusFinished;
    
    // 日志文件路径
    private static string _currentLogFilePath;
    private static readonly object _logLock = new object();

    public class OpenAIResponse
    {
        public Choice[] choices;
        public class Choice { public Message message; }
        public class Message { public string content; }
    }

    public class VLMResult { public string focused_object; }
    
    // 用于序列化的简单数据结构
    [System.Serializable]
    public class Vector3Data
    {
        public float x;
        public float y;
        public float z;
        
        public Vector3Data(Vector3 v)
        {
            x = (float)Math.Round(v.x, 2);
            y = (float)Math.Round(v.y, 2);
            z = (float)Math.Round(v.z, 2);
        }
    }
    
    [System.Serializable]
    public class ObjectData
    {
        public string name;
        public Vector3Data position;
        public Vector3Data scale;
        public BoundaryData boundary;
    }
    
    [System.Serializable]
    public class BoundaryData
    {
        public Vector3Data center;
        public Vector3Data size;
        public Vector3Data forward;
        public Vector3Data right;
        public Vector3Data up;
    }

    void Start() 
    {
        customApiUrl = ApiConfig.Instance.vlmUrl; 
        apiKey = ApiConfig.Instance.apiKey;
        model = ApiConfig.Instance.vlmModel; 

        targetCamera = Camera.main;
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
                    
                    // 5. 获取物体数据并触发完成事件
                    GetFocusedObjectsData();
                    
                    if (OnVLMFocusFinished != null && !string.IsNullOrEmpty(objectsDataDisplay))
                    {
                        OnVLMFocusFinished.Invoke(objectsDataDisplay);
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
    [ContextMenu("Get Objects Geometry Data")]
    public List<object> GetFocusedObjectsData()
    {
        List<ObjectData> objectsData = new List<ObjectData>();
        foreach (string objName in identifiedObjects)
        {
            GameObject go = GameObject.Find(objName);
            if (go == null) continue;

            // 获取合并后的边界（包括子对象的 Renderer）
            Bounds? bounds = GetCombinedBounds(go);
            if (bounds == null) continue;

            Bounds combinedBounds = bounds.Value;

            objectsData.Add(new ObjectData
            {
                name = go.name,
                position = new Vector3Data(new Vector3(combinedBounds.center.x, combinedBounds.max.y, combinedBounds.center.z)),
                scale = new Vector3Data(go.transform.localScale),
                boundary = new BoundaryData
                {
                    center = new Vector3Data(combinedBounds.center),
                    size = new Vector3Data(combinedBounds.size),
                    forward = new Vector3Data(go.transform.forward),
                    right = new Vector3Data(go.transform.right),
                    up = new Vector3Data(go.transform.up)
                }
            });
        }
        
        // 将数据序列化为格式化的JSON字符串
        objectsDataDisplay = JsonConvert.SerializeObject(objectsData, Formatting.Indented);
        
        // 记录 identifiedObjects, objectsDataDisplay 到日志文件
        LogVLMResult();

        // 为了保持返回类型兼容，转换为List<object>
        return objectsData.Cast<object>().ToList();
    }

    /// <summary>
    /// 获取 GameObject 的合并边界，包括自身和所有子对象的 Renderer
    /// </summary>
    private Bounds? GetCombinedBounds(GameObject go)
    {
        List<Renderer> renderers = new List<Renderer>();

        // 检查自身是否有 Renderer
        Renderer selfRenderer = go.GetComponent<Renderer>();
        if (selfRenderer != null)
        {
            renderers.Add(selfRenderer);
        }

        // 查找所有子对象中的 Renderer
        Renderer[] childRenderers = go.GetComponentsInChildren<Renderer>();
        foreach (Renderer childRenderer in childRenderers)
        {
            // 排除自身（如果自身有 Renderer，已经在上面添加了）
            if (childRenderer != selfRenderer)
            {
                renderers.Add(childRenderer);
            }
        }

        // 如果没有找到任何 Renderer，返回 null
        if (renderers.Count == 0)
        {
            return null;
        }

        // 合并所有 Renderer 的边界
        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Count; i++)
        {
            combinedBounds.Encapsulate(renderers[i].bounds);
        }

        return combinedBounds;
    }

    /// <summary>
    /// 创建日志文件
    /// </summary>
    private void CreateLogFile()
    {
        if (!string.IsNullOrEmpty(_currentLogFilePath) && File.Exists(_currentLogFilePath)) return; // 日志文件已存在

        // 生成日期戳格式：yyyyMMdd_HHmmss
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"SceneGraph_{timestamp}.txt";
        _currentLogFilePath = Path.Combine(Application.dataPath, "Logs", fileName);
    }

    /// <summary>
    /// 记录 identifiedObjects 到日志文件
    /// </summary>
    private void LogVLMResult()
    {
        if (identifiedObjects == null || identifiedObjects.Count == 0)
        {
            return;
        }

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"SceneGraph_{timestamp}.txt";
        _currentLogFilePath = Path.Combine(Application.dataPath, "Logs", fileName);

        lock (_logLock)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(_currentLogFilePath, true))
                {
                    writer.WriteLine($"--- Identified Objects Detection Result: ({identifiedObjects.Count}) ---");
                    writer.WriteLine(JsonConvert.SerializeObject(identifiedObjects));
                    writer.WriteLine();

                    writer.WriteLine($"--- Objects Geometry Data: ({identifiedObjects.Count}) ---");
                    writer.WriteLine(objectsDataDisplay);
                    writer.WriteLine();
                }

                Debug.Log($"[VLMFocus] 目标物体检测完成，记录到Log文件");
            }
            catch (Exception e)
            {
                Debug.LogError($"[VLMFocus] 写入日志失败: {e.Message}");
            }
        }
    }

    /// <summary>
    /// 获取当前日志文件路径（供 RelationDetection 使用）
    /// </summary>
    public string GetCurrentLogFilePath()
    {
        if (!File.Exists(_currentLogFilePath) || string.IsNullOrEmpty(_currentLogFilePath))  CreateLogFile();
        return _currentLogFilePath;
    }

    /// ------------ Test Cases ----------------

    [ContextMenu("Test Case 1")]
    public void testCase1()
    {
        // 设置 Main Camera 的 World Transform
        Vector3 position = new Vector3(
            0.405f,    // X (0.4047400951385498)
            1.525f,   // Y (1.524625301361084)
            -0.605f   // Z (-0.604840874671936)
        );
        
        // Rotation 使用四元数格式 (x, y, z, w)
        Quaternion rotationQuaternion = new Quaternion(
            0.002f,   // X (0.0016219038516283036)
            0.998f,   // Y (0.9980877637863159)
            -0.030f,  // Z (-0.029951637610793115)
            0.054f    // W (0.054047852754592898)
        );

        // 应用 Transform
        targetCamera.transform.position = position;
        targetCamera.transform.rotation = rotationQuaternion;

        identifiedObjects = new List<string> {"Clock","Blackboard","Bookcase","Globe","TeacherDesk"};
        GetFocusedObjectsData();
        OnVLMFocusFinished.Invoke(objectsDataDisplay);
        
    }

    [ContextMenu("Test Case 2")]
    public void testCase2()
    {
        // 设置 Main Camera 的 World Transform
        Vector3 position = new Vector3(
            -0.778f,   // X (-0.7779147624969482)
            1.440f,   // Y (1.4396485090255738)
            -2.812f   // Z (-2.81184720993042)
        );
        
        // Rotation 使用四元数格式 (x, y, z, w)
        Quaternion rotationQuaternion = new Quaternion(
            0.034f,   // X (0.03406103327870369)
            0.907f,   // Y (0.9074878096580505)
            -0.075f,  // Z (-0.07503792643547058)
            0.412f    // W (0.4119164049625397)
        );

        // 应用 Transform
        targetCamera.transform.position = position;
        targetCamera.transform.rotation = rotationQuaternion;

        identifiedObjects = new List<string> {"Blackboard","Bookcase","Globe","TeacherDesk"};
        GetFocusedObjectsData();
        OnVLMFocusFinished.Invoke(objectsDataDisplay);
    }

    [ContextMenu("Test Case 3")]
    public void testCase3()
    {
        // 设置 Main Camera 的 World Transform
        Vector3 position = new Vector3(
            0.091f,   // X (0.09073066711425781)
            1.878f,   // Y (1.8782755136489869)
            3.313f    // Z (3.3132362365722658)
        );
        
        // Rotation 使用四元数格式 (x, y, z, w)
        Quaternion rotationQuaternion = new Quaternion(
            0.111f,   // X (0.11076590418815613)
            0.011f,   // Y (0.010631740093231202)
            -0.001f,  // Z (-0.001184958964586258)
            0.994f    // W (0.9937889575958252)
        );

        // 应用 Transform
        targetCamera.transform.position = position;
        targetCamera.transform.rotation = rotationQuaternion;

        identifiedObjects = new List<string> {"DeskAndChair (15)","DeskAndChair (13)","DeskAndChair (17)","DeskAndChair (21)", "DeskAndChair (22)", "DeskAndChair (23)", "DeskAndChair (20)", "DeskAndChair (19)", "BackDoor", "Shelf", "Noticeboard", ""};
        GetFocusedObjectsData();
        OnVLMFocusFinished.Invoke(objectsDataDisplay);
    }
}