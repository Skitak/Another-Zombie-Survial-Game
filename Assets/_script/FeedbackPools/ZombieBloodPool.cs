using Asmos.Bus;
using Asmos.Timers;
using UnityEngine;
using UnityEngine.Pool;

public class ZombieBloodPool : MonoBehaviour
{
    ObjectPool<GameObject> pool;
    [SerializeField] GameObject zombieBlood;
    [SerializeField] float bloodTimeout;

    void Start()
    {
        pool = new(CreateBlood, GetBlood, ReleaseBlood, DestroyBlood, true, 40, 150);
        Bus.Subscribe("zombie hit data", (o) => PlaceBlood((Vector3)o[0], Quaternion.LookRotation((Vector3)o[1])));
    }
    public void PlaceBlood(Vector3 position, Quaternion rotation)
    {
        GameObject blood = pool.Get();
        blood.transform.SetPositionAndRotation(position, rotation);
        blood.GetComponent<ParticleSystem>().Play();
        new Timer(bloodTimeout, () => pool.Release(blood)).Play();
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
