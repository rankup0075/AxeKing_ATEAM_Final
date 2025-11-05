using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PickupInteractor : MonoBehaviour
{
    public PlayerInventory inventory;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        var drop = other.GetComponentInParent<DropItem>();
        if (drop == null) return;

        if (inventory == null)
        {
            inventory = GetComponentInParent<PlayerInventory>();
            if (inventory == null) return;
        }

        // 인벤토리 추가
        if (drop.ItemName == "골드")
        {
            inventory.AddItem("골드", drop.Amount);
        }
        else
        {
            inventory.AddMaterial(drop.ItemName, drop.Amount);
        }

        Debug.Log($"[획득] {drop.ItemName} {drop.Amount}개");

        Destroy(drop.gameObject);
    }
}
