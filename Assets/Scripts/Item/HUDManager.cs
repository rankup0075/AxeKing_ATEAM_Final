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
        if (uiSlots.TryGetValue(itemName, out var ui))
            ui.UpdateCount(count);
    }
}
