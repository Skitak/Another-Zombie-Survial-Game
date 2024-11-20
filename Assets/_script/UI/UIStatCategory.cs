using System.Collections.Generic;
using System.Linq;
using Asmos.Bus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIStatCategory : MonoBehaviour
{
    public StatCategory category;
    public Transform statLinesParent;
    public GameObject statLinePrefab;
    Dictionary<StatType, GameObject> statLines = new();

    void Awake()
    {
        for (int i = 0; i < statLinesParent.childCount; i++)
            Destroy(statLinesParent.GetChild(i).gameObject);
        foreach (KeyValuePair<StatType, StatDescription> kp in StatManager.Descriptions.Where(x => x.Value.category == category))
            statLines[kp.Key] = Instantiate(statLinePrefab, statLinesParent);
        Bus.Subscribe("Pause", (o) => { if ((bool)o[0]) RefreshStats(); });
    }

    public void RefreshStats()
    {
        foreach (KeyValuePair<StatType, StatDescription> kp in StatManager.Descriptions.Where(x => x.Value.category == category))
        {
            GameObject statLine = statLines[kp.Key];
            Stat stat = StatManager.GetStat(kp.Key);
            bool useContextData = true;

            float valueFlat = stat.GetFlatValue();
            float valuePercent = stat.GetPercentValue(useContextData);
            float total = stat.GetValue(useContextData);

            string valueFlatStr = stat.description.ValueToString(stat.GetFlatValue());
            string valuePercentStr = $"{valuePercent}%";
            string totalStr = stat.description.ValueToString(stat.GetValue(useContextData));
            string name = kp.Value.displayedName;

            // string flatStr = valuePercent != 0 ? $" + {valueDisplay(valuePercent, kp.Value)}" : "";
            // string perStr = perVal != 1 ? $" * {perVal * 100}%" : "";
            string calculusStr = "";
            if (stat.description.isPercent)
                calculusStr = $"{valuePercentStr}";
            else if (valuePercent == 0)
                calculusStr = $"{valueFlat}";
            else if (valuePercent > 0)
                calculusStr = $"{valueFlat} + {valuePercentStr}";
            else
                calculusStr = $"{valueFlat} / {valuePercentStr}";


            if (stat.modifiers.Any(x => x.modificationKind == ModificationKind.TOTAL_PERCENT))
                calculusStr += $" + <b>specials</b>";
            string label = $"{name} : {calculusStr} = {totalStr}";
            if (total == valueFlat || stat.description.isPercent && total == valuePercent)
                label = $"{name} : {totalStr}";
            statLine.GetComponentInChildren<TMP_Text>().text = label;
            statLine.GetComponentInChildren<Image>().sprite = kp.Value.icon;
        }

    }
}
