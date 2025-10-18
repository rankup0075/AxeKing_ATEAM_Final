using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class QuestBoardUI : MonoBehaviour
{
    public static QuestBoardUI Instance;

    public Transform questListContainer;
    public GameObject questSlotPrefab;
    public TextMeshProUGUI emptyText;
    public ScrollRect scrollRect;

    [Header("Player Info")]
    public TextMeshProUGUI currentGoldText;

    void Awake() { Instance = this; }

    void OnEnable()
    {
        // 의존성 준비될 때까지 지연 갱신
        StartCoroutine(SafeRefreshCoroutine());
    }

    IEnumerator SafeRefreshCoroutine()
    {
        // QuestManager와 컨테이너가 준비될 때까지 대기
        yield return new WaitUntil(() =>
            questListContainer != null &&
            questSlotPrefab != null &&
            QuestManager.Instance != null);

        RefreshUI();

        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
            Canvas.ForceUpdateCanvases();
        }
    }

    public void RefreshUI()
    {
        if (questListContainer == null || questSlotPrefab == null || QuestManager.Instance == null)
            return;

        foreach (Transform child in questListContainer)
            Destroy(child.gameObject);

        var quests = QuestManager.Instance.GetAllQuests();
        bool hasQuests = false;

        if (quests != null)
        {
            foreach (var q in quests)
            {
                var slot = Instantiate(questSlotPrefab, questListContainer);
                var ui = slot.GetComponent<QuestSlotUI>();
                if (ui != null) ui.Setup(q);
                hasQuests = true;
            }
        }

        if (emptyText != null) emptyText.gameObject.SetActive(!hasQuests);

        var gm = GameManager.Instance;
        if (currentGoldText != null && gm != null)
            currentGoldText.text = $"현재 골드: {gm.gold:N0}G"; // 필드명에 맞춤
    }

    public void Close() { gameObject.SetActive(false); }
}
