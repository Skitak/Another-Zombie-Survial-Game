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
        foreach (KeyValuePair<StatType, StatDescription> kp in StatManager.statDescriptions.Where(x => x.Value.category == category))
            statLines[kp.Key] = Instantiate(statLinePrefab, statLinesParent);
        Bus.Subscribe("Pause", (o) => { if ((bool)o[0]) RefreshStats(); });
    }

    public void RefreshStats()
    {
        foreach (KeyValuePair<StatType, StatDescription> kp in StatManager.statDescriptions.Where(x => x.Value.category == category))
        {
            GameObject statLine = statLines[kp.Key];
            Stat stat = StatManager.GetStat(kp.Key);
            bool useContextData = true;

            float baseVal = stat.GetBaseValue();
            float flatVal = stat.GetFlatValue(useContextData);
            float perVal = stat.GetPercentValue(useContextData);
            float total = stat.GetValue(useContextData);
            string name = kp.Value.displayedName;

            string valueDisplay(float value, StatDescription description) => description.displayType switch
            {
                StatDisplayType.PERCENT => $"{value}%",
                StatDisplayType.DEGREE => $"{value}°",
                _ => $"{value}",
            };
            string flatStr = flatVal != 0 ? $" + {valueDisplay(flatVal, kp.Value)}" : "";
            string perStr = perVal != 1 ? $" * {perVal * 100}%" : "";
            string baseStr = baseVal != total ? $": (base {valueDisplay(baseVal, kp.Value)}{flatStr}){perStr}" : "";
            string label = $"{name}{baseStr} = {valueDisplay(total, kp.Value)}";

            statLine.GetComponentInChildren<TMP_Text>().text = label;
            statLine.GetComponentInChildren<Image>().sprite = kp.Value.icon;
        }

    }
}
