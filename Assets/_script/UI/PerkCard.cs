using System.Collections.Generic;
using System.Threading.Tasks;
using Asmos.Bus;
using Asmos.UI;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PerkCard : MonoBehaviour
{
    public static List<PerkCard> perkCards = new();
    [SerializeField] Image image;
    [SerializeField] TMP_Text description;
    [SerializeField] TMP_Text title;
    [SerializeField] TMP_Text price;
    [SerializeField] GameObject crown;
    [SerializeField] GameObject hat;
    [SerializeField] GameObject beret;
    [SerializeField] GameObject stars;
    [SerializeField] GameObject tooExpensive;
    [SerializeField] Animator lockAnimator;
    [SerializeField] DOTweenAnimation[] purchaseAnimations;
    [SerializeField] DOTweenAnimation[] cantPurchaseAnimations;
    [SerializeField] DOTweenAnimation[] showTooExpensiveAnimations;
    [SerializeField] SliceEEDotween animController;
    Perk perk;
    Rarity rarity;
    [HideInInspector] public bool locked = false;
    [HideInInspector] public bool purchased = false;
    bool isTooExpensive = false;
    public Button button;
    void Awake()
    {
        perkCards.Add(this);
        Bus.Subscribe("MONEY", o => ToggleTooExpensive());
    }
    public async void InitializePerk(Perk perk, Rarity rarity)
    {
        if (purchased)
        {
            if (locked)
                ToggleLock();
            purchased = false;
        }
        if (locked)
            return;

        this.perk = perk;
        this.rarity = rarity;
        await RefreshVisuals();
        ToggleTooExpensive();
    }

    async Task RefreshVisuals()
    {
        image.sprite = perk.GetSprite();
        description.SetText(perk.GetLabel(false));
        title.SetText(perk.title);
        price.SetText($"{perk.price}$");
        ShowRarity();
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)title.transform.parent.parent);
        await animController.Show();
    }

    void ToggleTooExpensive()
    {
        if (perk == null || purchased)
            return;
        bool hasChanged = perk.price > MoneyManager.instance.GetMoney() != isTooExpensive;
        isTooExpensive = perk.price > MoneyManager.instance.GetMoney();
        if (hasChanged && isTooExpensive)
            foreach (DOTweenAnimation anim in showTooExpensiveAnimations)
                anim.tween.Restart();
        if (hasChanged && !isTooExpensive)
            foreach (DOTweenAnimation anim in showTooExpensiveAnimations)
                anim.tween.Rewind();
    }

    void ShowRarity()
    {
        crown.SetActive(rarity.HasFlag(Rarity.LEGENDARY));
        hat.SetActive(rarity.HasFlag(Rarity.RARE));
        beret.SetActive(rarity.HasFlag(Rarity.UNCOMMON));
        int rarity_value = (int)rarity;
        stars.transform.GetChild(0).gameObject.SetActive(rarity_value > 1);
        stars.transform.GetChild(1).gameObject.SetActive(rarity_value > 2);
        stars.transform.GetChild(2).gameObject.SetActive(rarity_value > 4);
    }

    public async void BuyPerk()
    {
        if (isTooExpensive)
        {
            foreach (DOTweenAnimation anim in cantPurchaseAnimations)
                anim.tween.Restart();
            return;
        }
        if (purchased)
            return;

        purchased = true;
        List<Task> tasks = new();
        foreach (DOTweenAnimation anim in purchaseAnimations)
        {
            anim.tween.Restart();
            tasks.Add(anim.tween.AsyncWaitForCompletion());
        }
        MoneyManager.instance.UpdateMoney(-perk.price, IncomeType.PERK);
        PerksManager.instance.AddPerk(perk);
        await Task.WhenAll(tasks);
        animController.Hide();
    }

    public void Show() { if (locked) animController.Show(); }


    public void HoverLock(bool hover)
    {
        lockAnimator.SetBool("hover", hover);
    }

    public void ToggleLock()
    {
        locked = !locked;
        lockAnimator.SetBool("open", !locked);
    }
}
