// 플레이어 근접 시 흡입되어 골드 지급
using UnityEngine;

public class GoldPickup : MonoBehaviour, IGoldPickup
{
    public int amount;
    public float pickupRange = 2f;
    public float moveSpeed = 6f;
    Transform player;

    public void SetGold(int value) => amount = value;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        if (!player) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist < pickupRange)
        {
            transform.position = Vector3.MoveTowards(transform.position, player.position + Vector3.up, moveSpeed * Time.deltaTime);
            if (dist < 0.5f)
            {
                GameManager.Instance.AddGold(amount);
                Destroy(gameObject);
            }
        }
    }
}
