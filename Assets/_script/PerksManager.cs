using System.Collections.Generic;
using System.Linq;
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
    Stack<PerkApplied> perksApplied = new();
    [SerializeField]
    [ReadOnly]
    Perk[] allPerks;
    void Start()
    {
        instance = this;
        rarityChances = baseRarityChances;
    }

    public void OpenPerks()
    {
        foreach (PerkCard card in PerkCard.perkCards)
        {
            int rarity = PickRarity();
            var perks = allPerks.Where((Perk perk) =>
                (int)perk.rarityMin <= rarity &&
                (int)perk.rarityMax >= rarity &&
                perk.timesPerkCanBeApplied > allPerks.Count((Perk _perk) => perk == _perk)
            );
            Perk randomPerk = perks.ElementAt(Random.Range(0, perks.Count()));
            card.InitializePerk(randomPerk, (Rarity)rarity);
        }
        ViewManager.instance.AddView(perkView);
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

    public async void PerkChosen(Perk perk, Rarity rarity)
    {
        perk.ApplyUpgrade(rarity);
        perksApplied.Push(new PerkApplied(rarity, perk));
        await ViewManager.instance.RemoveView();
        WaveManager.instance.StartNewWave();

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

