using UnityEngine;

public class DropItem : MonoBehaviour
{
    [Tooltip("인벤토리에 들어갈 아이템 이름")]
    public string ItemName = "골드";

    [Tooltip("개수/양")]
    public int Amount = 1;
}
