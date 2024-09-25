using UnityEngine;
using UnityEngine.Pool;

public class ZombieBloodPool : MonoBehaviour
{
    public static ZombieBloodPool instance;
    ObjectPool<GameObject> pool;
    [SerializeField] GameObject zombieBlood;
    [SerializeField] float bloodTimeout;

    void Start()
    {
        instance = this;
        pool = new(CreateBlood, GetBlood, ReleaseBlood, DestroyBlood, true, 40, 150);
    }
    public static void PlaceBlood(Vector3 position, Quaternion rotation)
    {
        GameObject blood = instance.pool.Get();
        blood.transform.SetPositionAndRotation(position, rotation);
        blood.GetComponent<ParticleSystem>().Play();
        new Timer(instance.bloodTimeout, () => instance.pool.Release(blood)).Play();
    }
    GameObject CreateBlood()
    {
        return Instantiate(zombieBlood);
    }
    void GetBlood(GameObject blood)
    {
        blood.SetActive(true);
    }
    void ReleaseBlood(GameObject blood)
    {
        blood.SetActive(false);
    }
    void DestroyBlood(GameObject blood)
    {
        Destroy(blood);
    }
}
