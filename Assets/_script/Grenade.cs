using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Asmos.Timers;
public class Grenade : MonoBehaviour
{
    public LayerMask targetLayerMask;
    public LayerMask allLayerMask;
    Timer timerToExplosion;
    new ParticleSystem particleSystem;
    const float BASE_EXPLOSION_RADIUS = 3.5f;
    [HideInInspector] public new Rigidbody rigidbody;
    void Awake()
    {
        particleSystem = GetComponentInChildren<ParticleSystem>();
        rigidbody = GetComponentInChildren<Rigidbody>();
        timerToExplosion = new Timer(StatManager.Get(StatType.EXPLOSION_SPEED), Explode).Play();
    }
    // TODO: This should collide with walls
    // For now, it only checks distance.
    void Explode()
    {
        rigidbody.isKinematic = true;
        transform.rotation = quaternion.identity;
        transform.GetChild(0).gameObject.SetActive(false);
        particleSystem.gameObject.transform.localScale = Vector3.one / BASE_EXPLOSION_RADIUS * Player.player.explosionRadius;
        particleSystem.Play();
        Collider[] hits = Physics.OverlapSphere(transform.position, Player.player.explosionRadius, targetLayerMask);
        // List<ITakeExplosionDamages> affectedTargets = new();
        Dictionary<ITakeExplosionDamages, float> affectedTargets = new();
        foreach (Collider collider in hits)
        {
            ITakeExplosionDamages target = collider.GetComponentInParent<ITakeExplosionDamages>();
            float distance = Vector3.Distance(transform.position, collider.transform.position);
            if (!affectedTargets.ContainsKey(target) || affectedTargets[target] > distance)
                affectedTargets[target] = distance;
            // if (Physics.Raycast(transform.position, collider.transform.position - transform.position, out RaycastHit hit, Mathf.Infinity, allLayerMask))
            // {
            //     if (hit.collider != collider)
            //         continue;
            // }
        }
        foreach (ITakeExplosionDamages key in affectedTargets.Keys)
            key.TakeExplosionDamages(1 - ((affectedTargets[key] / Player.player.explosionRadius) * 0.5f));

        TimerManager.RemoveTimer(timerToExplosion);
        Timer.OneShotTimer(5f, () => Destroy(gameObject));
    }
}
