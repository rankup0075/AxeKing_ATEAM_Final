using UnityEngine;
using System.Collections.Generic;

public class HUDManager : MonoBehaviour
{
    private PlayerInventory inventory;
    private Dictionary<string, HUDItemUI> uiSlots = new Dictionary<string, HUDItemUI>();

    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    void Start()
    {
        // 🔹 인벤토리 자동 탐색
        inventory = FindObjectOfType<PlayerInventory>();
        if (inventory == null)
        {
            Debug.LogWarning("[HUDManager] PlayerInventory를 찾을 수 없음");
            return;
        }

        // 🔹 인벤토리 이벤트 연결
        inventory.OnMaterialChanged += UpdateMaterialUI;

        // 🔹 HUDItemUI 자동 스캔
        HUDItemUI[] foundUIs = GetComponentsInChildren<HUDItemUI>(true);
        foreach (var ui in foundUIs)
        {
            if (!string.IsNullOrEmpty(ui.itemName))
            {
                uiSlots[ui.itemName] = ui;
                Debug.Log($"[HUDManager] '{ui.itemName}' HUD 슬롯 연결됨");
            }
        }

        // 🔹 초기값 반영
        foreach (var kv in uiSlots)
        {
            string name = kv.Key;
            int count = inventory.GetItemCount(name);
            kv.Value.UpdateCount(count);
        }
    }

    private void OnDestroy()
    {
        if (inventory != null)
            inventory.OnMaterialChanged -= UpdateMaterialUI;
    }

    private void UpdateMaterialUI(string itemName, int count)
    {
        // ✅ 영어 키 → 한글 HUD 이름 자동 변환
        string mappedName = ConvertItemKey(itemName);

        if (uiSlots.TryGetValue(mappedName, out var ui))
        {
            ui.UpdateCount(count);
        }
        else
        {
            Debug.LogWarning($"[HUDManager] '{mappedName}'에 해당하는 HUD 슬롯을 찾을 수 없음");
        }
    }

    // ✅ 아이템 키 이름을 HUD에 맞게 자동 변환
    private string ConvertItemKey(string key)
    {
        switch (key)
        {
            case "goblin": return "고블린의 가죽";
            case "golem": return "골렘의 파편";
            case "flaming_orb": return "화염 구슬";
            case "red_ice_crystals": return "눈물 조각";
            case "Cross_LP": return "십자가";
            default: return key;
        }
    }
}
