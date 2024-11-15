using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Asmos.Bus;
using Asmos.Timers;
using Asmos.UI;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

public class PerksManager : SerializedMonoBehaviour
{
    public static PerksManager instance;
    [SerializeField] ViewConfig perkView;
    [DictionaryDrawerSettings(DisplayMode = DictionaryDisplayOptions.ExpandedFoldout)]
    [SerializeField] Dictionary<Rarity, List<int>> baseRarityChances;
    [ReadOnly] Perk[] allPerks;
    int rarityChancesIndex = 0;
    [HideInInspector] public int[] rarityChances;
    Timer timeScaleTimer;
    Stack<Perk> perksApplied = new();
    // [SerializeField]
    int perksToPick = 0;
    [HideInInspector] public bool isOpened;
    void Awake()
    {
        instance = this;
        rarityChances = GetRarityChances(0);
        timeScaleTimer = new Timer(.5f);
        timeScaleTimer.OnTimerUpdate += () => Time.timeScale = timeScaleTimer.GetPercentageLeft();
        timeScaleTimer.useUpdateAsRewindAction = true;
        timeScaleTimer.useTimeScale = false;
        Bus.Subscribe("close perks", (o) => isOpened = false);
        Bus.Subscribe("refresh perks", (o) => RefreshPerks());
        Bus.Subscribe("improve drop", (o) => ImproveDropChances());
    }
    public async Task OpenPerksMenu(int perksAmout = 0)
    {
        isOpened = true;
        Player.player.SetInputEnabled(false);
        timeScaleTimer.Play();
        Cursor.lockState = CursorLockMode.Confined;
        perksToPick = perksAmout;
        await ViewManager.instance.AddView(perkView);
        RefreshPerks();
        if (perksToPick != 0)
            while (perksToPick > 0)
                await Task.Delay(100);
        else
            while (isOpened)
                await Task.Delay(100);
        await ViewManager.instance.RemoveView();
        Cursor.lockState = CursorLockMode.Locked;
        timeScaleTimer.Rewind();
        Player.player.SetInputEnabled(true);
        isOpened = false;
    }

    private Rarity PickRarity()
    {
        int randomValue = UnityEngine.Random.Range(0, 100);
        for (int i = 0; i < rarityChances.Length; i++)
        {
            randomValue -= rarityChances[i];
            if (randomValue <= 0f)
            {
                return (Rarity)i;
            }
        }
        return (Rarity)rarityChances.Length - 1;
    }
    int[] GetRarityChances(int chancesIndex)
    {
        int[] rarityChances = new int[4];
        for (int i = 0; i < 4; i++)
        {
            var list = baseRarityChances[(Rarity)i];
            rarityChances[i] = list[math.min(chancesIndex, list.Count - 1)];
        }
        return rarityChances;
    }
    public void RefreshPerks()
    {
        List<Perk> foundPerks = new();
        foreach (PerkCard card in PerkCard.perkCards)
        {
            var prioPerks = allPerks.Where(x => x.hasPrio);
            if (prioPerks.Count() != 0)
            {
                Perk prioPerk = prioPerks.ElementAt(UnityEngine.Random.Range(0, prioPerks.Count()));
                card.InitializePerk(prioPerk, prioPerk.rarity);
                continue;
            }
            Rarity rarity = PickRarity();
            IEnumerable<Perk> perks;
            do
            {
                perks = allPerks.Where((Perk perk) =>
                    perk.CanBeApplied() &&
                    perk.rarity == rarity &&
                    !foundPerks.Contains(perk) &&
                    perk.showInShop
                );
                rarity = (Rarity)Math.Max(0, ((int)rarity) - 1);
            } while (perks.Count() == 0);
            Perk randomPerk = perks.ElementAt(UnityEngine.Random.Range(0, perks.Count()));
            foundPerks.Add(randomPerk);
            card.InitializePerk(randomPerk, rarity);
        }
    }
    public void AddPerk(Perk perk)
    {
        perk.ApplyUpgrades();
        perksApplied.Push(perk);
        if (perksToPick > 0)
            --perksToPick;
    }
    void ImproveDropChances()
    {
        rarityChances = GetRarityChances(++rarityChancesIndex);
        Bus.PushData("drop updated");
    }
#if UNITY_EDITOR
    void OnValidate()
    {
        allPerks = Asmos.UI.Utils.GetAllInstances<Perk>();
    }
#endif
}

