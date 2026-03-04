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

        if (!TryGetEncoding(visSpecs, chart_id,
                out string xField, out string xType,
                out string yField, out string yType)) return;

        JSONNode dataValues = visSpecs["data"]["values"];
        if (dataValues == null)
        {
            Debug.LogWarning($"No data values found in vis spec for chart: {chart_id}");
            return;
        }

        JSONObject newEntry = BuildDefaultEntry(dataValues);

        if (!TrySetField(newEntry, xField, xType, x_value, "x_value")) return;
        if (!TrySetField(newEntry, yField, yType, y_value, "y_value")) return;

        dataValues.Add(newEntry);
        visSpecs["data"]["url"] = new JSONString("inline");

        vis.UpdateVis();

        Debug.Log($"AppendSingleSkill completed: chart={chart_id} x={x_value} y={y_value}");
    }

    // -------------------------------------------------------------------------
    // Encoding
    // -------------------------------------------------------------------------

    private static bool TryGetEncoding(
        JSONNode visSpecs, string chart_id,
        out string xField, out string xType,
        out string yField, out string yType)
    {
        xField = xType = yField = yType = null;

        JSONNode enc = visSpecs["encoding"];
        if (enc == null ||
            enc["x"] == null || enc["x"]["field"] == null ||
            enc["y"] == null || enc["y"]["field"] == null)
        {
            Debug.LogWarning($"Missing x or y field mapping in vis spec encoding for chart: {chart_id}");
            return false;
        }

        xField = enc["x"]["field"].Value;
        xType = enc["x"]["type"]?.Value ?? "nominal";
        yField = enc["y"]["field"].Value;
        yType = enc["y"]["type"]?.Value ?? "quantitative";
        return true;
    }

    // -------------------------------------------------------------------------
    // Entry Building
    // -------------------------------------------------------------------------

    private static JSONObject BuildDefaultEntry(JSONNode dataValues)
    {
        var entry = new JSONObject();
        if (dataValues.Count > 0)
            foreach (KeyValuePair<string, JSONNode> field in dataValues[0].AsObject)
                entry[field.Key] = new JSONString("0");

        return entry;
    }

    private static bool TrySetField(
        JSONObject entry, string field, string type, string value, string paramName)
    {
        if (type == "quantitative")
        {
            if (!double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsed))
            {
                Debug.LogWarning($"AppendSingleSkill: {paramName} '{value}' cannot be parsed as a number for quantitative field.");
                return false;
            }
            entry[field] = new JSONNumber(parsed);
        }
        else
        {
            entry[field] = new JSONString(value);
        }

        return true;
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
}