// DamageOnTrigger.cs
using UnityEngine;

public class DamageOnTrigger : MonoBehaviour
{
    public int damage = 10;
    public bool continuous = false;      // true = 닿아있는 동안 주기적 데미지
    public float tickInterval = 0.2f;    // continuous일 때 간격
    public bool destroyOnHit = false;    // 단발형에서 맞으면 파괴

    private float lastTickTime = -999f;

    void OnTriggerStay(Collider other)
    {
        if (!continuous) return;
        if (!other.TryGetComponent<PlayerHealth>(out var ph)) return;

        if (Time.time - lastTickTime >= tickInterval)
        {
            ph.TakeDamage(damage);
            lastTickTime = Time.time;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (continuous) return;
        if (!other.TryGetComponent<PlayerHealth>(out var ph)) return;

        ph.TakeDamage(damage);
        if (destroyOnHit) Destroy(gameObject);
    }
}
