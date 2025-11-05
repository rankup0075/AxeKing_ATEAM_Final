using UnityEngine;
using TMPro;

public class HUDItemUI : MonoBehaviour
{
    public string itemName;      // 예: "고블린의 가죽"
    public TMP_Text countText;   // 수량 표시 UI

    public void UpdateCount(int amount)
    {
        if (countText != null)
            countText.text = amount.ToString();
    }
}
