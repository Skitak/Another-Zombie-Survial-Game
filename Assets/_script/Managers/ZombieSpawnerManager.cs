using System;
using System.Collections.Generic;
using System.Linq;
using Asmos.Timers;
using UnityEngine;
using UnityEngine.Pool;

public class ZombieSpawnerManager : MonoBehaviour
{
    [SerializeField] float timeBeforeDissapear;
    public static ZombieSpawnerManager instance;
    public List<ZombieSpawner> spawners;
    ObjectPool<Zombie> pool;
    GameObject currentZombiePrefab;
    List<Zombie> zombiesAlive = new();

    void Awake()
    {
        instance = this;
        pool = new(CreateZombie, GetZombie, ReleaseZombie, DestroyZombie, true, 20, 100);
    }
    public void Spawn(GameObject zombiePrefab, ZombieParameters parameters, GameObject pickup)
    {
        currentZombiePrefab = zombiePrefab;
        Zombie zombie = pool.Get();
        zombiesAlive.Add(zombie);
        ZombieSpawner spawnerSelected = SelectZombieSpawner();
        zombie.Spawn(spawnerSelected.transform.position, parameters, pickup, spawnerSelected.spawnKind);
    }

    public void ZombieDied(Zombie zombie)
    {
        zombiesAlive.Remove(zombie);
        if (zombiesAlive.Count == 0)
            WaveManager.instance.NoZombiesLeft();
        Timer.OneShotTimer(timeBeforeDissapear, () => pool.Release(zombie));
    }

    public void RestartGame()
    {
        foreach (Zombie zombie in zombiesAlive)
        {
            zombie.animator.SetTrigger("end bite");
            pool.Release(zombie);
        }
        zombiesAlive.Clear();
    }

    #region pool
    Zombie CreateZombie()
    {
        GameObject newZombie = Instantiate(currentZombiePrefab);
        return newZombie.GetComponent<Zombie>();
    }
    void GetZombie(Zombie zombie)
    {
        zombie.gameObject.SetActive(true);
    }
    void ReleaseZombie(Zombie zombie)
    {
        zombie.gameObject.SetActive(false);
    }
    void DestroyZombie(Zombie zombie)
    {
        Destroy(zombie.gameObject);
    }
    #endregion

    ZombieSpawner SelectZombieSpawner()
    {
        return spawners[UnityEngine.Random.Range(0, spawners.Count - 1)];
    }

    void OnDrawGizmosSelected()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.blue;
        var localSpawners = GameObject.FindGameObjectsWithTag("Zombie spawner");
        foreach (GameObject spawner in localSpawners)
        {
            Gizmos.DrawSphere(spawner.transform.position, 1f);
        }
    }
}
