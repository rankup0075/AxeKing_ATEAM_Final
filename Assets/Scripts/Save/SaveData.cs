using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    public PlayerData player = new PlayerData();
    public List<QuestSaveData> quests = new List<QuestSaveData>();
    public List<RegionSaveData> regions = new List<RegionSaveData>();
}

[Serializable]
public class PlayerData
{
    public long gold;
    public int smallPotions;
    public int mediumPotions;
    public int largePotions;

    // 현재 체력
    public int currentHealth;

    // 인벤토리
    public List<ItemEntry> items = new List<ItemEntry>();
    public List<EquipmentEntry> weapons = new List<EquipmentEntry>();
    public List<EquipmentEntry> armors = new List<EquipmentEntry>();

    // 장착 중인 장비
    public EquipmentEntry equippedWeapon;
    public EquipmentEntry equippedArmor;
}

[Serializable]
public class ItemEntry
{
    public string name;
    public int count;
}

[Serializable]
public class EquipmentEntry
{
    public string itemName;
    public int type;        // ShopUI.ItemType을 int로 저장
    public int statBonus;
    public string iconName;
}

[Serializable]
public class QuestSaveData
{
    public string questId;
    public bool isAccepted;
    public bool isCompleted;
    public int currentProgress;
}

[Serializable]
public class RegionSaveData
{
    public string regionId;
    public List<StageSaveData> stages = new List<StageSaveData>();
}

[Serializable]
public class StageSaveData
{
    public string stageId;
    public bool isUnlocked;
}
