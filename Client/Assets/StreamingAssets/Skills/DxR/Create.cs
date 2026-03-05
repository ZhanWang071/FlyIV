using UnityEngine;
using DxR;
using SimpleJSON;
using System.Collections.Generic;
using System.IO;

public class Create
{
    public static void Execute(string view_id, string data_path, string chart_type, string x_field, string y_field)
    {
        Debug.Log("Executing CreateSkill...");

        if (string.IsNullOrEmpty(view_id))
        {
            Debug.LogWarning("CreateSkill: view_id is null or empty.");
            return;
        }

        if (string.IsNullOrEmpty(data_path))
        {
            Debug.LogWarning("CreateSkill: data_path is null or empty.");
            return;
        }

        if (string.IsNullOrEmpty(x_field) || string.IsNullOrEmpty(y_field))
        {
            Debug.LogWarning("CreateSkill: x_field or y_field is null or empty.");
            return;
        }

        // ── 1. 确定 mark 类型 ──────────────────────────────────────────────
        string markType = "bar";
        string chartTypeLower = chart_type != null ? chart_type.ToLower().Trim() : "bar";
        if (chartTypeLower == "point" || chartTypeLower == "scatter")
        {
            markType = "sphere";
        }
        else if (chartTypeLower == "line" || chartTypeLower == "bar")
        {
            markType = "cube";
        }

        // ── 2. 构建 spec JSON ──────────────────────────────────────────────
        JSONObject specJson = new JSONObject();

        JSONObject dataNode = new JSONObject();
        dataNode["url"] = new JSONString(data_path);
        specJson["data"] = dataNode;

        specJson["mark"] = new JSONString(markType);

        JSONObject encodingNode = new JSONObject();

        JSONObject xEncoding = new JSONObject();
        xEncoding["field"] = new JSONString(x_field);
        xEncoding["type"] = new JSONString("nominal");
        encodingNode["x"] = xEncoding;

        JSONObject yEncoding = new JSONObject();
        yEncoding["field"] = new JSONString(y_field);
        yEncoding["type"] = new JSONString("quantitative");
        encodingNode["y"] = yEncoding;

        specJson["encoding"] = encodingNode;

        // ── 3. 将 spec 写入磁盘 ────────────────────────────────────────────
        string specFileName = view_id + ".json";
        string specFilePath = Parser.GetFullSpecsPath(specFileName);

        try
        {
            File.WriteAllText(specFilePath, specJson.ToString(2));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("CreateSkill: Failed to write spec file: " + e.Message);
            return;
        }

        // ── 4. 若已存在同名 Vis，直接更新 ─────────────────────────────────
        GameObject existingObj = GameObject.Find(view_id);
        if (existingObj != null)
        {
            Vis existingVis = existingObj.GetComponent<Vis>();
            if (existingVis != null)
            {
                existingVis.visSpecsURL = specFileName;
                existingVis.UpdateVisSpecsFromTextSpecs();
                Debug.Log("CreateSkill: updated existing vis '" + view_id + "'.");
                return;
            }
        }

        // ── 5. 通过 prefab 实例化新的 DxRVis ──────────────────────────────
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/DxR/Prefabs/DxRVis.prefab");

        if (prefab == null)
        {
            Debug.LogWarning("CreateSkill: Could not load DxRVis prefab from AssetDatabase.");
            return;
        }

        // 先将 prefab 设为非激活再实例化，防止 Awake 在赋值前提前触发
        bool prefabWasActive = prefab.activeSelf;
        prefab.SetActive(false);
        GameObject root = GameObject.Instantiate(prefab);
        prefab.SetActive(prefabWasActive); // 恢复 prefab 原状，不影响 Asset

        root.name = view_id;

        // ── 6. 赋值 visSpecsURL，然后激活让 Start() 自动完成初始化 ─────────
        Vis vis = root.GetComponent<Vis>();
        if (vis == null)
        {
            Debug.LogWarning("CreateSkill: DxRVis prefab does not have a Vis component.");
            Object.Destroy(root);
            return;
        }

        // 在 Awake/Start 触发前设置 URL，Start() 会用它自动读取并渲染 spec
        vis.visSpecsURL = specFileName;

        // 激活对象 → 触发 Awake/Start → Vis 完成 parser 初始化 + spec 加载
        // 不需要手动调用 UpdateVisSpecsFromTextSpecs()
        root.SetActive(true);

        Debug.Log("CreateSkill completed: view=" + view_id + " data=" + data_path
            + " mark=" + markType + " x=" + x_field + " y=" + y_field);
    }
}