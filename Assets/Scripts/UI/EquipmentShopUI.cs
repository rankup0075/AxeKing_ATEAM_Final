

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class EquipmentShopUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject equipmentShopPanel;
    public Button closeButton;
    public Button buyButton;
    public TextMeshProUGUI goldText;

    public Button weaponButton;
    public Button armorButton;

    public Transform weaponContainer;
    public Transform armorContainer;

    public GameObject itemPrefab;

    [Header("Message")]
    public GameObject messagePanel;
    public TextMeshProUGUI messageText;

    [Header("Camera")]
    public Transform npcTransform;
    private CameraFollow camFollow;

    private PlayerInventory playerInventory;
    private GameManager gameManager;

    private EquipmentItem selectedItem = null;
    private GameObject selectedItemObject = null;

    private HashSet<string> ownedItems = new HashSet<string>();

    public enum ItemType { Weapon, Armor }

    [System.Serializable]
    public class EquipmentItem
    {
        public string itemName;
        public long price;
        public string description;
        public ItemType type;
        public int statBonus;
        public Sprite icon;
    }

    public EquipmentItem[] shopItems;

    void Start()
    {
        camFollow = Camera.main.GetComponent<CameraFollow>();
        playerInventory = GameObject.FindWithTag("Player")?.GetComponent<PlayerInventory>();
        gameManager = GameManager.Instance;

        if (closeButton != null) closeButton.onClick.AddListener(CloseShop);
        if (equipmentShopPanel != null) equipmentShopPanel.SetActive(false);
        if (messagePanel != null) messagePanel.SetActive(false);

        closeButton.onClick.AddListener(CloseShop);
        buyButton.onClick.AddListener(BuySelectedItem);

        weaponButton.onClick.AddListener(() =>
        {
            weaponContainer.gameObject.SetActive(true);
            armorContainer.gameObject.SetActive(false);
        });

        armorButton.onClick.AddListener(() =>
        {
            weaponContainer.gameObject.SetActive(false);
            armorContainer.gameObject.SetActive(true);
        });

        weaponContainer.gameObject.SetActive(true);
        armorContainer.gameObject.SetActive(false);
        messagePanel.SetActive(false);

        CreateShopItems();
    }

    void CreateShopItems()
    {
        foreach (Transform child in weaponContainer) Destroy(child.gameObject);      //  추가
        foreach (Transform child in armorContainer) Destroy(child.gameObject);       //  추가
        ownedItems.Clear();

        if (playerInventory != null)
        {
            foreach (var w in playerInventory.weaponStorage)
                ownedItems.Add(w.EquipmentitemName);
            foreach (var a in playerInventory.armorStorage)
                ownedItems.Add(a.EquipmentitemName);
        }

        foreach (var item in shopItems)
        {
            GameObject ui = Instantiate(itemPrefab);
            ui.transform.SetParent(item.type == ItemType.Weapon ? weaponContainer : armorContainer, false);

            var nameText = ui.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
            var priceText = ui.transform.Find("PriceText").GetComponent<TextMeshProUGUI>();
            var descText = ui.transform.Find("DescriptionText").GetComponent<TextMeshProUGUI>();
            var image = ui.transform.Find("ItemImage").GetComponent<Image>();
            var ownedLabel = ui.transform.Find("StatusText").gameObject;
            var button = ui.GetComponent<Button>();                    // ★ 추가

            nameText.text = item.itemName;
            priceText.text = $"{item.price:N0} G";
            descText.text = item.description;
            image.sprite = item.icon;

            bool alreadyOwned =
            playerInventory.weaponStorage.Exists(w => w.EquipmentitemName == item.itemName) ||
            playerInventory.armorStorage.Exists(a => a.EquipmentitemName == item.itemName);

            if (alreadyOwned)
            {
                ownedLabel.SetActive(true);
                button.interactable = false;
                ownedItems.Add(item.itemName);
            }
            else
            {
                ownedLabel.SetActive(false);
                var captured = item;
                button.onClick.AddListener(() => SelectItem(captured, ui));
            }
        }
    }

    void SelectItem(EquipmentItem item, GameObject ui)
    {
        if (ownedItems.Contains(item.itemName))
        {
            ShowMessage("이미 보유중입니다!", Color.gray);
            return;
        }

        if (selectedItemObject != null)
            selectedItemObject.GetComponent<Image>().color = Color.white;

        selectedItem = item;
        selectedItemObject = ui;
        ui.GetComponent<Image>().color = new Color(1f, 0f, 0f);
    }

    public void OpenShop()
    {
        equipmentShopPanel.SetActive(true);
        RefreshUI();

        if (camFollow != null && npcTransform != null)
            camFollow.SetTarget(npcTransform, true);

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
    }

    public void CloseShop()
    {
        equipmentShopPanel.SetActive(false);

        if (camFollow != null) camFollow.ResetTarget();

        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var controller = player.GetComponent<PlayerController>();
            if (controller != null) controller.canMove = true;
        }

        selectedItem = null;
        selectedItemObject = null;
        messagePanel.SetActive(false);
    }

    void RefreshUI()
    {
        goldText.text = $"Gold: {gameManager.Gold:N0} G";
    }

    void BuySelectedItem()
    {
        if (selectedItem == null)
        {
            ShowMessage("아이템을 선택하세요!", Color.yellow);
            return;
        }

        if (ownedItems.Contains(selectedItem.itemName))
        {
            ShowMessage("이미 보유중입니다!", Color.gray);
            return;
        }

        if (gameManager.Gold < selectedItem.price)
        {
            ShowMessage("돈이 부족합니다!", Color.red);
            return;
        }

        gameManager.SpendGold(selectedItem.price);

        ownedItems.Add(selectedItem.itemName);

        var ownedLabel = selectedItemObject.transform.Find("StatusText");
        if (ownedLabel != null) ownedLabel.gameObject.SetActive(true);

        selectedItemObject.GetComponent<Button>().interactable = false;
        selectedItemObject.GetComponent<Image>().color = Color.white;

        // 창고 저장
        playerInventory.AddEquipment(selectedItem.itemName,
            selectedItem.type == ItemType.Weapon ? ShopUI.ItemType.Weapon : ShopUI.ItemType.Armor,
            selectedItem.statBonus,
            selectedItem.icon);

        Debug.Log($"[Shop] {selectedItem.itemName} 구매 → PlayerInventory에 저장 완료");

        selectedItem = null;
        selectedItemObject = null;

        ShowMessage("구매 성공! 창고에 보관 완료!", Color.green);

        RefreshUI();
    }

    void ShowMessage(string msg, Color color)
    {
        messageText.text = msg;
        messageText.color = color;
        messagePanel.SetActive(true);

        CancelInvoke(nameof(HideMessage));
        Invoke(nameof(HideMessage), 3f);
    }

    void HideMessage()
    {
        messagePanel.SetActive(false);
    }
}
