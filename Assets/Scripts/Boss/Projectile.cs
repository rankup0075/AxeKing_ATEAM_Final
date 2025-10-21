// Projectile.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Projectile : MonoBehaviour
{
    public float speed = 12f;
    public float lifeTime = 6f;
    public int damage = 10;
    public LayerMask hitMask; // Player, Wall µî

    public GameObject hitVFX;
    private Rigidbody rb;
    private bool launched;

    public void Launch(Vector3 dir)
    {
        if (launched) return;
        launched = true;
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.linearVelocity = dir.normalized * speed;
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & hitMask) == 0) return;

        if (other.TryGetComponent<PlayerHealth>(out var ph))
            ph.TakeDamage(damage);

        if (hitVFX) Instantiate(hitVFX, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
