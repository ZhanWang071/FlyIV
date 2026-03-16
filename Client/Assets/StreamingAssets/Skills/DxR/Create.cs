public class Create
{
    public static void Execute(string view_id, string data_path, string chart_type,string x_field, string y_field, string color_field="")
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

        // ── 1. 根据 chart_type 确定 mark 和 spec 策略 ─────────────────────
        string chartTypeLower = chart_type != null ? chart_type.ToLower().Trim() : "bar";

        // ── 2. 构建 spec JSON ──────────────────────────────────────────────
        JSONObject specJson = new JSONObject();

        JSONObject dataNode = new JSONObject();
        dataNode["url"] = new JSONString(data_path);
        specJson["data"] = dataNode;

        JSONObject encodingNode = new JSONObject();

        if (chartTypeLower == "scatter" || chartTypeLower == "point")
        {
            // ── Scatter Plot：sphere mark，x/y 均为 quantitative ────────────
            specJson["mark"] = new JSONString("sphere");

            JSONObject xEnc = new JSONObject();
            xEnc["field"] = new JSONString(x_field);
            xEnc["type"] = new JSONString("quantitative");
            encodingNode["x"] = xEnc;

            JSONObject yEnc = new JSONObject();
            yEnc["field"] = new JSONString(y_field);
            yEnc["type"] = new JSONString("quantitative");
            encodingNode["y"] = yEnc;
        }
        else if (chartTypeLower == "bar_horizontal")
        {
            // ── 水平 Bar Chart：cube mark，x=quantitative, y=nominal ─────────
            specJson["mark"] = new JSONString("cube");

            JSONObject xEnc = new JSONObject();
            xEnc["field"] = new JSONString(y_field);
            xEnc["type"] = new JSONString("quantitative");
            encodingNode["x"] = xEnc;

            JSONObject yEnc = new JSONObject();
            yEnc["field"] = new JSONString(x_field);
            yEnc["type"] = new JSONString("nominal");
            encodingNode["y"] = yEnc;

            // 关键：width encoding 让柱子长度随数据变化
            JSONObject widthEnc = new JSONObject();
            widthEnc["field"] = new JSONString(y_field);
            widthEnc["type"] = new JSONString("quantitative");
            encodingNode["width"] = widthEnc;

            // 关键：xoffsetpct -0.5 让柱子从左侧基线向右生长
            JSONObject xOffsetEnc = new JSONObject();
            xOffsetEnc["value"] = new JSONNumber(-0.5);
            encodingNode["xoffsetpct"] = xOffsetEnc;
        }
        else if (chartTypeLower == "line")
        {
            // ── Line Chart：DxR 无 line mark，用 tick 近似 ─────────────────
            specJson["mark"] = new JSONString("tick");

            JSONObject xEnc = new JSONObject();
            xEnc["field"] = new JSONString(x_field);
            xEnc["type"] = new JSONString("quantitative");
            encodingNode["x"] = xEnc;

            JSONObject yEnc = new JSONObject();
            yEnc["field"] = new JSONString(y_field);
            yEnc["type"] = new JSONString("quantitative");
            encodingNode["y"] = yEnc;
        }
        else
        {
            // ── 默认：垂直 Bar Chart（官方 barchart_vertical.json 格式）──────
            // mark = cube，x=nominal, y=quantitative
            // + height encoding（柱高）+ yoffsetpct -0.5（从底部生长）
            specJson["mark"] = new JSONString("cube");

            JSONObject xEnc = new JSONObject();
            xEnc["field"] = new JSONString(x_field);
            xEnc["type"] = new JSONString("nominal");
            encodingNode["x"] = xEnc;

            JSONObject yEnc = new JSONObject();
            yEnc["field"] = new JSONString(y_field);
            yEnc["type"] = new JSONString("quantitative");
            encodingNode["y"] = yEnc;

            // 关键：height encoding 让柱子高度随数据变化
            JSONObject heightEnc = new JSONObject();
            heightEnc["field"] = new JSONString(y_field);
            heightEnc["type"] = new JSONString("quantitative");
            encodingNode["height"] = heightEnc;

            // 关键：yoffsetpct -0.5 让柱子从底部基线向上生长
            JSONObject yOffsetEnc = new JSONObject();
            yOffsetEnc["value"] = new JSONNumber(-0.5);
            encodingNode["yoffsetpct"] = yOffsetEnc;
        }

        // ── 可选：color encoding ──────────────────────────────────────────
        if (!string.IsNullOrEmpty(color_field))
        {
            JSONObject colorEnc = new JSONObject();
            colorEnc["field"] = new JSONString(color_field);
            colorEnc["type"] = new JSONString("nominal");
            encodingNode["color"] = colorEnc;
        }

        specJson["encoding"] = encodingNode;

        // ── 3. 写入磁盘 ────────────────────────────────────────────────────
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

        // ── 5. 实例化 prefab ───────────────────────────────────────────────
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/DxR/Prefabs/DxRVis.prefab");
        if (prefab == null)
        {
            Debug.LogWarning("CreateSkill: Could not load DxRVis prefab from AssetDatabase.");
            return;
        }

        bool prefabWasActive = prefab.activeSelf;
        prefab.SetActive(false);
        GameObject root = GameObject.Instantiate(prefab);
        prefab.SetActive(prefabWasActive);

        root.name = view_id;

        // ── 6. 获取 Vis 组件，赋值后激活 ──────────────────────────────────
        Vis vis = root.GetComponent<Vis>();
        if (vis == null)
            vis = root.GetComponentInChildren<Vis>(true);

        if (vis == null)
        {
            Debug.LogWarning("CreateSkill: DxRVis prefab does not have a Vis component.");
            UnityEngine.Object.Destroy(root);
            return;
        }

        vis.visSpecsURL = specFileName;
        root.SetActive(true);

        Debug.Log("CreateSkill completed: view=" + view_id + " data=" + data_path
            + " chartType=" + chartTypeLower + " x=" + x_field + " y=" + y_field);

        existingObj.tag = "Visualization_3D";
        GameObject parentContainer = GameObject.Find("VisObject");
        existingObj.transform.SetParent(parentContainer.transform);
    }
}