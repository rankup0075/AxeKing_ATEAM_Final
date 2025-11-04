using UnityEngine;

public class PlayerGold : MonoBehaviour
{
    // [삭제] int currentGold = 0;  // GameManager와 중복

    void Start()
    {
        // [수정] GameManager의 실제 골드 표시
        UIManager.Instance.UpdateGoldDisplay(GameManager.Instance.Gold);
    }

    public bool SpendGold(int amount)
    {
        // [수정] GameManager에 위임
        return GameManager.Instance.SpendGold(amount);
    }

    public void EarnGold(int amount)
    {
        // [수정] GameManager에 위임
        GameManager.Instance.AddGold(amount);
    }

    public int GetGold()
    {
        // [수정] GameManager에서 읽음
        return (int)GameManager.Instance.Gold;
    }
}
