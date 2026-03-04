public class Update
{
    public static void Execute(string chart_id, string element_id, string y_value)
    {
        Debug.Log("Executing UpdateSkill...");

        GameObject visObj = GameObject.Find(chart_id);
        if (visObj == null)
        {
            Debug.LogWarning("Chart not found: " + chart_id);
            return;
        }

        Vis vis = visObj.GetComponent<Vis>();
        if (vis == null)
        {
            Debug.LogWarning("Vis component not found on: " + chart_id);
            return;
        }

        int index;
        if (!int.TryParse(element_id, out index))
        {
            Debug.LogWarning("Invalid element_id: " + element_id);
            return;
        }

        JSONNode visSpecs = vis.GetVisSpecs();
        if (visSpecs == null)
        {
            Debug.LogWarning("Vis specs is null for chart: " + chart_id);
            return;
        }

        if (visSpecs["encoding"] == null || visSpecs["encoding"]["y"] == null || visSpecs["encoding"]["y"]["field"] == null)
        {
            Debug.LogWarning("No y field mapping found in vis spec encoding for chart: " + chart_id);
            return;
        }

        string yField = visSpecs["encoding"]["y"]["field"].Value;
        string yType = (visSpecs["encoding"]["y"]["type"] != null) ? visSpecs["encoding"]["y"]["type"].Value : "quantitative";

        JSONNode dataValues = visSpecs["data"]["values"];
        if (dataValues == null)
        {
            Debug.LogWarning("No inline data values found in vis spec for chart: " + chart_id);
            return;
        }

        if (index < 0 || index >= dataValues.Count)
        {
            Debug.LogWarning("element_id " + element_id + " exceeds data values count: " + dataValues.Count);
            return;
        }

        if (yType == "quantitative")
        {
            double parsedDouble;
            if (double.TryParse(y_value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out parsedDouble))
            {
                dataValues[index][yField] = new JSONNumber(parsedDouble);
            }
            else
            {
                Debug.LogWarning("UpdateSkill: y_value '" + y_value + "' cannot be parsed as a number for quantitative field.");
                return;
            }
        }
        else
        {
            dataValues[index][yField] = new JSONString(y_value);
        }

        visSpecs["data"]["url"] = new JSONString("inline");

        vis.UpdateVis();

        Debug.Log("UpdateSkill completed: chart=" + chart_id + " mark=" + element_id + " " + yField + "=" + y_value);
    }
}