public class Highlight
{
    public static void Execute(string chart_id, string element_id, string highlight_type)
    {
        Debug.Log("Executing HighlightSkill...");

        Vis vis = FindVis(chart_id);
        if (vis == null) return;

        if (!TryParseIndex(element_id, vis.markInstances.Count, out int index)) return;

        JSONNode specs = vis.GetVisSpecs();
        if (specs == null)
        {
            Debug.LogWarning("Vis specs not found.");
            return;
        }

        string jsonFilePath = ResolveDataPath(specs);
        if (jsonFilePath == null) return;

        JSONNode jsonData = JSON.Parse(File.ReadAllText(jsonFilePath));
        JSONArray valuesArray = jsonData?["values"].AsArray;

        if (valuesArray == null || index >= valuesArray.Count)
        {
            Debug.LogWarning("Values array is null or index out of range.");
            return;
        }

        ApplyHighlight(vis, valuesArray, index, highlight_type.ToLower());

        jsonData["values"] = valuesArray;
        File.WriteAllText(jsonFilePath, jsonData.ToString());
        vis.UpdateVis();

        Debug.Log($"HighlightSkill completed. Element {element_id} highlighted with type: {highlight_type}");
    }

    // -------------------------------------------------------------------------
    // Highlight Application
    // -------------------------------------------------------------------------

    private const string HighlightField = "_highlight";

    private static void ApplyHighlight(Vis vis, JSONArray valuesArray, int index, string type)
    {
        switch (type)
        {
            case "color":
                SetHighlightFlags(vis, valuesArray, index);
                Mark colorMark = vis.markInstances[index].GetComponent<Mark>();
                if (colorMark != null) colorMark.SetChannelValue("color", "#ffff00");
                break;

            case "scale":
                SetHighlightFlags(vis, valuesArray, index);
                vis.markInstances[index].transform.localScale *= 1.5f;
                break;

            case "opacity":
                SetHighlightFlags(vis, valuesArray, index);
                for (int i = 0; i < vis.markInstances.Count; i++)
                {
                    Mark m = vis.markInstances[i].GetComponent<Mark>();
                    if (m != null) m.SetChannelValue("opacity", i == index ? "1.0" : "0.3");
                }
                break;

            case "none":
                ClearHighlight(vis, valuesArray);
                break;

            default:
                Debug.LogWarning($"Unknown highlight_type: {type}. Use: color, scale, opacity, none.");
                break;
        }
    }

    private static void SetHighlightFlags(Vis vis, JSONArray valuesArray, int index)
    {
        for (int i = 0; i < valuesArray.Count; i++)
            valuesArray[i][HighlightField] = (i == index) ? "true" : "false";

        for (int i = 0; i < vis.markInstances.Count; i++)
        {
            Mark m = vis.markInstances[i].GetComponent<Mark>();
            if (m != null) m.datum[HighlightField] = (i == index) ? "true" : "false";
        }
    }

    private static void ClearHighlight(Vis vis, JSONArray valuesArray)
    {
        for (int i = 0; i < valuesArray.Count; i++)
            valuesArray[i].Remove(HighlightField);

        for (int i = 0; i < vis.markInstances.Count; i++)
        {
            Mark m = vis.markInstances[i].GetComponent<Mark>();
            if (m == null) continue;

            m.SetChannelValue("opacity", "1.0");
            if (m.datum.ContainsKey(HighlightField)) m.datum.Remove(HighlightField);
            vis.markInstances[i].transform.localScale = Vector3.one;
        }
    }

    // -------------------------------------------------------------------------
    // Path Resolution
    // -------------------------------------------------------------------------

    private static string ResolveDataPath(JSONNode specs)
    {
        string url = specs["data"]["url"].Value;
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogWarning("Data URL not found in vis specs.");
            return null;
        }

        string path = Path.Combine(Application.streamingAssetsPath, url);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"JSON file not found: {path}");
            return null;
        }

        return path;
    }

    // -------------------------------------------------------------------------
    // Shared Utilities
    // -------------------------------------------------------------------------

    private static Vis FindVis(string chart_id)
    {
        GameObject visObj = GameObject.Find(chart_id);
        if (visObj == null)
        {
            Debug.LogWarning($"Chart not found: {chart_id}");
            return null;
        }

        Vis vis = visObj.GetComponent<Vis>();
        if (vis == null)
            Debug.LogWarning($"Vis component not found on: {chart_id}");

        return vis;
    }

    private static bool TryParseIndex(string element_id, int maxCount, out int index)
    {
        if (!int.TryParse(element_id, out index))
        {
            Debug.LogWarning($"Invalid element_id: {element_id}");
            return false;
        }
        if (index < 0 || index >= maxCount)
        {
            Debug.LogWarning($"element_id out of range: {element_id}");
            return false;
        }
        return true;
    }
}