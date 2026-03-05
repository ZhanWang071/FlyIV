public class Create
{
    public static void Execute(
        string view_id,
        string data_path,
        string chart_type,
        string x_field,
        string y_field)
    {
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
        "Assets/DxR/Prefabs/DxRVis.prefab");

        if (prefab == null)
        {
            Debug.LogWarning("CreateSkill: Could not load DxRVis prefab from AssetDatabase.");
            return;
        }

        GameObject root = GameObject.Instantiate(prefab);
        
        
        root.name = view_id;

        Vis vis = root.GetComponent<Vis>();
        vis.visSpecsURL = "Examples/barchart_book.json";

        root.SetActive(true);
        vis.UpdateVis();
    }

    // -------------------------------------------------------------------------
    // Validation
    // -------------------------------------------------------------------------

    private static bool ValidateInputs(string view_id, string data_path, string x_field, string y_field)
    {
        if (string.IsNullOrEmpty(view_id))
        {
            Debug.LogWarning("CreateSkill: view_id is null or empty.");
            return false;
        }
        if (string.IsNullOrEmpty(data_path))
        {
            Debug.LogWarning("CreateSkill: data_path is null or empty.");
            return false;
        }
        if (string.IsNullOrEmpty(x_field) || string.IsNullOrEmpty(y_field))
        {
            Debug.LogWarning("CreateSkill: x_field or y_field is null or empty.");
            return false;
        }
        return true;
    }

    // -------------------------------------------------------------------------
    // Mark Type Resolution
    // -------------------------------------------------------------------------

    private static string ResolveMarkType(string chart_type)
    {
        string ct = chart_type?.ToLower().Trim() ?? "bar";
        return (ct == "point" || ct == "scatter") ? "sphere" : "bar";
    }

    // -------------------------------------------------------------------------
    // Spec Building
    // -------------------------------------------------------------------------

    private static JSONObject BuildSpec(string data_path, string markType, string x_field, string y_field)
    {
        JSONObject spec = new JSONObject();
        spec["data"] = new JSONObject();
        spec["data"]["url"] = new JSONString(data_path);
        spec["mark"] = new JSONString(markType);
        spec["encoding"] = new JSONObject();
        spec["encoding"]["x"] = BuildEncoding(x_field, "nominal");
        spec["encoding"]["y"] = BuildEncoding(y_field, "quantitative");
        return spec;
    }

    private static JSONObject BuildEncoding(string field, string type)
    {
        JSONObject enc = new JSONObject();
        enc["field"] = new JSONString(field);
        enc["type"] = new JSONString(type);
        return enc;
    }

    // -------------------------------------------------------------------------
    // File I/O
    // -------------------------------------------------------------------------

    private static bool TryWriteSpec(string specFileName, JSONObject specJson)
    {
        try
        {
            File.WriteAllText(Parser.GetFullSpecsPath(specFileName), specJson.ToString(2));
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"CreateSkill: Failed to write spec file: {e.Message}");
            return false;
        }
    }

    // -------------------------------------------------------------------------
    // Vis Management
    // -------------------------------------------------------------------------

    private static bool TryUpdateExisting(string view_id, string specFileName)
    {
        GameObject existingObj = GameObject.Find(view_id);
        if (existingObj == null) return false;

        Vis vis = existingObj.GetComponent<Vis>();
        if (vis == null) return false;

        vis.visSpecsURL = specFileName;
        vis.UpdateVisSpecsFromTextSpecs();
        Debug.Log($"CreateSkill: updated existing vis '{view_id}'.");
        return true;
    }

    private static void CreateNewVis(string view_id, string specFileName)
    {
        GameObject root = new GameObject(view_id);
        root.tag = "DxRVis";
        root.SetActive(false);

        GameObject dxrView = CreateChild(root, "DxRView");
        CreateChild(dxrView, "DxRMarks");
        CreateChild(dxrView, "DxRGuides");

        GameObject dxrInteractions = CreateChild(root, "DxRInteractions");
        dxrInteractions.AddComponent<Interactions>();

        CreateChild(root, "DxRGUI");

        Vis vis = root.AddComponent<Vis>();
        vis.visSpecsURL = specFileName;

        root.SetActive(true);
    }

    private static GameObject CreateChild(GameObject parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent.transform, false);
        return child;
    }
}