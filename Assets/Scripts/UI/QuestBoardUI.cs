//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//using System.Collections.Generic;

//public class QuestBoardUI : MonoBehaviour
//{
//    [Header("UI References")]
//    public GameObject questBoardPanel;
//    public Transform questContainer;
//    public GameObject questSlotPrefab;
//    public Button closeButton;

//    [Header("Quest Slot References")]
//    public TextMeshProUGUI questTitle;
//    public TextMeshProUGUI questDescription;
//    public TextMeshProUGUI questReward;
//    public Button acceptButton;
//    public Button submitButton;

//    private List<GameObject> questSlots = new List<GameObject>();
//    private GameQuestManager questManager;

//    void Start()
//    {
//        questManager = GameQuestManager.Instance;

//        if (closeButton != null)
//            closeButton.onClick.AddListener(CloseQuestBoard);

//        InitializeQuestBoard();
//    }

//    void InitializeQuestBoard()
//    {
//        if (questBoardPanel != null)
//            questBoardPanel.SetActive(false);

//        CreateQuestSlots();
//    }

//    void CreateQuestSlots()
//    {
//        if (questManager == null || questContainer == null || questSlotPrefab == null) return;

//        // 기존 슬롯들 제거
//        foreach (GameObject slot in questSlots)
//        {
//            if (slot != null) Destroy(slot);
//        }
//        questSlots.Clear();

//        // 새 퀘스트 슬롯들 생성
//        foreach (var quest in questManager.quests)
//        {
//            CreateQuestSlot(quest);
//        }
//    }

//    void CreateQuestSlot(GameQuestManager.Quest quest)
//    {
//        GameObject questSlot = Instantiate(questSlotPrefab, questContainer);
//        questSlots.Add(questSlot);

//        // 퀘스트 슬롯 UI 요소들 찾기
//        QuestSlotUI slotUI = questSlot.GetComponent<QuestSlotUI>();
//        if (slotUI == null)
//            slotUI = questSlot.AddComponent<QuestSlotUI>();

//        // 퀘스트 정보 설정
//        slotUI.SetupQuest(quest, this);
//    }

//    public void OpenQuestBoard()
//    {
//        if (questBoardPanel != null)
//        {
//            questBoardPanel.SetActive(true);
//            RefreshQuestDisplay();
//            Time.timeScale = 0f; // 게임 일시정지
//        }
//    }

//    public void CloseQuestBoard()
//    {
//        if (questBoardPanel != null)
//        {
//            questBoardPanel.SetActive(false);
//            Time.timeScale = 1f; // 게임 재개
//        }
//    }

//    public void RefreshQuestDisplay()
//    {
//        CreateQuestSlots();
//    }

//    public void OnQuestAccepted(int questId)
//    {
//        if (questManager != null)
//        {
//            questManager.AcceptQuest(questId);
//            RefreshQuestDisplay();
//        }
//    }

//    public void OnQuestSubmitted(int questId)
//    {
//        if (questManager != null)
//        {
//            questManager.CompleteQuest(questId);
//            RefreshQuestDisplay();
//        }
//    }
//}

//// 개별 퀘스트 슬롯을 위한 별도 클래스
//public class QuestSlotUI : MonoBehaviour
//{
//    [Header("UI Components")]
//    public TextMeshProUGUI questNameText;
//    public TextMeshProUGUI questDescriptionText;
//    public TextMeshProUGUI questRewardText;
//    public TextMeshProUGUI questStatusText;
//    public Button acceptButton;
//    public Button submitButton;

//    private GameQuestManager.Quest currentQuest;
//    private QuestBoardUI questBoardUI;

//    void Start()
//    {
//        FindUIComponents();
//    }

//    void FindUIComponents()
//    {
//        // UI 컴포넌트들 자동 찾기
//        if (questNameText == null)
//            questNameText = transform.Find("QuestName")?.GetComponent<TextMeshProUGUI>();

//        if (questDescriptionText == null)
//            questDescriptionText = transform.Find("QuestDescription")?.GetComponent<TextMeshProUGUI>();

//        if (questRewardText == null)
//            questRewardText = transform.Find("QuestReward")?.GetComponent<TextMeshProUGUI>();

//        if (questStatusText == null)
//            questStatusText = transform.Find("QuestStatus")?.GetComponent<TextMeshProUGUI>();

//        if (acceptButton == null)
//            acceptButton = transform.Find("AcceptButton")?.GetComponent<Button>();

//        if (submitButton == null)
//            submitButton = transform.Find("SubmitButton")?.GetComponent<Button>();
//    }

//    public void SetupQuest(GameQuestManager.Quest quest, QuestBoardUI boardUI)
//    {
//        currentQuest = quest;
//        questBoardUI = boardUI;

//        UpdateQuestDisplay();
//        SetupButtons();
//    }

//    void UpdateQuestDisplay()
//    {
//        if (currentQuest == null) return;

//        // 퀘스트 정보 표시
//        if (questNameText != null)
//            questNameText.text = currentQuest.questName;

//        if (questDescriptionText != null)
//            questDescriptionText.text = $"{currentQuest.requiredItem} {currentQuest.requiredAmount}개 수집";

//        if (questRewardText != null)
//            questRewardText.text = $"보상: {currentQuest.goldReward}골드";

//        // 상태 표시
//        if (questStatusText != null)
//        {
//            switch (currentQuest.status)
//            {
//                case GameQuestManager.QuestStatus.Available:
//                    questStatusText.text = "수락 가능";
//                    questStatusText.color = Color.white;
//                    break;
//                case GameQuestManager.QuestStatus.Accepted:
//                    questStatusText.text = "진행 중";
//                    questStatusText.color = Color.yellow;
//                    break;
//                case GameQuestManager.QuestStatus.Completed:
//                    questStatusText.text = "완료됨";
//                    questStatusText.color = Color.green;
//                    break;
//                case GameQuestManager.QuestStatus.Claimed:
//                    questStatusText.text = "완료";
//                    questStatusText.color = Color.gray;
//                    break;
//            }
//        }
//    }

//    void SetupButtons()
//    {
//        // 수락 버튼 설정
//        if (acceptButton != null)
//        {
//            acceptButton.gameObject.SetActive(currentQuest.status == GameQuestManager.QuestStatus.Available);
//            acceptButton.onClick.RemoveAllListeners();
//            acceptButton.onClick.AddListener(() => OnAcceptClicked());
//        }

//        // 제출 버튼 설정
//        if (submitButton != null)
//        {
//            bool canSubmit = currentQuest.status == GameQuestManager.QuestStatus.Accepted &&
//                           GameQuestManager.Instance.CanCompleteQuest(currentQuest.questId);

//            submitButton.gameObject.SetActive(currentQuest.status == GameQuestManager.QuestStatus.Accepted);
//            submitButton.interactable = canSubmit;
//            submitButton.onClick.RemoveAllListeners();
//            submitButton.onClick.AddListener(() => OnSubmitClicked());
//        }
//    }

//    void OnAcceptClicked()
//    {
//        if (questBoardUI != null)
//        {
//            questBoardUI.OnQuestAccepted(currentQuest.questId);
//        }
//    }

//    void OnSubmitClicked()
//    {
//        if (questBoardUI != null)
//        {
//            questBoardUI.OnQuestSubmitted(currentQuest.questId);
//        }
//    }
//}