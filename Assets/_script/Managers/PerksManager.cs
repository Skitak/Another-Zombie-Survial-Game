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
using UnityEngine.InputSystem;

public class PerksManager : SerializedMonoBehaviour
{
    public static PerksManager instance;
    InputAction openPerkAction;
    [SerializeField] ViewConfig perkView;
    [DictionaryDrawerSettings(KeyLabel = "Rarity", ValueLabel = "Chances")]
    [SerializeField] Dictionary<Rarity, List<int>> baseRarityChances;
    [ReadOnly] Perk[] allPerks;
    int rarityChancesIndex = 0;
    [HideInInspector] public int[] rarityChances;
    Timer timeScaleTimer;
    [HideInInspector, Unity.VisualScripting.DoNotSerialize] public List<Perk> appliedPerks = new();
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
        openPerkAction = InputSystem.actions.FindAction("Perk");
        foreach (var perk in allPerks)
            perk.InitializeCondition();

    }

    void Update()
    {
        if (openPerkAction.WasPressedThisFrame())
            OpenPerksMenu();
    }
    public async Task OpenPerksMenu(int perksAmout = 0)
    {
        isOpened = true;
        Player.player.EnableInput(false);
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
        Player.player.EnableInput(true);
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
        int i = 0;
        foreach (Rarity irarity in Enum.GetValues(typeof(Rarity)))
        {
            if (irarity == Rarity.ALL)
                continue;
            var list = baseRarityChances[irarity];
            rarityChances[i] = list[math.min(chancesIndex, list.Count - 1)];
            i++;
        }
        return rarityChances;
    }
    public void RefreshPerks()
    {
        List<Perk> foundPerks = new();
        foreach (PerkCard card in PerkCard.perkCards)
        {
            // Picking perks with prio
            var prioPerks = allPerks.Where(x => x.hasPrio && (x.ignoreConditionWithPrio || x.IsValid()));
            if (prioPerks.Count() != 0)
            {
                Perk prioPerk = prioPerks.ElementAt(UnityEngine.Random.Range(0, prioPerks.Count()));
                card.InitializePerk(prioPerk, prioPerk.rarity);
                continue;
            }

            // Picking perk normally
            Debug.Log("Picking perk");
            Rarity rarity = PickRarity();
            IEnumerable<Perk> perks;
            do
            {
                perks = allPerks.Where((Perk perk) =>
                    perk.CanBeApplied() &&
                    rarity.HasFlag(perk.rarity) &&
                    !foundPerks.Contains(perk)
                );
                rarity = (Rarity)Math.Max(1, ((int)rarity) >> 1);
            } while (perks.Count() == 0);
            Perk randomPerk = perks.ElementAt(UnityEngine.Random.Range(0, perks.Count()));
            foundPerks.Add(randomPerk);
            card.InitializePerk(randomPerk, rarity);
            Debug.Log("Done");
        }
    }
    public void AddPerk(Perk perk)
    {
        perk.ApplyModifiers();
        appliedPerks.Add(perk);
        if (perksToPick > 0)
            --perksToPick;
        Bus.PushData("Perk");
    }
    void ImproveDropChances()
    {
        rarityChances = GetRarityChances(++rarityChancesIndex);
        Bus.PushData("drop updated");
    }

    public int GetRarityChance(Rarity rarity) => rarityChances[rarity switch
    {
        Rarity.COMMON => 0,
        Rarity.UNCOMMON => 1,
        Rarity.RARE => 2,
        Rarity.LEGENDARY => 3,
        Rarity.ALL => 3,
        _ => 0,
    }];
#if UNITY_EDITOR
    void OnValidate()
    {
        allPerks = Asmos.UI.Utils.GetAllInstances<Perk>();
    }
#endif
}

