using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    public List<QuestData> allQuests = new List<QuestData>();

    void Start()
    {
        // 이미 인스펙터에서 설정된 퀘스트가 있다면 그대로 사용
        if (allQuests != null && allQuests.Count > 0)
        {
            Debug.Log($"[QuestManager] 인스펙터에서 지정된 퀘스트 {allQuests.Count}개 유지됨");
            return;
        }

        // [선택적] Resources에서 자동 로드 (없을 때만)
        //var loaded = Resources.LoadAll<QuestData>("QuestData");
        //allQuests = new List<QuestData>(loaded);


        // 인스펙터에 아무것도 없을 때만 예비 리스트 생성
        allQuests = new List<QuestData>();
        Debug.Log("[QuestManager] 빈 퀘스트 리스트 생성 (인스펙터 비어 있음)");
    }


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);   // [추가] 씬 이동 시 유지
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public List<QuestData> GetAllQuests()
    {
        return allQuests;
    }

    // 선행퀘스트 확인
    public bool IsQuestCompleted(string questId)
    {
        var quest = allQuests.Find(q => q.questId == questId);
        return quest != null && quest.isCompleted;
    }

    public void AcceptQuest(string questId)
    {
        var quest = allQuests.Find(q => q.questId == questId);
        if (quest != null)
        {
            quest.isAccepted = true;
            Debug.Log($"퀘스트 수락: {quest.questName}");
            UpdateQuestProgress(); // 수락 시 바로 진행도 확인
        }
    }

    // 인벤토리 기반으로 진행도 업데이트
    public void UpdateQuestProgress()
    {
        var playerInv = FindFirstObjectByType<PlayerInventory>();
        if (playerInv == null) return;

        foreach (var quest in allQuests)
        {
            if (quest.isAccepted && !quest.isCompleted)
            {
                // 인벤토리에서 개수를 직접 확인
                quest.currentProgress = playerInv.GetItemCount(quest.requiredItemName);
            }
        }
    }

    public void CompleteQuest(string questId)
    {
        var quest = allQuests.Find(q => q.questId == questId);
        if (quest != null && quest.isAccepted && !quest.isCompleted)
        {
            var playerInv = FindFirstObjectByType<PlayerInventory>();
            if (playerInv != null &&
                playerInv.GetItemCount(quest.requiredItemName) >= quest.targetProgress)
            {
                // 요구 아이템 반납
                playerInv.RemoveItem(quest.requiredItemName, quest.targetProgress);

                // 상태 업데이트
                quest.isAccepted = false;
                quest.isCompleted = true;

                // 보상 지급 (GameManager 통해서)
                GameManager.Instance.AddGold(quest.rewardGold);

                Debug.Log($"퀘스트 완료: {quest.questName} (보상 {quest.rewardGold:N0} 골드)");

                // 퀘스트보드 UI 갱신 (골드 텍스트 포함)
                if (QuestBoardUI.Instance != null)
                    QuestBoardUI.Instance.RefreshUI();
            }
            else
            {
                Debug.LogWarning($"[Quest] {quest.questName} 완료 실패 - 아이템 부족");
            }
        }
    }

    // ===========================
    // [추가] SaveLoadManager 연동용 메서드
    // ===========================

    public List<string> ExportActiveQuestIds() =>
        allQuests.Where(q => q.isAccepted && !q.isCompleted)
                 .Select(q => q.questId).ToList();

    public List<string> ExportCompletedQuestIds() =>
        allQuests.Where(q => q.isCompleted)
                 .Select(q => q.questId).ToList();

    public void ImportQuests(IEnumerable<string> active, IEnumerable<string> completed)
    {
        foreach (var q in allQuests)
        {
            q.isAccepted = false;
            q.isCompleted = false;
        }

        foreach (var id in active)
        {
            var quest = allQuests.Find(q => q.questId == id);
            if (quest != null) quest.isAccepted = true;
        }

        foreach (var id in completed)
        {
            var quest = allQuests.Find(q => q.questId == id);
            if (quest != null) quest.isCompleted = true;
        }

        UpdateQuestProgress();
    }

}
