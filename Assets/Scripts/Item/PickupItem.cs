using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class PickupItem : MonoBehaviour
{
    public enum PickupType { Gold, Material }

    [Header("설정")]
    public PickupType type = PickupType.Gold;
    public string itemName = "Frost Crystal"; // 재료 이름
    public int amount = 1;

    [Header("동작 세부 설정")]
    public float pickupRange = 2.5f;
    public float moveSpeed = 7f;
    public LayerMask groundMask;

    Transform player;
    Rigidbody rb;
    Collider col;
    bool snapping;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        rb.useGravity = true;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        col.isTrigger = false;

        if (player)
        {
            foreach (Collider pCol in player.GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(col, pCol, true);
        }

        // 같은 종류의 아이템끼리 충돌 방지
        var allPickups = FindObjectsByType<PickupItem>(FindObjectsSortMode.None);
        foreach (var other in allPickups)
        {
            if (other != this)
                Physics.IgnoreCollision(col, other.GetComponent<Collider>(), true);
        }

        Destroy(gameObject, 20f); // 20초 후 자동 삭제
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
                player.position + Vector3.up * 0.5f,
                moveSpeed * Time.deltaTime
            );

            if (dist < 0.6f)
            {
                Collect();
                Destroy(gameObject);
            }
        }
        else if (snapping)
        {
            snapping = false;
            rb.useGravity = true;
            rb.isKinematic = false;
        }
    }

    void Collect()
    {
        switch (type)
        {
            case PickupType.Gold:
                GameManager.Instance.AddGold(amount);
                UIManager.Instance?.ShowFloatingText($"+{amount} Gold 💰", player.position + Vector3.up * 2);
                break;

            case PickupType.Material:
                var inv = player.GetComponent<PlayerInventory>();
                if (inv != null)
                {
                    inv.AddMaterial(itemName, amount);
                    UIManager.Instance?.ShowFloatingText($"+{itemName} ❄️", player.position + Vector3.up * 2);
                }
                break;
        }
    }
}
