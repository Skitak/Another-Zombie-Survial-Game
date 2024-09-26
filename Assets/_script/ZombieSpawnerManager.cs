using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ZombieSpawnerManager : MonoBehaviour
{
    [SerializeField] float timeBeforeDissapear;
    public static ZombieSpawnerManager instance;
    GameObject[] spawners;
    ObjectPool<Zombie> pool;
    GameObject currentZombiePrefab;
    List<Zombie> zombiesAlive = new();

    public enum SpawnDistance
    {
        CLOSE, NORMAL, FAR
    }
    void Awake()
    {
        instance = this;
        spawners = GameObject.FindGameObjectsWithTag("Zombie spawner");
        pool = new(CreateZombie, GetZombie, ReleaseZombie, DestroyZombie, true, 20, 100);
    }
    public void Spawn(GameObject zombiePrefab, SpawnDistance distance, ZombieParameters parameters)
    {
        currentZombiePrefab = zombiePrefab;
        Zombie zombie = pool.Get();
        zombiesAlive.Add(zombie);
        Vector3 spawnPoint = spawners[UnityEngine.Random.Range(0, spawners.Length - 1)].transform.position;
        zombie.Spawn(Vector3.up + spawnPoint, parameters);
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

    void OnDrawGizmosSelected()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.blue;
        var localSpawners = GameObject.FindGameObjectsWithTag("Zombie spawner");
        foreach (GameObject spawner in localSpawners)
        {
            Gizmos.DrawSphere(spawner.transform.position, 5f);
        }
    }
}
