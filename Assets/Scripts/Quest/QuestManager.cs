using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    public List<QuestData> allQuests = new List<QuestData>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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

}
