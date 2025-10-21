using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RegionButtonUI : MonoBehaviour
{
    public Image regionImage;            // 썸네일 (선택사항)
    public TextMeshProUGUI regionNameText;
    public GameObject lockOverlay;       // 잠금 표시
    public Button button;

    private RegionData regionData;

    public void Setup(RegionData region)
    {
        regionData = region;

        if (regionNameText != null)
            regionNameText.text = region.regionName;

        // 썸네일 있으면 표시
        if (regionImage != null && region.thumbnail != null)
            regionImage.sprite = region.thumbnail;

        // 잠금 처리
        bool unlocked = region.isUnlocked;
        if (lockOverlay != null) lockOverlay.SetActive(!unlocked);
        if (button != null) button.interactable = unlocked;

        if (regionImage != null)
        {
            var color = regionImage.color;
            color.a = unlocked ? 1f : 1f; // 잠기면 40% 투명
            regionImage.color = color;
        }

    }

    public void AddClickListener(UnityEngine.Events.UnityAction action)
{
    if (button != null)
    {
        button.onClick.RemoveListener(action); // 중복 방지
        button.onClick.AddListener(action);
    }
    else
    {
        Debug.LogWarning($"{name} 버튼 참조가 연결되지 않았습니다.");
    }
}
}
