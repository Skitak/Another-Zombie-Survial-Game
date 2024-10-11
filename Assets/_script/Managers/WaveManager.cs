using System;
using System.Collections.Generic;
using Asmos.Bus;
using Sirenix.OdinInspector;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager instance;
    [SerializeField] float waveCooldown;
    [SerializeField] int maxZombies;
    [SerializeField][Min(1)] int startingWave;
    [SerializeField] GameObject zombiePrefab;
    [ListDrawerSettings(ShowIndexLabels = true)]
    [SerializeField] Waves[] waves;
    [TableList][SerializeField] PickupChances[] pickups;
    Waves currentWave;
    Timer waveCooldownTimer, spawnTimer;
    int waveCount, zombieCount;
    void Awake()
    {
        instance = this;
        waveCooldownTimer = new(waveCooldown, StartNewWave);
        spawnTimer = new(100f, SpawnZombie);
    }

    #region Gameloop

    public async void StartGame()
    {
        waveCount = startingWave - 1;
        if (waveCount > 0)
            await PerksManager.instance.OpenPerksMenu(waveCount);
        waveCooldownTimer.ResetPlay();
        Bus.PushData("wave", waveCount);
    }

    public void EndGame()
    {
        spawnTimer.Pause();
    }

    public void RestartGame()
    {
        StartGame();
    }

    #endregion

    #region Waveloop
    public void StartNewWave()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Player.player.SetInputEnabled(true);
        if (waves.Length <= waveCount)
            currentWave = waves[^1];
        else
            currentWave = waves[waveCount];
        ++waveCount;
        zombieCount = 0;
        spawnTimer.EndTime = currentWave.spawnTime;
        spawnTimer.ResetPlay();
        Bus.PushData("wave start", waveCount);
    }

    async void EndWave()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Player.player.SetInputEnabled(false);
        await PerksManager.instance.OpenPerksMenu();
        waveCooldownTimer.ResetPlay();
    }

    void SpawnZombie()
    {
        GameObject pickup = FindPickup();
        ZombieSpawnerManager.instance.Spawn(zombiePrefab, currentWave.zombieParameters, pickup);
        if (++zombieCount < currentWave.zombiesAmount)
            spawnTimer.ResetPlay();
    }
    public void NoZombiesLeft()
    {
        if (zombieCount == currentWave.zombiesAmount)
            EndWave();
    }

    #endregion

    # region utils
    GameObject FindPickup()
    {
        foreach (PickupChances pickupChances in pickups)
        {
            int maxRandom = (int)(currentWave.zombiesAmount / pickupChances.dropRate);
            if (UnityEngine.Random.Range(0, maxRandom) == 0)
                return pickupChances.pickup;
        }
        return null;
    }
    public int GetWaveCount() => waveCount;
    #endregion
}


#region structs

[Serializable]
public struct Waves
{
    public int zombiesAmount;
    public float spawnTime;
    public ZombieParameters zombieParameters;

    public Waves(int zombiesAmount, float spawnTime, ZombieParameters zombieParameters) : this()
    {
        this.zombiesAmount = zombiesAmount;
        this.spawnTime = spawnTime;
        if (zombieParameters.moveKinds == null || zombieParameters.moveKinds.Count == 0)
            zombieParameters.moveKinds = new List<ZombieMovesKindChances>(){
                new(ZombieMovesKind.WALK, 1),
                new(ZombieMovesKind.MEDIUM, 0),
                new(ZombieMovesKind.RUN, 0),
                new(ZombieMovesKind.SPRINT, 0),
            };
        this.zombieParameters = zombieParameters;
    }
}

#endregion