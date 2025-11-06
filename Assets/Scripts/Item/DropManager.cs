using UnityEngine;
using System.Collections.Generic;

public class DropManager : MonoBehaviour
{
    public static DropManager Instance;

    [Header("골드 프리팹")]
    public GameObject goldPrefab;

    [Header("기본 재료 프리팹 (없을 때 대체용)")]
    public GameObject defaultMaterialPrefab;

    [Header("착지 반짝임 이펙트 (선택)")]
    public GameObject coinLandingEffectPrefab;

    // 내부 키 → 리소스 프리팹 이름
    private readonly Dictionary<int, string[]> regionMaterialNames = new Dictionary<int, string[]>
    {
        { 1, new [] { "goblin" } },
        { 2, new [] { "GOLEMLP" } },
        { 3, new [] { "flaming_orb" } },
        { 4, new [] { "MC_10009" } },
        { 5, new [] { "Cross_LP" } },
    };

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 이번 처치에서의 드랍을 "한 번에" 생성.
    /// goldAmount > 0 이면 골드 1개(수량=goldAmount) 생성.
    /// materialCount 만큼 재료 생성.
    /// </summary>
    public void SpawnDrops(Vector3 position, int regionId, int goldAmount = 0, int materialCount = 1)
    {
        // 지면에 맞춰 살짝 띄워서 스폰 기준점 구하기
        position = GetGroundedPosition(position);

        // === 골드 ===
        if (goldAmount > 0 && goldPrefab != null)
        {
            Vector3 goldPos = position + new Vector3(Random.Range(-0.3f, 0.3f), 0.15f, Random.Range(-0.3f, 0.3f));
            GameObject gold = Instantiate(goldPrefab, goldPos, Quaternion.identity);

            // 안전하게 DropItem 새로 셋팅(혹시 프리팹에 Amount=1 붙어있던 거 덮어쓰기)
            ForceSetupDropData(gold, ConvertToKoreanName("gold"), goldAmount);

            // 물리/트리거 세팅
            SetupDropPhysicsAndPickup(gold, 0.1f);

            PlayLandingEffect(goldPos);
            Debug.Log($"[DropManager] 골드 드랍: {goldAmount}개");
        }

        // === 재료 ===
        if (materialCount > 0 && regionMaterialNames.TryGetValue(regionId, out var names))
        {
            for (int i = 0; i < materialCount; i++)
            {
                string key = names[Random.Range(0, names.Length)];
                GameObject prefab = Resources.Load<GameObject>($"ItemIcon/Drop/{key}");
                if (prefab == null)
                {
                    Debug.LogWarning($"[DropManager] '{key}' 프리팹을 못찾음: Resources/ItemIcon/Drop/{key}");
                    prefab = defaultMaterialPrefab;
                }

                if (prefab != null)
                {
                    Vector3 matPos = position + new Vector3(Random.Range(-0.2f, 0.2f), 0.15f, Random.Range(-0.2f, 0.2f));
                    GameObject drop = Instantiate(prefab, matPos, prefab.transform.rotation);

                    // 안전하게 이름/수량 세팅
                    ForceSetupDropData(drop, ConvertToKoreanName(key), 1);

                    SetupDropPhysicsAndPickup(drop, 0.2f);
                    PlayLandingEffect(matPos);
                }
            }
        }
    }

    // 지면 Raycast로 위치 보정
    private Vector3 GetGroundedPosition(Vector3 basePos)
    {
        Vector3 origin = basePos + Vector3.up * 3f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 10f))
            return hit.point + Vector3.up * 0.1f;
        return basePos + Vector3.up * 0.5f;
    }

    /// <summary>
    /// 프리팹에 붙어 있을 수 있는 기존 DropItem을 무시하고,
    /// 반드시 우리가 원하는 이름/수량으로 덮어쓴다.
    /// </summary>
    private void ForceSetupDropData(GameObject obj, string itemName, int amount)
    {
        // 기존 DropItem이 있으면 그대로 쓰되 값만 강제 세팅
        var drop = obj.GetComponent<DropItem>();
        if (drop == null) drop = obj.AddComponent<DropItem>();

        drop.ItemName = itemName;
        drop.Amount = amount;

        // 레이어 설정(선택)
        obj.layer = LayerMask.NameToLayer("DroppedItem");
    }

    /// <summary>
    /// 드랍된 오브젝트에 물리(충돌) + 줍기(트리거) 세팅을 자동으로 붙여준다.
    /// - 루트: 비-트리거 콜라이더(지면과 충돌), Rigidbody(useGravity 지연 켜기)
    /// - 자식: SphereCollider(isTrigger=true) → 플레이어가 근접하면 주움
    /// </summary>
    private void SetupDropPhysicsAndPickup(GameObject obj, float mass)
    {
        // 1) 루트 콜라이더(비-트리거) 보장
        Collider rootCol = obj.GetComponent<Collider>();
        if (rootCol == null)
        {
            var mr = obj.GetComponentInChildren<MeshRenderer>();
            if (mr != null)
            {
                var box = obj.AddComponent<BoxCollider>();
                Vector3 size = mr.bounds.size;
                Vector3 center = mr.bounds.center - obj.transform.position;
                box.size = size;
                box.center = center;
                rootCol = box;
            }
            else
            {
                var box = obj.AddComponent<BoxCollider>();
                box.size = new Vector3(0.3f, 0.3f, 0.3f);
                box.center = new Vector3(0, 0.15f, 0);
                rootCol = box;
            }
        }
        rootCol.isTrigger = false;

        // 2) Rigidbody (중력 지연 켜기)
        var rb = obj.GetComponent<Rigidbody>();
        if (rb == null) rb = obj.AddComponent<Rigidbody>();
        rb.mass = mass;
        rb.useGravity = false;
        rb.linearDamping = 2f;
        rb.angularDamping = 1f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // 관통 방지: 약간 올려놓고, 잠깐 후 중력 켜기
        obj.transform.position += Vector3.up * 0.05f;
        if (obj.GetComponent<EnableGravityAfterDelay>() == null)
            obj.AddComponent<EnableGravityAfterDelay>();

        // 3) 줍기용 트리거(자식)
        bool hasPickupTrigger = false;
        foreach (var c in obj.GetComponentsInChildren<Collider>())
            if (c.isTrigger) { hasPickupTrigger = true; break; }

        if (!hasPickupTrigger)
        {
            var triggerGO = new GameObject("PickupTrigger");
            triggerGO.transform.SetParent(obj.transform, false);
            triggerGO.transform.localPosition = Vector3.zero;

            var sphere = triggerGO.AddComponent<SphereCollider>();
            sphere.isTrigger = true;

            // 특정 프리팹은 너무 커서 반경 조절
            if (obj.name.Contains("MC_10009"))
                sphere.radius = 0.12f; // 너무 작아 주워지지 않던 문제 방지
            else
                sphere.radius = 0.6f;
        }
    }

    private void PlayLandingEffect(Vector3 pos)
    {
        if (coinLandingEffectPrefab == null) return;
        var fx = Instantiate(coinLandingEffectPrefab, pos, Quaternion.identity);
        Destroy(fx, 1.5f);
    }

    private string ConvertToKoreanName(string key)
    {
        switch (key)
        {
            case "gold": return "골드";
            case "goblin": return "고블린의 가죽";
            case "GOLEMLP": return "골렘의 파편";
            case "golem": return "골렘의 파편";
            case "flaming_orb": return "화염 구슬";
            case "red_ice_crystals": return "눈물 조각";
            case "MC_10009": return "눈물 조각";
            case "Cross_LP": return "십자가";
            default: return key;
        }
    }
}
