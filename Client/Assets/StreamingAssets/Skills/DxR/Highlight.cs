public class Highlight
{
    private const string HighlightField = "_highlight";

    public static void Execute(string chart_id, string element_id, string highlight_type)
    {
        Debug.Log("Executing HighlightSkill...");

        Vis vis = FindVis(chart_id);
        if (vis == null) return;

        if (!int.TryParse(element_id, out int index))
        {
            Debug.LogWarning($"Invalid element_id: {element_id}");
            return;
        }

        JSONNode visSpecs = vis.GetVisSpecs();
        JSONNode dataValues = GetDataValues(visSpecs, chart_id);
        if (dataValues == null) return;

        if (index < 0 || index >= dataValues.Count)
        {
            Debug.LogWarning($"element_id {element_id} out of range (total: {dataValues.Count})");
            return;
        }

        if (!ApplyHighlight(dataValues, index, highlight_type)) return;

        visSpecs["data"]["url"] = new JSONString("inline");
        vis.UpdateVis();

        Debug.Log($"HighlightSkill completed. Element {element_id} highlighted with type: {highlight_type}");
    }

    // -------------------------------------------------------------------------
    // Highlight Application
    // -------------------------------------------------------------------------

    private static bool ApplyHighlight(JSONNode dataValues, int index, string highlight_type)
    {
        switch (highlight_type.ToLower())
        {
            case "color":
            case "scale":
            case "opacity":
                for (int i = 0; i < dataValues.Count; i++)
                    dataValues[i][HighlightField] = new JSONString(i == index ? "true" : "false");
                return true;

            case "none":
                for (int i = 0; i < dataValues.Count; i++)
                    dataValues[i].Remove(HighlightField);
                return true;

            default:
                Debug.LogWarning($"Unknown highlight_type: {highlight_type}. Use: color, scale, opacity, none.");
                return false;
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

    private static JSONNode GetDataValues(JSONNode visSpecs, string chart_id)
    {
        if (visSpecs == null)
        {
            Debug.LogWarning("Vis specs not found.");
            return null;
        }

        JSONNode dataValues = visSpecs["data"]["values"];
        if (dataValues == null)
            Debug.LogWarning($"No data values found in vis spec for chart: {chart_id}");

        return dataValues;
    }
}