// SaveData.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    // [추가] 슬롯 요약에서 쓰는 단순 퀘스트 리스트(완료 여부 포함)
    public List<QuestEntry> quests = new List<QuestEntry>();        // [추가]
    public PlayerSaveData player = new PlayerSaveData();            // [수정] 필드 구성 단순화
    public List<RegionSaveData> regions = new List<RegionSaveData>();
}

[Serializable]
public class QuestEntry    // [추가] SaveSelectUI 요약용 구조
{
    public string questId;
    public bool isCompleted;
}

[Serializable]
public class PlayerSaveData
{
    public long gold;

    // [수정] 포션 필드명을 PlayerInventory와 일치
    public int smallPotions;    // [수정]
    public int mediumPotions;   // [수정]
    public int largePotions;    // [수정]

    // [추가] HP 저장
    public int currentHP;   // [추가]
    public int maxHP;       // [추가]

    public List<ItemEntry> items = new List<ItemEntry>(); // [추가]

    // [추가] 보유 장비 목록(창고)
    public List<EquipmentSaveData> weapons = new List<EquipmentSaveData>(); // [추가]
    public List<EquipmentSaveData> armors = new List<EquipmentSaveData>(); // [추가]

    // [수정] 장착중인 장비
    public EquipmentSaveData equippedWeapon;
    public EquipmentSaveData equippedArmor;
}

[Serializable]
public class ItemEntry
{
    public string itemName;
    public int amount;
}

[Serializable]
public class EquipmentSaveData
{
    public string itemName;
    public ShopUI.ItemType type;
    public int statBonus;
    public string iconName;     // [추가] Resources 아이콘 이름
}

[Serializable]
public class RegionSaveData
{
    public string regionId;
    public bool isUnlocked;     // [추가] 영지 잠금 상태 저장
    public List<StageSaveData> stages = new List<StageSaveData>();
}

[Serializable]
public class StageSaveData
{
    public string stageId;
    public bool isUnlocked;
    public bool isCompleted;    // [추가]
}
