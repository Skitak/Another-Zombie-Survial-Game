using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ZombieSpawnerManager : MonoBehaviour
{
    public static ZombieSpawnerManager instance;
    static GameObject[] spawners;
    ObjectPool<Zombie> pool;
    GameObject currentZombiePrefab;
    int zombiesAlive;
    public enum SpawnDistance
    {
        CLOSE, NORMAL, FAR
    }
    void Awake()
    {
        instance = this;
        spawners = GameObject.FindGameObjectsWithTag("Zombie spawner");
        pool = new(CreateZombie, GetZombie, ReleaseZombie, DestroyZombie, true, 20, 100);
        zombiesAlive = 0;
    }
    public void Spawn(GameObject zombiePrefab, SpawnDistance distance, ZombieParameters parameters)
    {
        currentZombiePrefab = zombiePrefab;
        Zombie zombie = pool.Get();
        ++zombiesAlive;
    }

    public void ZombieDied(Zombie zombie)
    {
        pool.Release(zombie);
        if (--zombiesAlive == 0)
            WaveManager.instance.NoZombiesLeft();
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
