using System;
using Asmos.Bus;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager instance;
    [SerializeField] float waveCooldown;
    [SerializeField] int maxZombies;
    [SerializeField][Min(1)] int startingWave;
    [SerializeField] GameObject zombiePrefab;
    [SerializeField] Waves[] waves;
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
            await PerksManager.instance.OpenPerks(waveCount);
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
        await PerksManager.instance.OpenPerks();
        StartNewWave();
    }

    void SpawnZombie()
    {
        ZombieSpawnerManager.instance.Spawn(zombiePrefab, currentWave.distance, currentWave.zombieParameters);
        if (++zombieCount < currentWave.zombiesAmount)
            spawnTimer.ResetPlay();
    }

    public void NoZombiesLeft()
    {
        if (zombieCount == currentWave.zombiesAmount)
            EndWave();
    }

    #endregion
}


#region structs

[Serializable]
public struct Waves
{
    public int zombiesAmount;
    public float spawnTime;
    // public float totalTime;
    public ZombieSpawnerManager.SpawnDistance distance;
    public ZombieParameters zombieParameters;
}

#endregion