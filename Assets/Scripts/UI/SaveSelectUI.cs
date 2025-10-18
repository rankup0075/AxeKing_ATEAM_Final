using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections;

[System.Serializable]
public class SlotInfoUI
{
    public Button slotButton;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI itemText;
    public TextMeshProUGUI questText;
    public TextMeshProUGUI stageText;
}

public class SaveSelectUI : MonoBehaviour
{
    [Header("Refs")]
    public GameObject saveSelectPanel;      // = this.gameObject 또는 부모 패널
    public GameObject saveSlotContainer;    // SlotContainer
    public GameObject confirmPanel;         // ConfirmPanel
    public TextMeshProUGUI confirmText;
    public Button yesButton;
    public Button noButton;
    public Button closeButton;              // 상단 닫기

    [Header("Slots")]
    public SlotInfoUI[] slotUIs;            // 3칸

    bool saveMode = false;                  // false=로드, true=저장
    int pendingSlot = -1;

    void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(CloseSelf);
        gameObject.SetActive(false);
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 확인창이 열려 있다면 뒤로 돌아가기
            if (confirmPanel.activeSelf)
            {
                confirmPanel.SetActive(false);
                saveSlotContainer.SetActive(true);
                return;
            }

            // 기본: UI 닫기
            CloseSelf();
        }
    }

    public void Setup(bool isSaveMode)
    {
        saveMode = isSaveMode;
        pendingSlot = -1;

        saveSlotContainer.SetActive(true);
        confirmPanel.SetActive(false);
        gameObject.SetActive(true);

        // 인게임에서 열면 일시정지
        if (saveMode) { Time.timeScale = 0f; LockPlayer(true); }

        UpdateSlotInfoDisplay();
    }

    public void CloseSelf()
    {
        // 인게임 저장 모드에서 닫힐 때만 해제
        if (saveMode) { Time.timeScale = 1f; LockPlayer(false); }
        gameObject.SetActive(false);
    }

    void LockPlayer(bool freeze)
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null) return;
        var controller = player.GetComponent<PlayerController>();
        var anim = player.GetComponent<Animator>();
        if (controller != null) controller.canControl = !freeze;
        if (anim != null) anim.speed = freeze ? 0f : 1f;
    }

    public void OnSlotClick(int slot)
    {
        if (!saveMode)
        {
            // 로드 모드
            SaveLoadManager.Instance.currentSlot = slot;
            gameObject.SetActive(false);

            MenuManager mm = FindFirstObjectByType<MenuManager>();
            if (mm != null)
            {
                // 자동저장 처리
                if (slot == 0)
                {
                    SaveLoadManager.Instance.currentSlot = -1; // 자동저장 구분용
                    mm.StartCoroutine(mm.LoadSceneAndLoadGame(mm.firstSceneName, true, true)); // 자동저장 전용 호출
                }
                else
                {
                    mm.ContinueFromSlot(slot);
                }
            }
            return;
        }

        // 저장 모드
        pendingSlot = slot;
        string path = Path.Combine(Application.persistentDataPath, $"save_slot{slot}.json");
        bool exists = File.Exists(path);

        confirmText.text = exists
            ? $"슬롯 {slot}의 기존 데이터를 덮어쓰시겠습니까?"
            : $"슬롯 {slot}에 저장하시겠습니까?";

        saveSlotContainer.SetActive(false);
        confirmPanel.SetActive(true);

        yesButton.onClick.RemoveAllListeners();
        noButton.onClick.RemoveAllListeners();
        yesButton.onClick.AddListener(() => ConfirmSave(true));
        noButton.onClick.AddListener(() => ConfirmSave(false));
    }

    void ConfirmSave(bool accepted)
    {
        if (!accepted)
        {
            confirmPanel.SetActive(false);
            saveSlotContainer.SetActive(true);
            return;
        }

        SaveLoadManager.Instance.currentSlot = pendingSlot;
        SaveLoadManager.Instance.SaveGame();

        confirmText.text = "저장 완료!";
        yesButton.gameObject.SetActive(false);
        noButton.gameObject.SetActive(false);
        StartCoroutine(CloseAfterDelay());   // 그대로 호출
    }

    private IEnumerator CloseAfterDelay()
    {
        yield return new WaitForSecondsRealtime(1f); // ← 핵심
        AfterSaved();
    }

    void AfterSaved()
    {
        yesButton.gameObject.SetActive(true);
        noButton.gameObject.SetActive(true);
        confirmPanel.SetActive(false);
        saveSlotContainer.SetActive(true);
        UpdateSlotInfoDisplay();
    }


    void UpdateSlotInfoDisplay()
    {
        for (int i = 0; i < slotUIs.Length; i++)
        {
            string path;
            bool isAuto = (i == 0);

            if (isAuto)
                path = Path.Combine(Application.persistentDataPath, "save_auto.json");
            else
                path = Path.Combine(Application.persistentDataPath, $"save_slot{i}.json"); // 슬롯1~3

            var ui = slotUIs[i];

            if (!File.Exists(path))
            {
                ui.titleText.text = isAuto ? "자동저장 (없음)" : $"슬롯 {i} (빈 슬롯)";
                ui.goldText.text = "";
                ui.itemText.text = "";
                ui.questText.text = "";
                ui.stageText.text = "";
                continue;
            }

            string json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<SaveData>(json);

            int totalWeapons = data.player.weapons.Count;
            int totalArmors = data.player.armors.Count;
            int totalEquipments = totalWeapons + totalArmors;
            int totalQuests = data.quests.Count;
            int completedQuests = data.quests.FindAll(q => q.isCompleted).Count;
            int unlockedStages = 0;
            foreach (var r in data.regions)
                unlockedStages += r.stages.FindAll(s => s.isUnlocked).Count;

            ui.titleText.text = isAuto ? "자동저장" : $"슬롯 {i}";
            ui.goldText.text = $"골드: {data.player.gold}";
            ui.itemText.text = $"장비: {totalEquipments}개 (무기 {totalWeapons}, 방어구 {totalArmors})";
            ui.questText.text = $"퀘스트: {completedQuests}/{totalQuests}";
            ui.stageText.text = $"스테이지: {unlockedStages}개 해금";
        }
    }



    public void OnResetClick(int slot)
    {
        pendingSlot = slot;
        confirmText.text = $"파일 {slot}을 초기화하겠습니까?";
        saveSlotContainer.SetActive(false);
        confirmPanel.SetActive(true);

        yesButton.onClick.RemoveAllListeners();
        noButton.onClick.RemoveAllListeners();
        yesButton.onClick.AddListener(() => ConfirmReset(true));
        noButton.onClick.AddListener(() => ConfirmReset(false));
    }

    void ConfirmReset(bool accepted)
    {
        if (!accepted)
        {
            confirmPanel.SetActive(false);
            saveSlotContainer.SetActive(true);
            return;
        }

        SaveLoadManager.Instance.ResetSlot(pendingSlot);
        confirmText.text = $"파일 {pendingSlot}을 초기화했습니다.";

        yesButton.gameObject.SetActive(false);
        noButton.gameObject.SetActive(false);
        Invoke(nameof(AfterReset), 2.0f);
    }

    void AfterReset()
    {
        yesButton.gameObject.SetActive(true);
        noButton.gameObject.SetActive(true);
        confirmPanel.SetActive(false);
        saveSlotContainer.SetActive(true);
        UpdateSlotInfoDisplay();
    }
}
