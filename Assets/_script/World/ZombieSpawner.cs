using Asmos.Bus;
using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    public SpawnKind spawnKind;
    public int waveActive = 1;
    void Start()
    {
        Bus.Subscribe("WAVE", (o) =>
        {
            int wave = (int)o[0];
            if (wave < waveActive && ZombieSpawnerManager.instance.spawners.Contains(this))
                ZombieSpawnerManager.instance.spawners.Remove(this);
            if (wave >= waveActive && !ZombieSpawnerManager.instance.spawners.Contains(this))
                ZombieSpawnerManager.instance.spawners.Add(this);
        });
    }
}

public enum SpawnKind
{
    INSTANT, GROUND
}