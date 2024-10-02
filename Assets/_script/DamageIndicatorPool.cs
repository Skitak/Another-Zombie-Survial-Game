using Asmos.Bus;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;

public class DamageIndicatorPool : MonoBehaviour
{
    // public static ZombieBloodPool instance;
    [SerializeField] GameObject indicator;
    [SerializeField] float timeout;
    [SerializeField] float force;
    ObjectPool<TMP_Text> pool;
    Canvas canvas;
    Color baseColor = new(0.9215686f, 0.08614843f, 0.05098037f);
    Color baseColorTransparent = new(0.9215686f, 0.08614843f, 0.050980373f, 0f);
    void Start()
    {
        // instance = this;
        canvas = GetComponentInChildren<Canvas>();
        pool = new(CreateIndicator, GetIndicator, ReleaseIndicator, DestroyIndicator, true, 40, 150);
        Bus.Subscribe("zombie hit data", PlaceIndicator);
    }
    void PlaceIndicator(params object[] args)
    {
        RaycastHit hit = (RaycastHit)args[0];
        TMP_Text indicator = pool.Get();
        indicator.SetText(args[1].ToString());
        indicator.transform.SetParent(canvas.transform);
        indicator.transform.position = hit.point;
        indicator.color = baseColor;
        Rigidbody rigid = indicator.GetComponent<Rigidbody>();
        rigid.isKinematic = false;
        rigid.AddForce(ApplySpreadToDirection(Vector3.up, 60f) * force);
        Timer timer = new Timer(timeout, () =>
        {
            rigid.isKinematic = true;
            pool.Release(indicator);
        }).Play();
        timer.OnTimerUpdate += () =>
        {
            indicator.transform.LookAt(Camera.main.transform.forward + indicator.transform.position);
            indicator.color = Color.Lerp(baseColor, baseColorTransparent, timer.GetPercentage());
        };
    }
    TMP_Text CreateIndicator()
    {
        return Instantiate(indicator).GetComponent<TMP_Text>();
    }
    void GetIndicator(TMP_Text indicator)
    {
        indicator.gameObject.SetActive(true);
    }
    void ReleaseIndicator(TMP_Text indicator)
    {
        indicator.gameObject.SetActive(false);
    }
    void DestroyIndicator(TMP_Text indicator)
    {
        Destroy(indicator.gameObject);
    }
    public Vector3 ApplySpreadToDirection(Vector3 initialDirection, float spread)
    {
        float spreadRange = spread / 2f;
        Vector2 randomRotation = UnityEngine.Random.insideUnitCircle * spreadRange;
        Vector3 newDirection = Quaternion.AngleAxis(randomRotation.x, Vector3.left) * initialDirection;
        return Quaternion.AngleAxis(randomRotation.y, Vector3.Cross(newDirection, Vector3.left)) * newDirection;
    }
}
