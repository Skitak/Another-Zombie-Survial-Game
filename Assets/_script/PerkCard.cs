using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PerkCard : MonoBehaviour
{
    public static List<PerkCard> perkCards = new();
    [SerializeField] Image image;
    [SerializeField] TMP_Text label;
    [SerializeField] GameObject crown;
    [SerializeField] GameObject hat;
    [SerializeField] GameObject beret;
    Perk perk;
    Rarity rarity;
    void Start()
    {
        perkCards.Add(this);
    }
    public void InitializePerk(Perk perk, Rarity rarity)
    {
        this.perk = perk;
        this.rarity = rarity;
        image.sprite = perk.sprite;
        label.SetText(perk.GetLabel(rarity));
        ShowRarity();
    }

    void ShowRarity()
    {
        crown.SetActive(rarity == Rarity.LEGENDARY);
        hat.SetActive(rarity == Rarity.RARE);
        beret.SetActive(rarity == Rarity.UNCOMMON);
    }

    public void PerkSelected()
    {
        PerksManager.instance.PerkChosen(perk, rarity);
    }
}
