using UnityEngine;

//[CreateAssetMenu(fileName = "New Equipment", menuName = "Inventory/Equipment")]
public class ItemEquipment  
{
    public string EquipmentitemName;
    public ShopUI.ItemType Equipmenttype;
    public int EquipmentstatBonus;
    public Sprite icon;

    public enum EquipmentSlot { Weapon, Armor }

    public void ApplyStats(PlayerController player, PlayerHealth health)
    {
        if (player == null || health == null)
        {
            Debug.LogWarning("[ItemEquipment] ApplyStats called too early (player or health null)");
            return;
        }

        switch (Equipmenttype)
        {
            case ShopUI.ItemType.Weapon:
                player.attackDamage += EquipmentstatBonus;
                break;

            case ShopUI.ItemType.Armor:
                // [수정] 직접 더하지 말고 PlayerHealth API 사용
                health.IncreaseMaxHealth(EquipmentstatBonus, keepCurrent: true);
                break;
        }
    }


    public void RemoveStats(PlayerController player, PlayerHealth health)
    {
        if (Equipmenttype == ShopUI.ItemType.Weapon)
        {
            player.attackDamage -= EquipmentstatBonus;
        }
        else if (Equipmenttype == ShopUI.ItemType.Armor)
        {
            health.IncreaseMaxHealth(-EquipmentstatBonus, keepCurrent: true);
        }
    }
}

