using System;
using Asmos.Bus;
using UnityEngine;
using UnityEngine.Pool;

public class WaveManager : MonoBehaviour
{
    public static WaveManager instance;
    [SerializeField] float waveCooldown;
    [SerializeField] int maxZombies;
    [SerializeField] GameObject zombiePrefab;
    [SerializeField] Waves[] waves;
    Waves currentWave;
    Timer waveCooldownTimer, spawnTimer;
    int waveCount, zombieCount;
    void Start()
    {
        instance = this;
        waveCooldownTimer = new(waveCooldown, StartNewWave);
        spawnTimer = new(100f, SpawnZombie);
        StartGame();
    }

    #region Gameloop

    public void StartGame()
    {
        waveCount = 0;
        waveCooldownTimer.ResetPlay();
        Bus.PushData("wave", 1);
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
    void StartNewWave()
    {
        if (waves.Length <= waveCount)
            currentWave = waves[^1];
        else
            currentWave = waves[waveCount];
        ++waveCount;
        zombieCount = 0;
        spawnTimer.EndTime = currentWave.spawnTime;
        spawnTimer.ResetPlay();
        Bus.PushData("wave", waveCount);
    }

    void EndWave()
    {
        // TODO : Choose Perks
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

[Serializable]
public struct ZombieParameters
{
    public int health;
    public float speed;
    public bool walk;
}

#endregion