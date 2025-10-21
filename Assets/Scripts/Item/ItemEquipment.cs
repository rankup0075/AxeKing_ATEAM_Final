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
        if (Equipmenttype == ShopUI.ItemType.Weapon)
        {
            player.attackDamage += EquipmentstatBonus;
        }
        else if (Equipmenttype == ShopUI.ItemType.Armor)
        {
            health.IncreaseMaxHealth(EquipmentstatBonus);
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
            health.IncreaseMaxHealth(-EquipmentstatBonus);
        }
    }
}

