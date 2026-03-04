public class AppendSingle
{
    public static void Execute(string chart_id, string x_value, string y_value)
    {
        Debug.Log("Executing AppendSingleSkill...");

        Vis vis = FindVis(chart_id);
        if (vis == null) return;

        JSONNode visSpecs = vis.GetVisSpecs();
        if (visSpecs == null)
        {
            Debug.LogWarning($"Vis specs is null for chart: {chart_id}");
            return;
        }

        string xField = GetEncodingField(visSpecs, "x");
        string yField = GetEncodingField(visSpecs, "y");

        if (xField == null || yField == null)
        {
            Debug.LogWarning($"Missing x or y field mapping in vis spec encoding for chart: {chart_id}");
            return;
        }

        // --- Build new datum ---
        var newDatum = BuildDatum(vis, xField, yField, x_value, y_value);

        // --- Update runtime data ---
        vis.data.values.Add(newDatum);

        AppendToSpecValues(visSpecs, vis, newDatum);
        AppendMark(vis, newDatum, x_value, y_value);
        PersistAppend(vis, visSpecs, newDatum);

        Debug.Log($"AppendSingleSkill completed: chart={chart_id} x={x_value} y={y_value}");
    }

    // -------------------------------------------------------------------------
    // Data Building
    // -------------------------------------------------------------------------

    private static Dictionary<string, string> BuildDatum(
        Vis vis, string xField, string yField, string x_value, string y_value)
    {
        var datum = new Dictionary<string, string>();
        foreach (string field in vis.data.fieldNames)
            datum[field] = "0";

        datum[xField] = x_value;
        datum[yField] = y_value;
        return datum;
    }

    private static void AppendToSpecValues(
        JSONNode visSpecs, Vis vis, Dictionary<string, string> newDatum)
    {
        JSONNode dataValues = visSpecs["data"]["values"];
        if (dataValues == null) return;

        JSONObject newEntry = new JSONObject();
        foreach (string field in vis.data.fieldNames)
            newEntry[field] = new JSONString(newDatum[field]);

        dataValues.Add(newEntry);
    }

    private static void AppendMark(Vis vis, Dictionary<string, string> newDatum, string x_value, string y_value)
    {
        if (vis.markInstances.Count == 0) return;

        GameObject template = vis.markInstances[vis.markInstances.Count - 1];
        GameObject newMark = GameObject.Instantiate(template, template.transform.parent);
        Mark mark = newMark.GetComponent<Mark>();

        if (mark != null)
        {
            mark.datum = newDatum;
            mark.SetChannelValue("x", x_value);
            mark.SetChannelValue("y", y_value);
        }

        vis.markInstances.Add(newMark);
    }

    // -------------------------------------------------------------------------
    // Persistence
    // -------------------------------------------------------------------------

    private static void PersistAppend(Vis vis, JSONNode visSpecs, Dictionary<string, string> newDatum)
    {
        if (visSpecs["data"]["url"] != null && visSpecs["data"]["url"].Value != "inline")
        {
            string dataFilePath = Parser.GetFullDataPath(visSpecs["data"]["url"].Value);
            if (!File.Exists(dataFilePath)) return;

            JSONNode dataFileJson = JSON.Parse(File.ReadAllText(dataFilePath));
            if (dataFileJson == null) return;

            JSONObject appendEntry = new JSONObject();
            foreach (string field in vis.data.fieldNames)
                appendEntry[field] = new JSONString(newDatum[field]);

            dataFileJson.Add(appendEntry);
            File.WriteAllText(dataFilePath, dataFileJson.ToString(2));
        }
        else
        {
            string specFilePath = Parser.GetFullSpecsPath(vis.visSpecsURL);
            File.WriteAllText(specFilePath, JSON.Parse(visSpecs.ToString()).ToString(2));
        }
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

    private static string GetEncodingField(JSONNode visSpecs, string axis)
    {
        return visSpecs["encoding"]?[axis]?["field"]?.Value;
    }
}