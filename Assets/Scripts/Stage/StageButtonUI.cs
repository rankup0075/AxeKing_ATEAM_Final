using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageButtonUI : MonoBehaviour
{
    public Image stageImage;
    public TextMeshProUGUI stageNameText;
    public Button button;
    public GameObject lockOverlay; // Lock UI


    private StageData stageData;

    public void Setup(StageData data)
    {
        stageData = data;

        // 스테이지 이름 표시
        if (stageNameText != null)
            stageNameText.text = stageData.stageName;

        // 썸네일 표시
        if (stageImage != null && stageData.thumbnail != null)
            stageImage.sprite = stageData.thumbnail;

        // 잠금 여부 반영
        if (lockOverlay != null)
            lockOverlay.SetActive(!stageData.isUnlocked);
        button.interactable = stageData.isUnlocked;

        // 클릭 시 동작 연결
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnStageSelected);


    }

    private void OnStageSelected()
    {
        Debug.Log($"스테이지 선택됨: {stageData.stageName}");
        // UIManager나 StageManager 통해서 실제 스테이지 로드로 연결

        if (stageData.isUnlocked)
        {
            // StageManager를 통해 씬 로드
            StageManager.Instance.EnterStage(stageData.stageId);
        }
        else
        {
            Debug.LogWarning($"[StageButtonUI] {stageData.stageName} 은 잠겨있음");
        }
    }

    //void UpdateUI()
    //{
    //    stageNameText.text = stageData.stageName;

    //    // 잠금 여부에 따라 오버레이 표시
    //    if (lockOverlay != null)
    //        lockOverlay.SetActive(!stageData.isUnlocked);

    //    // 버튼 활성화/비활성화
    //    GetComponent<Button>().interactable = stageData.isUnlocked;
    //}
}
