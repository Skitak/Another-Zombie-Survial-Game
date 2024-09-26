using System.Collections.Generic;
using System.Text.RegularExpressions;
using Asmos.Bus;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class BusReaderLabel : MonoBehaviour
{
    [InfoBox("To add a bus value, set your label normally and place brackets around your key")]
    public bool iUnderstood;
    public bool dontDisplayWithoutData = true;
    TMP_Text label;
    string textBaseValue;
    readonly Regex regex = new(@"\[(.*?)\]");
    Dictionary<string, string> keysToValues = new();
    void Awake()
    {
        label = GetComponent<TMP_Text>();
        textBaseValue = label.text;
        BindAllKeys();
        RefreshLabel();
    }

    void BindAllKeys()
    {
        MatchCollection matches = regex.Matches(textBaseValue);
        for (int i = 0; i < matches.Count; i++)
        {
            string key = matches[i].Groups[1].Value; // Extract the content inside the brackets
            keysToValues.Add(key, "");
            Bus.Subscribe(key, (o) =>
            {
                keysToValues[key] = o[0].ToString();
                RefreshLabel();
            });
        }
    }

    void RefreshLabel()
    {
        string newLabel = textBaseValue;
        bool hasEmptyString = false;
        foreach (string key in keysToValues.Keys)
        {
            newLabel = newLabel.Replace($"[{key}]", keysToValues[key]);
            if (keysToValues[key].IsNullOrWhitespace())
                hasEmptyString = true;
        }

        if (hasEmptyString && dontDisplayWithoutData)
            label.SetText("");
        else
            label.SetText(newLabel);
    }
}
