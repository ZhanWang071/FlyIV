public class Update
{
    public static void Execute(string chart_id, string element_id, string y_value)
    {
        Debug.Log("Executing UpdateSkill...");

        Vis vis = FindVis(chart_id);
        if (vis == null) return;

        if (!int.TryParse(element_id, out int index))
        {
            Debug.LogWarning($"Invalid element_id: {element_id}");
            return;
        }

        JSONNode visSpecs = vis.GetVisSpecs();
        if (visSpecs == null)
        {
            Debug.LogWarning($"Vis specs is null for chart: {chart_id}");
            return;
        }

        if (!TryGetYEncoding(visSpecs, chart_id, out string yField, out string yType)) return;

        JSONNode dataValues = visSpecs["data"]["values"];
        if (dataValues == null)
        {
            Debug.LogWarning($"No inline data values found in vis spec for chart: {chart_id}");
            return;
        }

        if (index < 0 || index >= dataValues.Count)
        {
            Debug.LogWarning($"element_id {element_id} exceeds data values count: {dataValues.Count}");
            return;
        }

        if (!TrySetValue(dataValues[index], yField, yType, y_value)) return;

        vis.UpdateVisSpecsFromTextSpecs();

        Debug.Log($"UpdateSkill completed: chart={chart_id} mark={element_id} {yField}={y_value}");
    }

    // -------------------------------------------------------------------------
    // Scene Lookup
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

    // -------------------------------------------------------------------------
    // Encoding Lookup
    // -------------------------------------------------------------------------

    private static bool TryGetYEncoding(JSONNode visSpecs, string chart_id, out string yField, out string yType)
    {
        yField = null;
        yType = "quantitative";

        if (visSpecs["encoding"] == null ||
            visSpecs["encoding"]["y"] == null ||
            visSpecs["encoding"]["y"]["field"] == null)
        {
            Debug.LogWarning($"No y field mapping found in vis spec encoding for chart: {chart_id}");
            return false;
        }

        yField = visSpecs["encoding"]["y"]["field"].Value;
        yType = visSpecs["encoding"]["y"]["type"]?.Value ?? "quantitative";
        return true;
    }

    // -------------------------------------------------------------------------
    // Value Assignment
    // -------------------------------------------------------------------------

    private static bool TrySetValue(JSONNode entry, string yField, string yType, string y_value)
    {
        if (yType == "quantitative")
        {
            if (!double.TryParse(y_value, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsed))
            {
                Debug.LogWarning($"UpdateSkill: y_value '{y_value}' cannot be parsed as a number for quantitative field.");
                return false;
            }
            entry[yField] = new JSONNumber(parsed);
        }
        else
        {
            entry[yField] = new JSONString(y_value);
        }

        return true;
    }
}