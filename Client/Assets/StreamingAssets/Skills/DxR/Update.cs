public class Update
{
    public static void Execute(string chart_id, string element_id, string y_value)
    {
        Debug.Log("Executing UpdateSkill...");

        Vis vis = FindVis(chart_id);
        if (vis == null) return;

        if (!TryParseIndex(element_id, vis.markInstances.Count, out int index)) return;

        JSONNode visSpecs = vis.GetVisSpecs();
        if (visSpecs == null)
        {
            Debug.LogWarning($"Vis specs is null for chart: {chart_id}");
            return;
        }

        string yField = GetEncodingField(visSpecs, "y");
        if (yField == null)
        {
            Debug.LogWarning($"No y field mapping found in vis spec encoding for chart: {chart_id}");
            return;
        }

        JSONNode dataValues = visSpecs["data"]["values"];
        if (dataValues == null)
        {
            Debug.LogWarning($"No data values found in vis spec for chart: {chart_id}");
            return;
        }
        if (index >= dataValues.Count)
        {
            Debug.LogWarning($"element_id {element_id} exceeds data values count: {dataValues.Count}");
            return;
        }

        // --- Update in-memory spec ---
        dataValues[index][yField] = new JSONString(y_value);

        // --- Persist to file ---
        PersistUpdate(vis, visSpecs, index, yField, y_value);

        // --- Update live mark ---
        Mark mark = vis.markInstances[index].GetComponent<Mark>();
        if (mark != null)
        {
            mark.datum[yField] = y_value;
            mark.SetChannelValue("y", y_value);
        }

        if (index < vis.data.values.Count)
            vis.data.values[index][yField] = y_value;

        Debug.Log($"UpdateSkill completed: chart={chart_id} mark={element_id} {yField}={y_value}");
    }

    // -------------------------------------------------------------------------
    // Persistence
    // -------------------------------------------------------------------------

    private static void PersistUpdate(Vis vis, JSONNode visSpecs, int index, string yField, string y_value)
    {
        if (visSpecs["data"]["url"] != null && visSpecs["data"]["url"].Value != "inline")
        {
            string dataFilePath = Parser.GetFullDataPath(visSpecs["data"]["url"].Value);
            if (!File.Exists(dataFilePath)) return;

            JSONNode dataFileJson = JSON.Parse(File.ReadAllText(dataFilePath));
            if (dataFileJson != null && index < dataFileJson.Count)
            {
                dataFileJson[index][yField] = new JSONString(y_value);
                File.WriteAllText(dataFilePath, dataFileJson.ToString(2));
            }
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

    private static bool TryParseIndex(string element_id, int maxCount, out int index)
    {
        if (!int.TryParse(element_id, out index))
        {
            Debug.LogWarning($"Invalid element_id: {element_id}");
            return false;
        }
        if (index < 0 || index >= maxCount)
        {
            Debug.LogWarning($"element_id out of range: {element_id} (total marks: {maxCount})");
            return false;
        }
        return true;
    }

    private static string GetEncodingField(JSONNode visSpecs, string axis)
    {
        return visSpecs["encoding"]?[axis]?["field"]?.Value;
    }
}