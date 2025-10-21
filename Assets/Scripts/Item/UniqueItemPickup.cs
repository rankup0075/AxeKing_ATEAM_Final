using UnityEngine;

// 플레이어 근접 시 흡입되어 인벤토리에 고유 아이템 추가. 스냅 해제 시 바닥으로 낙하.
[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class UniqueItemPickup : MonoBehaviour, IUniquePickup
{
    [Header("Pickup Settings")]
    public string itemName;
    public int count = 1;
    public float pickupRange = 2f;
    public float moveSpeed = 6f;
    public float groundCheckDistance = 0.5f;
    public LayerMask groundMask;
    public string pickupTag = "Player";

    Transform player;
    Rigidbody rb;
    Collider col;
    bool snapping;

    public void SetItem(string name, int amt)
    {
        itemName = name;
        count = amt;
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag(pickupTag)?.transform;
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        rb.useGravity = true;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        col.isTrigger = false;

        if (player)
        {
            foreach (Collider pCol in player.GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(col, pCol, true);
        }

        var golds = Object.FindObjectsByType<GoldPickup>(FindObjectsSortMode.None);
        foreach (var other in golds)
            Physics.IgnoreCollision(col, other.GetComponent<Collider>(), true);

        var uniques = Object.FindObjectsByType<UniqueItemPickup>(FindObjectsSortMode.None);
        foreach (var other in uniques)
        {
            if (other != this)
                Physics.IgnoreCollision(col, other.GetComponent<Collider>(), true);
        }
    }

    void Update()
    {
        if (!player) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist < pickupRange)
        {
            snapping = true;
            rb.useGravity = false;
            rb.isKinematic = true;

            transform.position = Vector3.MoveTowards(
                transform.position,
                player.position + Vector3.up,
                moveSpeed * Time.deltaTime
            );

            if (dist < 0.6f)
            {
                var inv = player.GetComponent<PlayerInventory>();
                if (inv != null) inv.AddItem(itemName, count);
                Destroy(gameObject);
            }
        }
        else if (snapping)
        {
            snapping = false;
            rb.isKinematic = false;
            rb.useGravity = true;

            if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, 2f, groundMask))
            {
                if (transform.position.y < hit.point.y + 0.05f)
                {
                    Vector3 corrected = transform.position;
                    corrected.y = hit.point.y + 0.05f;
                    transform.position = corrected;
                    rb.linearVelocity = Vector3.zero;
                }
            }
        }
    }
}
