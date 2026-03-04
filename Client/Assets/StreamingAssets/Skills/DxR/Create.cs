public class Create
{
    public static void Execute(
        string view_id,
        string data_path,
        string chart_type,
        string x_field,
        string y_field)
    {
        Debug.Log("Executing CreateSkill...");

        if (!ValidateInputs(view_id, data_path, x_field, y_field)) return;

        Vis vis = FindVis(view_id);
        if (vis == null) return;

        JSONNode visSpecs = vis.GetVisSpecs();
        if (visSpecs == null)
        {
            Debug.LogWarning($"CreateSkill: visSpecs is null for '{view_id}'.");
            return;
        }

        string markType = ResolveMarkType(chart_type);

        ApplySpecs(visSpecs, data_path, markType, x_field, y_field);

        vis.UpdateVis();

        Debug.Log($"CreateSkill completed: view={view_id} data={data_path} " +
                  $"mark={markType} x={x_field} y={y_field}");
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
    // Spec Application
    // -------------------------------------------------------------------------

    private static void ApplySpecs(
        JSONNode visSpecs, string data_path, string markType, string x_field, string y_field)
    {
        visSpecs["data"]["url"] = new JSONString(data_path);
        visSpecs["mark"] = new JSONString(markType);

        if (visSpecs["encoding"] == null)
            visSpecs["encoding"] = new JSONObject();

        visSpecs["encoding"]["x"] = BuildEncoding(x_field, "nominal");
        visSpecs["encoding"]["y"] = BuildEncoding(y_field, "quantitative");
    }

    private static JSONNode BuildEncoding(string field, string type)
    {
        JSONNode enc = new JSONObject();
        enc["field"] = new JSONString(field);
        enc["type"] = new JSONString(type);
        return enc;
    }

    // -------------------------------------------------------------------------
    // Shared Utilities
    // -------------------------------------------------------------------------

    private static Vis FindVis(string view_id)
    {
        GameObject visObj = GameObject.Find(view_id);
        if (visObj == null)
        {
            Debug.LogWarning($"CreateSkill: GameObject '{view_id}' not found in the scene.");
            return null;
        }

        Vis vis = visObj.GetComponent<Vis>();
        if (vis == null)
            Debug.LogWarning($"CreateSkill: No Vis component found on '{view_id}'.");

        return vis;
    }
}