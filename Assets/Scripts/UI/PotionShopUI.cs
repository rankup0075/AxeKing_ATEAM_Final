using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PotionShopUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject potionShopPanel;
    public Button closeButton;
    public Button buyButton;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI messageText;
    public GameObject messagePanel;

    [Header("Potion Item Prefab")]
    public GameObject potionItemPrefab;
    public Transform potionListContainer;

    private PlayerInventory playerInventory;
    private GameObject selectedItemObject;
    private Potion selectedPotion;

    public Transform npcTransform;
    private CameraFollow camFollow;
    private GameManager gameManager;

    // 프리팹 UI 오브젝트 캐싱용
    private Dictionary<int, GameObject> potionUIObjects = new Dictionary<int, GameObject>();

    [System.Serializable]
    public class Potion
    {
        public string itemName;
        public long price;
        public int potionType; // 0:소형, 1:중형, 2:대형
        public string description;
        public Sprite icon;
    }

    public List<Potion> shopPotions = new List<Potion>()
    {
        new Potion { itemName="소형 포션", price=500, potionType=0, description="체력 +30", icon=null },
        new Potion { itemName="중형 포션", price=1500, potionType=1, description="체력 +50", icon=null },
        new Potion { itemName="대형 포션", price=3000, potionType=2, description="체력 완전 회복", icon=null },
    };

    void Start()
    {
        camFollow = Camera.main.GetComponent<CameraFollow>();
        playerInventory = GameObject.FindWithTag("Player")?.GetComponent<PlayerInventory>();
        gameManager = GameManager.Instance;

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseShop);

        if (buyButton != null)
            buyButton.onClick.AddListener(BuyPotion);

        if (potionShopPanel != null)
            potionShopPanel.SetActive(false);

        if (messagePanel != null)
            messagePanel.SetActive(false);

        CreatePotionItems();
    }

    void CreatePotionItems()
    {
        potionUIObjects.Clear();

        foreach (var potion in shopPotions)
        {
            GameObject ui = Instantiate(potionItemPrefab, potionListContainer);
            ui.GetComponent<Button>().onClick.AddListener(() => SelectPotion(potion, ui));

            ui.transform.Find("NameText").GetComponent<TextMeshProUGUI>().text = potion.itemName;
            ui.transform.Find("PriceText").GetComponent<TextMeshProUGUI>().text = $"{potion.price:N0}G";
            ui.transform.Find("DescriptionText").GetComponent<TextMeshProUGUI>().text = potion.description;

            if (potion.icon != null)
                ui.transform.Find("PotionIcon").GetComponent<Image>().sprite = potion.icon;

            potionUIObjects[potion.potionType] = ui;
        }

        UpdatePotionItemUI();
    }

    public void UpdatePotionItemUI()
    {
        foreach (var potion in shopPotions)
        {
            if (potionUIObjects.TryGetValue(potion.potionType, out GameObject ui))
            {
                int ownedCount = GetPotionCount(potion.potionType);
                TextMeshProUGUI ownedText = ui.transform.Find("CountText")?.GetComponent<TextMeshProUGUI>();
                if (ownedText != null)
                    ownedText.text = $"보유 : {ownedCount:N0}개";
            }
        }
    }

    int GetPotionCount(int type)
    {
        return type switch
        {
            0 => playerInventory.smallPotions,
            1 => playerInventory.mediumPotions,
            2 => playerInventory.largePotions,
            _ => 0
        };
    }

    void SelectPotion(Potion potion, GameObject ui)
    {
        if (selectedItemObject != null)
            selectedItemObject.GetComponent<Image>().color = Color.white;

        selectedPotion = potion;
        selectedItemObject = ui;
        ui.GetComponent<Image>().color = new Color(1f, 0.0f, 0.0f); // 연한 붉은색
    }

    void BuyPotion()
    {
        if (selectedPotion == null || playerInventory == null) return;

        if (GameManager.Instance.Gold >= selectedPotion.price)
        {
            GameManager.Instance.SpendGold(selectedPotion.price );
            playerInventory.AddPotion(selectedPotion.potionType, 1);

            ShowMessage("구매 성공!", Color.green);
            RefreshUI();
            UpdatePotionItemUI();
        }
        else
        {
            ShowMessage("돈이 부족합니다.", Color.red);
        }
    }

    public void OpenShop()
    {
        potionShopPanel.SetActive(true);
        RefreshUI();
        UpdatePotionItemUI();

        if (camFollow != null && npcTransform != null)
            camFollow.SetTarget(npcTransform, true);

        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var controller = player.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.canMove = false;
                controller.StopImmediately(); // ← 이동 강제 정지
            }
        }
    }

    public void CloseShop()
    {
        potionShopPanel.SetActive(false);

        if (camFollow != null) camFollow.ResetTarget();

        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var controller = player.GetComponent<PlayerController>();
            if (controller != null) controller.canMove = true;
        }

        selectedPotion = null;
        selectedItemObject = null;
    }

    void RefreshUI()
    {
        goldText.text = $"Gold: {GameManager.Instance.Gold:N0} G";
    }

    void ShowMessage(string msg, Color color)
    {
        if (messageText != null)
        {
            messagePanel.SetActive(true);
            messageText.text = msg;
            messageText.color = color;

            CancelInvoke(nameof(HideMessage));
            Invoke(nameof(HideMessage), 2.5f);
        }
    }

    void HideMessage()
    {
        if (messagePanel != null)
            messagePanel.SetActive(false);
    }
}
