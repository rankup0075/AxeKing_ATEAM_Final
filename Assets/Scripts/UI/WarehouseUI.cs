using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class WarehouseUI : MonoBehaviour
{
    public GameObject warehousePanel;
    public Button weaponTabButton;
    public Button armorTabButton;
    public Button closeButton;
    public Button equipButton;
    public Button unequipButton;

    public Transform itemListContainer;
    public GameObject itemPrefab;
    public GameObject messagePanel;

    private PlayerInventory playerInventory;
    private ShopUI.ItemType currentTab = ShopUI.ItemType.Weapon;
    private ItemEquipment selectedItem;

    [Header("Camera")]
    public Transform chestTransform;
    private CameraFollow camFollow;

    private GameManager gameManager;

    [Header("Stats UI")]
    public TextMeshProUGUI attackStatText;
    public TextMeshProUGUI healthStatText;

    [Header("Equipped Panel")]
    public Transform equippedWeaponPanel;
    public Transform equippedArmorPanel;
    private GameObject equippedWeaponUI;
    private GameObject equippedArmorUI;

    void Start()
    {
        camFollow = Camera.main.GetComponent<CameraFollow>();
        playerInventory = GameObject.FindWithTag("Player").GetComponent<PlayerInventory>();
        gameManager = GameManager.Instance;

        closeButton.onClick.AddListener(CloseWarehouse);
        weaponTabButton.onClick.AddListener(() => ShowItems(ShopUI.ItemType.Weapon));
        armorTabButton.onClick.AddListener(() => ShowItems(ShopUI.ItemType.Armor));
        equipButton.onClick.AddListener(EquipSelectedItem);
        unequipButton.onClick.AddListener(UnequipCurrentItem);

        warehousePanel.SetActive(false);
        messagePanel.SetActive(false);
        equipButton.interactable = false;
        unequipButton.interactable = false;
    }
    public void RefreshAll()
    {
        ShowItems(ShopUI.ItemType.Weapon);
        ShowItems(ShopUI.ItemType.Armor);
    }
    void RefreshStats()
    {
        var pc = playerInventory.GetComponent<PlayerController>();
        var ph = playerInventory.GetComponent<PlayerHealth>();

        attackStatText.text = $"공격력: {pc.attackDamage}";
        healthStatText.text = $"체력: {ph.CurrentHealth}/{ph.MaxHealth}";
    }

    public void OpenWarehouse()
    {
        warehousePanel.SetActive(true);
        Debug.Log("OpenWareHouse 감지");

        if (camFollow != null && chestTransform != null)
            camFollow.SetTarget(chestTransform, false, true);

        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var controller = player.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.canMove = false;
                controller.StopImmediately();
            }
        }

        selectedItem = null;
        equipButton.interactable = false;
        unequipButton.interactable = false;

        // 현재 장착중인 아이템 패널 먼저 갱신
        RefreshEquippedPanel();

        // 인벤토리 탭 갱신
        ShowItems(currentTab);
    }

    void CloseWarehouse()
    {
        warehousePanel.SetActive(false);

        if (camFollow != null) camFollow.ResetTarget();

        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var controller = player.GetComponent<PlayerController>();
            if (controller != null) controller.canMove = true;
        }
    }

    void ShowItems(ShopUI.ItemType type)
    {
        currentTab = type;

        foreach (Transform child in itemListContainer)
            Destroy(child.gameObject);

        List<ItemEquipment> itemsToShow = (type == ShopUI.ItemType.Weapon) ?
            playerInventory.weaponStorage : playerInventory.armorStorage;

        Debug.Log($"[Warehouse] {type} 리스트 불러오기 - 총 {itemsToShow.Count}개");

        foreach (var item in itemsToShow)
        {
            GameObject obj = Instantiate(itemPrefab, itemListContainer);

            obj.transform.Find("NameText").GetComponent<TextMeshProUGUI>().text = item.EquipmentitemName;
            obj.transform.Find("DescriptionText").GetComponent<TextMeshProUGUI>().text =
                type == ShopUI.ItemType.Weapon ? $"공격력 +{item.EquipmentstatBonus}" : $"체력 +{item.EquipmentstatBonus}";
            obj.transform.Find("ItemImage").GetComponent<Image>().sprite = item.icon;

            var statusText = obj.transform.Find("StatusText").GetComponent<TextMeshProUGUI>();
            bool isEquipped = (type == ShopUI.ItemType.Weapon &&
                   playerInventory.currentWeapon != null &&
                   playerInventory.currentWeapon.EquipmentitemName == item.EquipmentitemName)
               || (type == ShopUI.ItemType.Armor &&
                   playerInventory.currentArmor != null &&
                   playerInventory.currentArmor.EquipmentitemName == item.EquipmentitemName);


            statusText.text = isEquipped ? "장착중" : "";
            statusText.gameObject.SetActive(true);

            obj.GetComponent<Button>().onClick.AddListener(() =>
            {
                selectedItem = item;
                HighlightSelection(obj);

                // 버튼 상태 갱신
                equipButton.interactable = !isEquipped;   // 장착중이면 비활성화
                unequipButton.interactable = isEquipped; // 장착중이면 해제 가능

                Debug.Log(isEquipped
                    ? $"[Warehouse] {item.EquipmentitemName} 선택됨 (장착중 → 해제 가능)"
                    : $"[Warehouse] {item.EquipmentitemName} 선택됨 (장착 가능)");
            });
        }

        RefreshStats();
    }

    void HighlightSelection(GameObject selected)
    {
        foreach (Transform child in itemListContainer)
        {
            var image = child.GetComponent<Image>();
            if (image != null)
                image.color = Color.white;
        }
        selected.GetComponent<Image>().color = new Color(1f, 0.7f, 0.7f);
    }

    void EquipSelectedItem()
    {
        if (selectedItem == null) return;

        playerInventory.EquipItem(selectedItem, selectedItem.Equipmenttype);
        Debug.Log($"[Warehouse] {selectedItem.EquipmentitemName} 장착 완료");

        // === 외형 반영 추가 ===
        var visual = FindFirstObjectByType<PlayerVisual>();
        if (visual != null)
        {
            if (selectedItem.Equipmenttype == ShopUI.ItemType.Weapon)
                visual.ApplyWeapon(selectedItem.EquipmentitemName);
            else if (selectedItem.Equipmenttype == ShopUI.ItemType.Armor)
                visual.ApplyArmor(selectedItem.EquipmentitemName);
        }

        // === 스탯 갱신 ===
        RefreshEquippedPanel();
        ShowItems(currentTab);
        RefreshStats();

        equipButton.interactable = false;
        unequipButton.interactable = true;
    }

    void UnequipCurrentItem()
    {
        if (selectedItem == null) return;

        var visual = FindFirstObjectByType<PlayerVisual>();

        if (selectedItem.Equipmenttype == ShopUI.ItemType.Weapon &&
            playerInventory.currentWeapon != null &&
            playerInventory.currentWeapon.EquipmentitemName == selectedItem.EquipmentitemName)
        {
            playerInventory.EquipItem(null, ShopUI.ItemType.Weapon);
            if (visual != null) visual.ApplyWeapon(null);
            Debug.Log($"[Warehouse] {selectedItem.EquipmentitemName} 무기 해제 완료");
        }
        else if (selectedItem.Equipmenttype == ShopUI.ItemType.Armor &&
                 playerInventory.currentArmor != null &&
                 playerInventory.currentArmor.EquipmentitemName == selectedItem.EquipmentitemName)
        {
            playerInventory.EquipItem(null, ShopUI.ItemType.Armor);
            if (visual != null) visual.ApplyArmor(null);
            Debug.Log($"[Warehouse] {selectedItem.EquipmentitemName} 방어구 해제 완료");
        }

        selectedItem = null;
        RefreshEquippedPanel();
        ShowItems(currentTab);
        equipButton.interactable = false;
        unequipButton.interactable = false;
    }


    void RefreshEquippedPanel()
    {
        // 무기 슬롯 갱신
        if (equippedWeaponUI != null) Destroy(equippedWeaponUI);
        if (playerInventory.currentWeapon != null)
            equippedWeaponUI = CreateEquippedUI(playerInventory.currentWeapon, equippedWeaponPanel);

        // 방어구 슬롯 갱신
        if (equippedArmorUI != null) Destroy(equippedArmorUI);
        if (playerInventory.currentArmor != null)
            equippedArmorUI = CreateEquippedUI(playerInventory.currentArmor, equippedArmorPanel);
    }

    GameObject CreateEquippedUI(ItemEquipment item, Transform parent)
    {
        GameObject obj = Instantiate(itemPrefab, parent);

        obj.transform.Find("NameText").GetComponent<TextMeshProUGUI>().text = item.EquipmentitemName;
        obj.transform.Find("DescriptionText").GetComponent<TextMeshProUGUI>().text =
            item.Equipmenttype == ShopUI.ItemType.Weapon ?
            $"공격력 +{item.EquipmentstatBonus}" : $"체력 +{item.EquipmentstatBonus}";
        obj.transform.Find("ItemImage").GetComponent<Image>().sprite = item.icon;

        // 장착 패널에서는 StatusText 숨김
        obj.transform.Find("StatusText").gameObject.SetActive(false);

        // 클릭 시 해제 버튼 활성화
        obj.GetComponent<Button>().onClick.AddListener(() =>
        {
            selectedItem = item;
            equipButton.interactable = false;
            unequipButton.interactable = true;
            Debug.Log($"[Warehouse] 장착 패널 아이템 {item.EquipmentitemName} 선택됨 (해제 가능)");
        });

        return obj;
    }
}
