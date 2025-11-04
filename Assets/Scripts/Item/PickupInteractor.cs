using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PickupInteractor : MonoBehaviour
{
    private void Reset()
    {
        // 트리거 자동 활성화
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어만 감지
        if (!other.CompareTag("Player")) return;

        // 부모 오브젝트에서 DropItem 컴포넌트 가져오기
        DropItem drop = GetComponentInParent<DropItem>();
        if (drop == null) return;

        // 플레이어 인벤토리 가져오기
        PlayerInventory inventory = other.GetComponent<PlayerInventory>();
        if (inventory == null) return;

        // ✅ 아이템 이름 구분 처리
        if (drop.ItemName == "골드")
        {
            // 골드라면 골드 획득 로직 (원하면 재화 전용 함수로 확장 가능)
            inventory.AddItem("골드", drop.Amount);
            Debug.Log($"[획득] 골드 {drop.Amount}개");
        }
        else
        {
            // 나머지는 재료로 처리
            inventory.AddMaterial(drop.ItemName, drop.Amount);
            Debug.Log($"[획득] {drop.ItemName} x{drop.Amount}");
        }

        // 이펙트나 사운드 추가 가능 (원하면 여기 추가)
        Destroy(drop.gameObject); // 아이템 제거
    }
}
