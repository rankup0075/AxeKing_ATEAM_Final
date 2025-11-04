// SaveLoadManager.cs
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance;

    // [추가] 슬롯 관리
    public int currentSlot = -1;  // -1 = 자동저장
    string AutoPath => Path.Combine(Application.persistentDataPath, "save_auto.json");      // [추가]
    string SlotPath(int slot) => Path.Combine(Application.persistentDataPath, $"save_slot{slot}.json"); // [추가]

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ========================
    // 저장 계열
    // ========================
    public void AutoSave()
    {
        var data = CollectCurrentGameData(); // [수정] 공통 함수 재사용
        File.WriteAllText(AutoPath, JsonUtility.ToJson(data, true));
        Debug.Log($"[SaveLoad] 자동저장 완료 → {AutoPath}");
    }

    public void SaveGame()  // [추가] 슬롯 저장
    {
        if (currentSlot < 0) { AutoSave(); return; }
        var data = CollectCurrentGameData();
        var path = SlotPath(currentSlot);
        File.WriteAllText(path, JsonUtility.ToJson(data, true));
        Debug.Log($"[SaveLoad] 슬롯 {currentSlot} 저장 완료 → {path}");
    }

    public void ResetSlot(int slot) // [추가] 슬롯 리셋
    {
        var p = slot < 0 ? AutoPath : SlotPath(slot);
        if (File.Exists(p)) File.Delete(p);
        Debug.Log($"[SaveLoad] {(slot < 0 ? "자동" : $"슬롯 {slot}")} 파일 삭제");
    }

    SaveData CollectCurrentGameData()
    {
        var data = new SaveData();
        var gm = GameManager.Instance;
        data.player.gold = gm.Gold;

        // === HP 저장 ===
        var pc = PlayerController.Instance ?? FindFirstObjectByType<PlayerController>();
        var hp = pc ? pc.GetComponent<PlayerHealth>() : FindFirstObjectByType<PlayerHealth>();
        if (hp != null)
        {
            data.player.currentHP = hp.currentHealth;
            data.player.maxHP = hp.maxHealth;
        }

        // === 인벤토리 / 장비 ===
        var inv = FindFirstObjectByType<PlayerInventory>();
        if (inv != null)
        {
            data.player.smallPotions = inv.smallPotions;
            data.player.mediumPotions = inv.mediumPotions;
            data.player.largePotions = inv.largePotions;

            data.player.weapons.Clear();
            foreach (var w in inv.weaponStorage)
                data.player.weapons.Add(new EquipmentSaveData
                {
                    itemName = w.EquipmentitemName,
                    type = ShopUI.ItemType.Weapon,
                    statBonus = w.EquipmentstatBonus,
                    iconName = w.icon ? w.icon.name : null
                });

            data.player.armors.Clear();
            foreach (var a in inv.armorStorage)
                data.player.armors.Add(new EquipmentSaveData
                {
                    itemName = a.EquipmentitemName,
                    type = ShopUI.ItemType.Armor,
                    statBonus = a.EquipmentstatBonus,
                    iconName = a.icon ? a.icon.name : null
                });

            data.player.items.Clear();
            foreach (var kvp in inv.items)
            {
                data.player.items.Add(new ItemEntry
                {
                    itemName = kvp.Key,
                    amount = kvp.Value
                });
            }

            if (inv.currentWeapon != null)
                data.player.equippedWeapon = new EquipmentSaveData
                {
                    itemName = inv.currentWeapon.EquipmentitemName,
                    type = ShopUI.ItemType.Weapon,
                    statBonus = inv.currentWeapon.EquipmentstatBonus,
                    iconName = inv.currentWeapon.icon ? inv.currentWeapon.icon.name : null
                };
            if (inv.currentArmor != null)
                data.player.equippedArmor = new EquipmentSaveData
                {
                    itemName = inv.currentArmor.EquipmentitemName,
                    type = ShopUI.ItemType.Armor,
                    statBonus = inv.currentArmor.EquipmentstatBonus,
                    iconName = inv.currentArmor.icon ? inv.currentArmor.icon.name : null
                };
        }

        // === 스테이지 / 영지 ===
        var sm = StageManager.Instance ?? FindFirstObjectByType<StageManager>();
        if (sm != null)
        {
            data.regions.Clear();
            foreach (var region in sm.regions)
            {
                var r = new RegionSaveData { regionId = region.regionId, isUnlocked = region.isUnlocked };
                foreach (var s in region.stages)
                    r.stages.Add(new StageSaveData
                    {
                        stageId = s.stageId,
                        isUnlocked = s.isUnlocked,
                        isCompleted = s.isCompleted
                    });
                data.regions.Add(r);
            }
        }

        // === 퀘스트 ===
        var qm = QuestManager.Instance ?? FindFirstObjectByType<QuestManager>();
        data.quests.Clear();
        if (qm != null)
        {
            foreach (var id in qm.ExportCompletedQuestIds())
                data.quests.Add(new QuestEntry { questId = id, isCompleted = true });
            foreach (var id in qm.ExportActiveQuestIds())
                if (!data.quests.Any(e => e.questId == id))
                    data.quests.Add(new QuestEntry { questId = id, isCompleted = false });
        }

        return data;
    }

    // ========================
    // 로드 계열
    // ========================
    public void LoadFromFile(string path) // [추가] 진입점 통합
    {
        if (!File.Exists(path)) { Debug.LogWarning($"[SaveLoad] 파일 없음: {path}"); return; }
        var json = File.ReadAllText(path);
        var data = JsonUtility.FromJson<SaveData>(json);
        StartCoroutine(LoadWhenReady_Co(data));
    }

    public void LoadAutoSave()
    {
        // 자동저장 파일을 강제로 로드
        currentSlot = -1;
        LoadGame();
    }

    public void LoadGame() // [수정] currentSlot 기준 자동/슬롯 로드
    {
        var path = currentSlot < 0 ? AutoPath : SlotPath(currentSlot);
        LoadFromFile(path);
    }

    IEnumerator LoadWhenReady_Co(SaveData data)
    {
        // [수정] 필요한 싱글톤 모두 대기
        yield return new WaitUntil(() =>
            GameManager.Instance != null &&
            PlayerController.Instance != null &&
            FindFirstObjectByType<PlayerInventory>() != null &&
            StageManager.Instance != null &&
            QuestManager.Instance != null &&
            UIManager.Instance != null &&
            FindFirstObjectByType<PlayerVisual>() != null      // [추가]
        );

        ApplySaveData(data);
        Debug.Log("[SaveLoad] 로드 완료");
    }

    void ApplySaveData(SaveData data)
    {
        var gm = GameManager.Instance;
        var inv = FindFirstObjectByType<PlayerInventory>();
        var ui = UIManager.Instance;
        var pc = PlayerController.Instance ?? FindFirstObjectByType<PlayerController>();
        var hp = pc ? pc.GetComponent<PlayerHealth>() : FindFirstObjectByType<PlayerHealth>();
        var visual = FindFirstObjectByType<PlayerVisual>();

        // --- 골드 ---
        gm.SetGold(data.player.gold);
        ui.UpdateHUDGold(data.player.gold);

        // --- HP ---
        if (hp != null)
        {
            hp.maxHealth = data.player.maxHP;
            hp.currentHealth = Mathf.Clamp(data.player.currentHP, 0, hp.maxHealth);
            ui.UpdateHealthBar(hp.currentHealth, hp.maxHealth);
            ui.UpdateHUDHealth(hp.currentHealth, hp.maxHealth);
        }

        // --- 포션, 창고 ---
        if (inv != null)
        {
            inv.smallPotions = data.player.smallPotions;
            inv.mediumPotions = data.player.mediumPotions;
            inv.largePotions = data.player.largePotions;
            ui.UpdateHUDPotions(inv.smallPotions, inv.mediumPotions, inv.largePotions);

            inv.weaponStorage.Clear();
            foreach (var w in data.player.weapons)
                inv.weaponStorage.Add(new ItemEquipment
                {
                    EquipmentitemName = w.itemName,
                    Equipmenttype = ShopUI.ItemType.Weapon,
                    EquipmentstatBonus = w.statBonus,
                    icon = string.IsNullOrEmpty(w.iconName) ? null : Resources.Load<Sprite>($"ItemIcon/Weapon/{w.iconName}")
                });

            inv.armorStorage.Clear();
            foreach (var a in data.player.armors)
                inv.armorStorage.Add(new ItemEquipment
                {
                    EquipmentitemName = a.itemName,
                    Equipmenttype = ShopUI.ItemType.Armor,
                    EquipmentstatBonus = a.statBonus,
                    icon = string.IsNullOrEmpty(a.iconName) ? null : Resources.Load<Sprite>($"ItemIcon/Armor/{a.iconName}")
                });

            inv.items.Clear();
            foreach (var item in data.player.items)
                {
                    inv.items[item.itemName] = item.amount;
                }

            QuestManager.Instance?.UpdateQuestProgress();
        }

        // --- 장착 복구 및 비주얼 반영 ---
        if (inv != null)
        {
            if (data.player.equippedWeapon != null && !string.IsNullOrEmpty(data.player.equippedWeapon.itemName))
            {
                var w = data.player.equippedWeapon;
                var eq = new ItemEquipment
                {
                    EquipmentitemName = w.itemName,
                    Equipmenttype = ShopUI.ItemType.Weapon,
                    EquipmentstatBonus = w.statBonus,
                    icon = string.IsNullOrEmpty(w.iconName) ? null : Resources.Load<Sprite>($"ItemIcon/Weapon/{w.iconName}")
                };
                inv.EquipItem(eq, ShopUI.ItemType.Weapon);
                visual?.ApplyWeapon(eq.EquipmentitemName);

                var player = PlayerController.Instance ?? FindFirstObjectByType<PlayerController>();
                var health = player ? player.GetComponent<PlayerHealth>() : FindFirstObjectByType<PlayerHealth>();
                eq.ApplyStats(player, health);
            }

            if (data.player.equippedArmor != null && !string.IsNullOrEmpty(data.player.equippedArmor.itemName))
            {
                var a = data.player.equippedArmor;
                var eq = new ItemEquipment
                {
                    EquipmentitemName = a.itemName,
                    Equipmenttype = ShopUI.ItemType.Armor,
                    EquipmentstatBonus = a.statBonus,
                    icon = string.IsNullOrEmpty(a.iconName) ? null : Resources.Load<Sprite>($"ItemIcon/Armor/{a.iconName}")
                };
                inv.EquipItem(eq, ShopUI.ItemType.Armor);
                visual?.ApplyArmor(eq.EquipmentitemName);
            }

            // [추가] 비주얼 재적용
            StartCoroutine(ReapplyVisualsNextFrame(data));
        }

        // --- 스테이지 / 영지 ---
        var sm = StageManager.Instance;
        if (sm != null && data.regions != null)
        {
            foreach (var rr in data.regions)
            {
                var region = sm.regions.FirstOrDefault(x => x.regionId == rr.regionId);
                if (region == null) continue;
                region.isUnlocked = rr.isUnlocked;
                foreach (var ss in rr.stages)
                {
                    var stage = region.stages.FirstOrDefault(x => x.stageId == ss.stageId);
                    if (stage == null) continue;
                    stage.isUnlocked = ss.isUnlocked;
                    stage.isCompleted = ss.isCompleted;
                }

                // [추가] 스테이지 해금 보정
                for (int i = 0; i < region.stages.Count - 1; i++)
                {
                    if (region.stages[i].isCompleted)
                        region.stages[i + 1].isUnlocked = true;
                }
                if (region.stages.Any(s => s.isCompleted))
                    region.isUnlocked = true;
            }
            sm.RefreshStageList();
        }

        // --- 퀘스트 ---
        var qm = QuestManager.Instance;
        if (qm != null)
        {
            var active = data.quests.Where(q => !q.isCompleted).Select(q => q.questId).ToList();
            var completed = data.quests.Where(q => q.isCompleted).Select(q => q.questId).ToList();
            qm.ImportQuests(active, completed);
            qm.UpdateQuestProgress();
            QuestBoardUI.Instance?.RefreshUI();
        }

        inv?.SendMessage("RefreshUI", SendMessageOptions.DontRequireReceiver);
    }

    // --- 비주얼 재적용 ---
    IEnumerator ReapplyVisualsNextFrame(SaveData data)
    {
        yield return null;
        var visual = FindFirstObjectByType<PlayerVisual>();
        if (visual == null) yield break;

        if (data.player.equippedWeapon != null && !string.IsNullOrEmpty(data.player.equippedWeapon.itemName))
            visual.ApplyWeapon(data.player.equippedWeapon.itemName);
        if (data.player.equippedArmor != null && !string.IsNullOrEmpty(data.player.equippedArmor.itemName))
            visual.ApplyArmor(data.player.equippedArmor.itemName);
    }
}
