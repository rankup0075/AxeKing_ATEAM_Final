using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject shopPanel;
    public Transform itemContainer;
    public GameObject itemPrefab;
    public TextMeshProUGUI goldText;

    [Header("Shop Items")]
    public List<ShopItem> shopItems = new List<ShopItem>();

    [System.Serializable]
    public class ShopItem
    {
        public string itemName;
        public long price;
        public string description;
        public ItemType type;
        public int statBonus;
    }

    public enum ItemType { Weapon, Armor }

    void Start()
    {
        CreateShopItems();
    }

    void CreateShopItems()
    {
        foreach (Transform child in itemContainer)
            Destroy(child.gameObject);

        foreach (var item in shopItems)
        {
            GameObject itemUI = Instantiate(itemPrefab, itemContainer);

            TextMeshProUGUI nameText = itemUI.transform.Find("Name").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI priceText = itemUI.transform.Find("Price").GetComponent<TextMeshProUGUI>();
            Button buyButton = itemUI.transform.Find("BuyButton").GetComponent<Button>();

            nameText.text = item.itemName;
            priceText.text = $"{item.price:N0} G";

            buyButton.onClick.AddListener(() => BuyItem(item));
        }
    }

    void BuyItem(ShopItem item)
    {
        if (GameManager.Instance.Gold >= item.price)
        {
            if (GameManager.Instance.SpendGold(item.price))
            {
                PlayerInventory inventory = GameObject.FindWithTag("Player").GetComponent<PlayerInventory>();
                inventory.AddEquipment(item.itemName, item.type, item.statBonus);
                UIManager.Instance.UpdateGoldDisplay(GameManager.Instance.Gold);
            }
        }
        else
        {
            Debug.Log("∞ÒµÂ ∫Œ¡∑!");
        }
    }

    public void OpenShop()
    {
        shopPanel.SetActive(true);
        UIManager.Instance.UpdateGoldDisplay(GameManager.Instance.Gold);
        Time.timeScale = 0f;
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);
        Time.timeScale = 1f;
    }
}
