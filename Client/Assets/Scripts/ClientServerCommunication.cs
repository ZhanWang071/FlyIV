using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class ClientServerCommunication : MonoBehaviour
{
    private string serverUrl = "http://127.0.0.1:5001/generate_skill";

    [Serializable]
    public class FunctionRequest {
        public string function;
        public string description;
    }

    [Serializable]
    public class PythonResponse {
        public string function;
        public string code;
    }

    [ContextMenu("Test Communication")]
    public async void TestCommunication()
    {
        var requestData = new List<FunctionRequest> {
            new FunctionRequest { function = "UPDATE(string chart_id, string element_id, string y_value)", description = "Update the value of an existing mark" },
            new FunctionRequest { function = "DELETE_ELEMENT(string chart_id, string element_id)", description = "Delete element" },
            new FunctionRequest { function = "APPEND_SIGNLE(string chart_id, string x_value, string y_value)", description = "Append a signle element" },
            new FunctionRequest { function = "APPEND_SERIES(string chart_id, List<string> x_values, List<string> y_values, int serie_index)", description = "Append a series of elements" },
            new FunctionRequest { function = "HIGHLIGHT(string chart_id, string element_id, string highlight_type)", description = "Highlight a single element" }
        };

        await SendToPythonAndSave(requestData);
    }

    public async Task SendToPythonAndSave(List<FunctionRequest> data)
    {
        string jsonPayload = JsonConvert.SerializeObject(data);
        
        using (UnityWebRequest request = new UnityWebRequest(serverUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            Debug.Log("正在发送请求至 Python...");
            var operation = request.SendWebRequest();

            while (!operation.isDone) await Task.Yield();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseJson = request.downloadHandler.text;
                var responses = JsonConvert.DeserializeObject<List<PythonResponse>>(responseJson);
                ProcessAndSave(responses);
            }
            else
            {
                Debug.LogError("通信失败: " + request.error);
            }
        }
    }

    private void ProcessAndSave(List<PythonResponse> responses)
    {
        string savePath = Path.Combine(Application.streamingAssetsPath, "Skills");
        if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);

        foreach (var res in responses)
        {
            // 1. 命名转换: DELETE_ELEMENT -> DeleteElement
            string className = FormatToPascalCase(res.function);
            string fileName = $"{className}.cs";
            string fullPath = Path.Combine(savePath, fileName);

            // 2. 存储代码
            File.WriteAllText(fullPath, res.code);
            Debug.Log($"<color=lime>已生成文件: {fileName}</color> 路径: {fullPath}");
        }
    }

    private string FormatToPascalCase(string input)
    {
        // 将 UPDATE_STUFF 转换为 UpdateStuff
        string[] words = input.ToLower().Split('_');
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
        }
        return string.Join("", words);
    }
}