public class AppendSeries
{
    public static void Execute(string chart_id, List<string> x_values, List<string> y_values, int serie_index)
    {
        Debug.Log("Executing AppendSeriesSkill...");

        Vis vis = FindVis(chart_id);
        if (vis == null) return;

        if (!ValidateLists(x_values, y_values)) return;

        JSONNode specs = vis.GetVisSpecs();
        if (specs == null)
        {
            Debug.LogWarning("Vis specs not found.");
            return;
        }

        string jsonFilePath = ResolveDataPath(specs);
        if (jsonFilePath == null) return;

        JSONNode jsonData = JSON.Parse(File.ReadAllText(jsonFilePath));
        if (jsonData == null)
        {
            Debug.LogWarning("Failed to parse JSON file.");
            return;
        }

        string xField = specs["encoding"]["x"]["field"].Value;
        string yField = specs["encoding"]["y"]["field"].Value;

        if (string.IsNullOrEmpty(xField) || string.IsNullOrEmpty(yField))
        {
            Debug.LogWarning("x or y field mapping not found in encoding.");
            return;
        }

        string serieField = specs["encoding"]["color"]?["field"]?.Value ?? "";

        JSONArray valuesArray = jsonData["values"].AsArray ?? new JSONArray();
        jsonData["values"] = valuesArray;

        string serieValue = ResolveSerieValue(valuesArray, serieField, serie_index);

        AppendPoints(valuesArray, x_values, y_values, xField, yField, serieField, serieValue);

        File.WriteAllText(jsonFilePath, jsonData.ToString());
        vis.UpdateVis();

        Debug.Log($"AppendSeriesSkill completed. Appended {x_values.Count} data points to serie {serie_index}.");
    }

    // -------------------------------------------------------------------------
    // Serie Resolution
    // -------------------------------------------------------------------------

    private static string ResolveSerieValue(JSONArray valuesArray, string serieField, int serie_index)
    {
        if (string.IsNullOrEmpty(serieField)) return serie_index.ToString();

        var seen = new HashSet<string>();
        var sorted = new List<string>();

        for (int i = 0; i < valuesArray.Count; i++)
        {
            string sv = valuesArray[i][serieField].Value;
            if (seen.Add(sv)) sorted.Add(sv);
        }

        sorted.Sort();

        return (serie_index >= 0 && serie_index < sorted.Count)
            ? sorted[serie_index]
            : serie_index.ToString();
    }

    private static void AppendPoints(
        JSONArray valuesArray,
        List<string> x_values,
        List<string> y_values,
        string xField,
        string yField,
        string serieField,
        string serieValue)
    {
        for (int i = 0; i < x_values.Count; i++)
        {
            JSONNode entry = new JSONObject();
            entry[xField] = x_values[i];
            entry[yField] = y_values[i];

            if (!string.IsNullOrEmpty(serieField))
                entry[serieField] = serieValue;

            valuesArray.Add(entry);
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

    private static bool ValidateLists(List<string> x_values, List<string> y_values)
    {
        if (x_values == null || y_values == null || x_values.Count != y_values.Count)
        {
            Debug.LogWarning("x_values and y_values must be non-null and have the same length.");
            return false;
        }
        return true;
    }
}