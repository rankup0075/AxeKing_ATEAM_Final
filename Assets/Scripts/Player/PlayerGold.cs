using UnityEngine;

public class PlayerGold : MonoBehaviour
{
    public int currentGold = 0;

    void Start()
    {
        UIManager.Instance.UpdateGoldDisplay(currentGold);
    }

    public bool SpendGold(int amount)
    {
        if (currentGold >= amount)
        {
            currentGold -= amount;
            UIManager.Instance.UpdateGoldDisplay(currentGold);
            return true;
        }
        return false;
    }

    public void EarnGold(int amount)
    {
        currentGold += amount;
        UIManager.Instance.UpdateGoldDisplay(currentGold);
    }

    public int GetGold()
    {
        return currentGold;
    }
}
