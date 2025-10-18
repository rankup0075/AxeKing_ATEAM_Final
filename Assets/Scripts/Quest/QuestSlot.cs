using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class QuestSlotUI : MonoBehaviour
{
    public TextMeshProUGUI questNameText;
    public TextMeshProUGUI questDescText;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI rewardText;
    public Button actionButton;
    public Image slotBackground;   //슬롯 전체 배경 (Inspector에서 연결)

    private QuestData questData;
    private Color normalColor;

    void Awake()
    {
        if (slotBackground != null)
            normalColor = slotBackground.color;
    }

    public void Setup(QuestData quest)
    {
        questData = quest;
        questNameText.text = quest.questName;
        questDescText.text = quest.description;
        rewardText.text = $"보상: {quest.rewardGold:N0} Gold";

        actionButton.onClick.RemoveAllListeners();

        if (quest.isCompleted)
        {
            actionButton.gameObject.SetActive(false);
            progressText.gameObject.SetActive(true);
            progressText.text = $"완료됨 {quest.targetProgress}/{quest.targetProgress}";

            if (slotBackground != null)
                slotBackground.color = Color.gray;
        }
        else if (quest.isAccepted)
        {
            progressText.gameObject.SetActive(true);
            progressText.text = $"진행중 : {quest.currentProgress}/{quest.targetProgress}";

            if (quest.currentProgress >= quest.targetProgress)
            {
                actionButton.gameObject.SetActive(true);
                actionButton.interactable = true;
                actionButton.GetComponentInChildren<TextMeshProUGUI>().text = "완료하기";
                actionButton.onClick.AddListener(() => OnComplete());
            }
            else
            {
                actionButton.gameObject.SetActive(true);
                actionButton.interactable = false;
                actionButton.GetComponentInChildren<TextMeshProUGUI>().text = "완료하기";
            }

            if (slotBackground != null)
                slotBackground.color = normalColor;
        }
        else
        {
            progressText.gameObject.SetActive(false);
            actionButton.gameObject.SetActive(true);

            bool canAccept = string.IsNullOrEmpty(quest.prerequisiteQuestId)
                             || QuestManager.Instance.IsQuestCompleted(quest.prerequisiteQuestId);

            actionButton.interactable = canAccept;
            actionButton.GetComponentInChildren<TextMeshProUGUI>().text = "수락하기";
            actionButton.onClick.AddListener(() => OnAccept());

            if (slotBackground != null)
                slotBackground.color = canAccept ? normalColor : Color.gray; //조건 안 되면 회색
        }
    }

    private void OnAccept()
    {
        QuestManager.Instance.AcceptQuest(questData.questId);
        Setup(questData);
    }

    private void OnComplete()
    {
        QuestManager.Instance.CompleteQuest(questData.questId);

        // 슬롯 비활성화 처리
        if (slotBackground != null)
            slotBackground.color = Color.gray;

        actionButton.gameObject.SetActive(false);
        progressText.text = $"완료됨 {questData.targetProgress}/{questData.targetProgress}";

        // 보드 전체 새로고침 (다음 퀘스트 활성화 반영)
        QuestBoardUI.Instance.RefreshUI();
    }

}
