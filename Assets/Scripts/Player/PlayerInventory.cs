using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Potions")]
    public int smallPotions = 0;
    public int mediumPotions = 0;
    public int largePotions = 0;

    [Header("Equipment")]
    public ItemEquipment currentWeapon;
    public ItemEquipment currentArmor;

    [Header("Items")]
    public Dictionary<string, int> items = new Dictionary<string, int>();

    public List<ItemEquipment> weaponStorage = new List<ItemEquipment>();
    public List<ItemEquipment> armorStorage = new List<ItemEquipment>();

    private PlayerHealth playerHealth;
    private PlayerController playerController;

    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerController = GetComponent<PlayerController>();

        // === 세이브 존재 여부 확인 ===
        bool hasSave =
            File.Exists(Path.Combine(Application.persistentDataPath, "save_auto.json")) ||
            File.Exists(Path.Combine(Application.persistentDataPath, "save_slot1.json")) ||
            File.Exists(Path.Combine(Application.persistentDataPath, "save_slot2.json")) ||
            File.Exists(Path.Combine(Application.persistentDataPath, "save_slot3.json"));

        if (!hasSave)
        {
            // 세이브 없을 때만 기본 아이템 목록 초기화
            InitializeItems();
        }

        UpdateUI();
    }

    public void InitializeItems()
    {
        // 기본 자원 초기화
        items["고블린의 가죽"] = 0;
        items["골렘의 파편"] = 0;
        items["화염 구슬"] = 0;
        items["눈물 조각"] = 0;
        items["찢어진 고서"] = 0;
    }

    // 아이템 추가
    public void AddItem(string itemName, int amount)
    {
        if (!items.ContainsKey(itemName))
        {
            items[itemName] = 0;
        }

        items[itemName] += amount;
        Debug.Log($"[Inventory] {itemName} {amount}개 추가 (총 {items[itemName]}개)");

        if (QuestManager.Instance != null)
            QuestManager.Instance.UpdateQuestProgress();

        UIManager.Instance?.UpdateHUDPotions(smallPotions, mediumPotions, largePotions);

        UpdateUI();

        GameManager.Instance?.SavePlayerData();
    }

    // ==============================
    // [NEW] 재료 전용 추가 함수 (드랍 아이템 획득용)
    // ==============================
    public void AddMaterial(string itemName, int amount)
    {
        if (!items.ContainsKey(itemName))
            items[itemName] = 0;

        items[itemName] += amount;
        Debug.Log($"[Inventory] 재료 '{itemName}' {amount}개 획득 (총 {items[itemName]}개)");

        UpdateUI();

        // 퀘스트 업데이트
        if (QuestManager.Instance != null)
            QuestManager.Instance.UpdateQuestProgress();

        // UI에 알림 (플로팅 텍스트)
        UIManager.Instance?.ShowFloatingText($"+{amount} {itemName}", transform.position + Vector3.up * 2f);

        GameManager.Instance?.SavePlayerData();
    }


    // 아이템 제거
    public bool RemoveItem(string itemName, int amount)
    {
        if (items.ContainsKey(itemName) && items[itemName] >= amount)
        {
            items[itemName] -= amount;
            Debug.Log($"[Inventory] {itemName} {amount}개 제거 (남은 {items[itemName]}개)");

            if (QuestManager.Instance != null)
                QuestManager.Instance.UpdateQuestProgress();

            UpdateUI();

            return true;
        }
        Debug.LogWarning($"[Inventory] {itemName} 부족 (시도: {amount}, 보유: {GetItemCount(itemName)})");
        return false;
    }

    // 개수 조회
    public int GetItemCount(string itemName)
    {
        return items.ContainsKey(itemName) ? items[itemName] : 0;
    }

    public void AddEquipment(string itemName, ShopUI.ItemType type, int statBonus, Sprite icon = null)
    {
        ItemEquipment newEquipment = new ItemEquipment
        {
            EquipmentitemName = itemName,
            Equipmenttype = type,
            EquipmentstatBonus = statBonus,
            icon = icon
        };

        if (type == ShopUI.ItemType.Weapon)
        {
            weaponStorage.Add(newEquipment);
            Debug.Log($"[Inventory] 무기 {itemName} 창고에 추가됨 (총 {weaponStorage.Count}개 보유)");
        }
        else if (type == ShopUI.ItemType.Armor)
        {
            armorStorage.Add(newEquipment);
            Debug.Log($"[Inventory] 방어구 {itemName} 창고에 추가됨 (총 {armorStorage.Count}개 보유)");
        }
    }

    public void EquipItem(ItemEquipment equipment, ShopUI.ItemType type)
    {
        if (type == ShopUI.ItemType.Weapon)
        {
            if (currentWeapon != null)
                currentWeapon.RemoveStats(playerController, playerHealth);

            currentWeapon = equipment;

            if (equipment != null)
                equipment.ApplyStats(playerController, playerHealth);
        }
        else if (type == ShopUI.ItemType.Armor)
        {
            if (currentArmor != null)
                currentArmor.RemoveStats(playerController, playerHealth);

            currentArmor = equipment;

            if (equipment != null)
                equipment.ApplyStats(playerController, playerHealth);
        }
    }

    public void UsePotion(int potionType)
    {
        if (playerHealth.CurrentHealth >= playerHealth.MaxHealth) return;

        switch (potionType)
        {
            case 0: if (smallPotions > 0) { smallPotions--; playerHealth.Heal(30); } break;
            case 1: if (mediumPotions > 0) { mediumPotions--; playerHealth.Heal(50); } break;
            case 2: if (largePotions > 0) { largePotions--; playerHealth.Heal(playerHealth.MaxHealth); } break;
        }

        UpdateUI();
    }

    public void AddPotion(int potionType, int amount)
    {
        switch (potionType)
        {
            case 0: smallPotions += amount; break;
            case 1: mediumPotions += amount; break;
            case 2: largePotions += amount; break;
        }
        UpdateUI();
    }

    void UpdateUI()
    {
        UIManager.Instance.UpdatePotionCount(smallPotions, mediumPotions, largePotions);

        // [NEW] HUD 포션 UI도 갱신
        UIManager.Instance.UpdateHUDPotions(smallPotions, mediumPotions, largePotions);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.goblinLeatherText.text = GetItemCount("고블린의 가죽").ToString();
            UIManager.Instance.golemStoneText.text = GetItemCount("골렘의 파편").ToString();
            UIManager.Instance.flameBeadsText.text = GetItemCount("화염 구슬").ToString();
            UIManager.Instance.pieceofTearsText.text = GetItemCount("눈물 조각").ToString();
            UIManager.Instance.tornOldBookText.text = GetItemCount("찢어진 고서").ToString();
        }
    }

    // ==============================
    // 테스트용: 키 입력으로 아이템 수 조정
    // ==============================
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J)) AddItem("고블린의 가죽", 10);
        if (Input.GetKeyDown(KeyCode.K)) AddItem("골렘의 파편", 10);
        if (Input.GetKeyDown(KeyCode.L)) AddItem("화염 구슬", 10);
        //if (Input.GetKeyDown(KeyCode.Alpha4)) AddItem("눈물 조각", 10);
        //if (Input.GetKeyDown(KeyCode.Alpha5)) AddItem("찢어진 고서", 10);

        if (Input.GetKeyDown(KeyCode.Q))
            Debug.Log($"[Inventory] 고블린의 가죽: {GetItemCount("고블린의 가죽")}");
    }


    // ==============================
    // 펫 AI 연동용: 현재 장비 이름 반환
    // ==============================
    public string GetEquippedWeaponName()
    {
        if (currentWeapon != null && !string.IsNullOrEmpty(currentWeapon.EquipmentitemName))
            return currentWeapon.EquipmentitemName;
        return "무기 없음";
    }

    public string GetEquippedArmorName()
    {
        if (currentArmor != null && !string.IsNullOrEmpty(currentArmor.EquipmentitemName))
            return currentArmor.EquipmentitemName;
        return "방어구 없음";
    }

}
