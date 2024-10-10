using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Asmos.UI;
using Sirenix.OdinInspector;
using UnityEngine;

public class PerksManager : MonoBehaviour
{
    public static PerksManager instance;
    [SerializeField] ViewConfig perkView;
    [SerializeField]
    [RequiredListLength(4)]
    [InfoBox("This array represents chances of rarity perks, it MUST have as much elements as there are rarities and it's total SHOULD be of 1.")]
    float[] baseRarityChances = new float[] { .5f, .4f, .1f, 0f };
    float[] rarityChances;
    Timer timeScaleTimer;
    Stack<PerkApplied> perksApplied = new();
    [SerializeField]
    [ReadOnly]
    Perk[] allPerks;
    int perksToPick = 0;
    void Awake()
    {
        instance = this;
        rarityChances = baseRarityChances;
        timeScaleTimer = new Timer(.5f);
        timeScaleTimer.OnTimerUpdate += () => Time.timeScale = timeScaleTimer.GetPercentageLeft();
        timeScaleTimer.useUpdateAsRewindAction = true;
        timeScaleTimer.useTimeScale = false;
    }

    public async Task OpenPerksMenu(int perksAmout = 1)
    {
        Player.player.SetInputEnabled(false);
        timeScaleTimer.Play();
        Cursor.lockState = CursorLockMode.Confined;
        perksToPick = perksAmout;
        RefreshPerks();
        await ViewManager.instance.AddView(perkView);
        while (perksToPick > 0)
            await Task.Delay(100);
        await ViewManager.instance.RemoveView();
        Cursor.lockState = CursorLockMode.Locked;
        timeScaleTimer.Rewind();
        // Time.timeScale = 1;
        Player.player.SetInputEnabled(true);
    }

    private int PickRarity()
    {
        float randomValue = UnityEngine.Random.Range(0f, 1f);
        for (int i = 0; i < rarityChances.Length; i++)
        {
            randomValue -= baseRarityChances[i];
            if (randomValue <= 0f)
            {
                return i;
            }
        }
        return rarityChances.Length - 1;
    }

    public void RefreshPerks()
    {
        List<Perk> foundPerks = new();
        foreach (PerkCard card in PerkCard.perkCards)
        {
            int rarity = PickRarity();
            var perks = allPerks.Where((Perk perk) =>
                (int)perk.rarityMin <= rarity &&
                (int)perk.rarityMax >= rarity &&
                !perk.dontShowAsUpgrade &&
                !foundPerks.Contains(perk) &&
                perk.timesPerkCanBeApplied > allPerks.Count((Perk _perk) => perk == _perk)
            );
            Perk randomPerk = perks.ElementAt(Random.Range(0, perks.Count()));
            foundPerks.Add(randomPerk);
            card.InitializePerk(randomPerk, (Rarity)rarity);
        }
    }

    public void PerkChosenFromMenu(Perk perk, Rarity rarity)
    {
        foreach (PerkCard card in PerkCard.perkCards)
            card.button.enabled = false;
        AddPerk(perk, rarity);
        if (--perksToPick > 0)
            RefreshPerks();
    }
    public void AddPerk(Perk perk, Rarity rarity)
    {
        perk.ApplyUpgrade(rarity);
        perksApplied.Push(new PerkApplied(rarity, perk));
    }
    struct PerkApplied
    {
        public Rarity rarity;
        public Perk perk;

        public PerkApplied(Rarity rarity, Perk perk)
        {
            this.rarity = rarity;
            this.perk = perk;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        allPerks = Utils.GetAllInstances<Perk>();
    }
#endif
}

