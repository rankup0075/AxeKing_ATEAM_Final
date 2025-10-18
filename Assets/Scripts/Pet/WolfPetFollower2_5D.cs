// Assets/Scripts/Pet/WolfPetFollower2_5D.cs
using UnityEngine;
using System;
using System.Text;

[RequireComponent(typeof(Rigidbody))]
public class WolfPetFollower2_5D : MonoBehaviour
{
    public enum DomainTerritory
    {
        ForestGate,       // 1영지: 숲의 입구 (고블린)
        StoneGraves,      // 2영지: 돌 무덤 (바위 골렘)
        FireSpiritPlay,   // 3영지: 화염 정령들의 놀이터 (화염 정령)
        FrozenMountain,   // 4영지: 얼어붙은 산 (얼음 정령)
        AncientTemple,    // 5영지: 고대 신전 (신봉자)
        FinalSanctum      // 6영지: 최후의 신전 (산신령)
    }

    [Header("Progress / World State")]
    [Tooltip("현재 영지(도메인)")]
    public DomainTerritory currentDomain = DomainTerritory.ForestGate;

    [Range(1, 3)] public int currentStageIndex = 1;   // 각 영지 3 스테이지
    [Range(1, 3)] public int currentRoundIndex = 1;   // 각 스테이지 3 라운드(3라운드=미니보스)

    [Header("Player Equipment")]
    public string playerCurrentWeapon = "초급 도끼";
    public string playerCurrentArmor = "천 갑옷";

    [Serializable]
    public class DomainGearRule
    {
        public DomainTerritory domain;
        public string bossName;
        public string recommendedWeapon;
        public string recommendedArmor;
        [TextArea(1, 3)] public string reason;
    }

    // ✅ 요청한 장비/이유 세팅(영지별)
    [Header("Gear Rules by Domain")]
    public DomainGearRule[] gearRules =
    {
        new DomainGearRule{
            domain = DomainTerritory.ForestGate,
            bossName = "고블린",
            recommendedWeapon = "돌 도끼",
            recommendedArmor = "돌 갑옷",
            reason = "고블린의 단검 출혈 피해를 단단한 돌 소재로 상쇄."
        },
        new DomainGearRule{
            domain = DomainTerritory.StoneGraves,
            bossName = "바위 골렘",
            recommendedWeapon = "철 도끼",
            recommendedArmor = "철 갑옷",
            reason = "바위 골렘의 높은 방어력을 철 도끼로 관통."
        },
        new DomainGearRule{
            domain = DomainTerritory.FireSpiritPlay,
            bossName = "화염 정령",
            recommendedWeapon = "화염 도끼",
            recommendedArmor = "화염 갑옷",
            reason = "화상 패턴을 화염 저항 장비로 상쇄."
        },
        new DomainGearRule{
            domain = DomainTerritory.FrozenMountain,
            bossName = "얼음 정령",
            recommendedWeapon = "얼음 도끼",
            recommendedArmor = "얼음 갑옷",
            reason = "둔화 패턴을 냉기 저항 장비로 상쇄."
        },
        new DomainGearRule{
            domain = DomainTerritory.AncientTemple,
            bossName = "신봉자(인간형)",
            recommendedWeapon = "신성한 도끼",
            recommendedArmor = "신성한 갑옷",
            reason = "저주 패턴(둔화/도트 피해)을 신성 속성으로 상쇄."
        },
        new DomainGearRule{
            domain = DomainTerritory.FinalSanctum,
            bossName = "산신령(최종보스)",
            recommendedWeapon = "최후의 도끼",
            recommendedArmor = "최후의 갑옷",
            reason = "전천후 대응이 필요한 최종 패턴에 최종급 장비가 필요."
        }
    };

    [Header("Follow Settings")]
    public float followDistance = 1.5f;
    public float walkFollowSpeed = 4f;
    public float runFollowSpeed = 6f;
    public float accel = 10f;
    public float decel = 14f;

    [Header("Animation")]
    public Animator anim;
    public string speedParam = "Speed";

    [Header("Dialogue (AI Only)")]
    public string petName = "Wolf";
    public float talkDistance = 2.0f;
    public KeyCode talkKey = KeyCode.Return;

    // ============================
    // ✅ 허브(마을) 포털/상점 배치 (왼→오)
    //    무기/방어구 → 물약 → 창고 → 퀘스트 → 영지 포탈
    //    실제 씬 좌표에 맞게 인스펙터에서 조정하세요.
    // ============================
    [Header("Hub Layout (Left → Right)")]
    public float hubWeaponArmorShopX = -50f;
    public float hubPotionShopX = -45f;
    public float hubStorageX = -40f;
    public float hubQuestBoardX = -35f;
    public float hubDomainPortalX = -30f;
    [Tooltip("플레이어가 이 범위 안에 있으면 해당 지점에 '있다/바로 옆'으로 판단")]
    public float hubSnapTolerance = 2.0f;

    private Rigidbody rb;
    private float vx;
    private float zLock;

    private PlayerController player;
    private Animator playerAnim;
    private Rigidbody playerRb;
    private bool prevPlayerGrounded = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        if (!anim) anim = GetComponentInChildren<Animator>();
        zLock = transform.position.z;
        TryResolvePlayer();
    }

    private void TryResolvePlayer()
    {
        if (PlayerController.Instance != null)
        {
            player = PlayerController.Instance;
            playerAnim = player.GetComponent<Animator>();
            playerRb = player.GetComponent<Rigidbody>();
            return;
        }

        var pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null)
        {
            player = pObj.GetComponent<PlayerController>();
            playerAnim = pObj.GetComponent<Animator>();
            playerRb = pObj.GetComponent<Rigidbody>();
        }
    }

    private void EnsureDialogueManager()
    {
        if (DialogueManager.Instance == null)
        {
            var go = new GameObject("DialogueManager");
            go.AddComponent<DialogueManager>();
        }
    }

    private void Update()
    {
        if (player == null || playerRb == null || playerAnim == null)
            TryResolvePlayer();

        if (Input.GetKeyDown(talkKey))
        {
            var dm = DialogueManager.Instance;
            if (dm != null && dm.IsOpen) return;

            if (UIManager.Instance != null)
            {
                var cg = UIManager.Instance.settingsPanel_InGame?.GetComponent<CanvasGroup>();
                if (cg != null && cg.alpha > 0.5f) return;
            }

            if (player == null) return;

            float sqrDist = (player.transform.position - transform.position).sqrMagnitude;
            if (sqrDist <= talkDistance * talkDistance)
            {
                EnsureDialogueManager();
                dm = DialogueManager.Instance;
                if (dm == null)
                {
                    Debug.LogError("[WolfPetFollower2_5D] DialogueManager 생성 실패");
                    return;
                }

                string strongPrompt = BuildWolfSystemPrompt();
                dm.StartAIDialogue(petName, strongPrompt, null);
            }
        }
    }

    private void FixedUpdate()
    {
        if (player == null || playerAnim == null || playerRb == null) return;

        bool dialogueOpen = DialogueManager.Instance != null && DialogueManager.Instance.IsOpen;
        if (dialogueOpen)
        {
            var pos = rb.position;
            pos.z = zLock;
            rb.MovePosition(pos);
            FaceTowardsPlayerSlow();
            if (anim) anim.SetFloat(speedParam, 0f);
            return;
        }

        float vxPlayer = Mathf.Abs(playerRb.linearVelocity.x);
        float runCutoff = Mathf.Max(0.6f * player.runSpeed, player.walkSpeed + 0.1f);
        bool playerRunningNow = vxPlayer >= runCutoff;

        bool groundedNow = GetPlayerGroundedSafe();
        if (prevPlayerGrounded && !groundedNow)
            rb.AddForce(Vector3.up * player.jumpForce, ForceMode.Impulse);
        prevPlayerGrounded = groundedNow;

        Vector3 pos2 = rb.position;
        float dx = player.transform.position.x - pos2.x;
        float adx = Mathf.Abs(dx);

        float maxSpeed = playerRunningNow ? runFollowSpeed : walkFollowSpeed;
        float desiredVx = (adx > followDistance) ? Mathf.Sign(dx) * maxSpeed : 0f;

        float rate = Mathf.Approximately(desiredVx, 0f) ? decel : accel;
        vx = Mathf.MoveTowards(vx, desiredVx, rate * Time.fixedDeltaTime);

        pos2.x += vx * Time.fixedDeltaTime;
        pos2.z = zLock;
        rb.MovePosition(pos2);

        if (Mathf.Abs(vx) > 0.01f)
        {
            float yaw = (vx >= 0) ? 0f : 180f;
            rb.MoveRotation(Quaternion.Euler(0, yaw, 0));
        }

        float animSpeed = (Mathf.Abs(desiredVx) < 0.01f) ? 0f : (playerRunningNow ? 1f : 0.5f);
        if (anim) anim.SetFloat(speedParam, animSpeed);
    }

    private void FaceTowardsPlayerSlow()
    {
        if (player == null) return;
        float dir = Mathf.Sign(player.transform.position.x - transform.position.x);
        float yaw = dir >= 0 ? 0f : 180f;
        var want = Quaternion.Euler(0, yaw, 0);
        rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, want, 720f * Time.fixedDeltaTime));
    }

    private bool GetPlayerGroundedSafe() => Mathf.Abs(playerRb.linearVelocity.y) < 0.01f;

    // ==============
    //  NAV 유틸
    // ==============
    private string GetPlayerHubZoneName(float x)
    {
        // 각 지점 근처면 그 지점 이름을 리턴
        if (Mathf.Abs(x - hubWeaponArmorShopX) <= hubSnapTolerance) return "무기/방어구 상점";
        if (Mathf.Abs(x - hubPotionShopX) <= hubSnapTolerance) return "물약 상점";
        if (Mathf.Abs(x - hubStorageX) <= hubSnapTolerance) return "창고";
        if (Mathf.Abs(x - hubQuestBoardX) <= hubSnapTolerance) return "퀘스트 게시판";
        if (Mathf.Abs(x - hubDomainPortalX) <= hubSnapTolerance) return "영지 포탈";
        return null; // 특정 지점 "바로 앞"은 아님
    }

    private string BuildNavHint(float x)
    {
        // 허브 왼→오 순으로 비교하며, 플레이어 위치 기준 ‘다음에 만날 것’과 ‘되돌아갈 것’을 간단히 말해줌
        // 항상 1~2문장에 담기도록 짧게 반환
        if (x < hubWeaponArmorShopX - hubSnapTolerance)
            return "허브 서쪽 밖이야. 조금만 오른쪽으로 가면 무기/방어구 상점이 먼저 보여.";
        if (x < hubPotionShopX - hubSnapTolerance)
            return "지금 무기/방어구 상점 근처야. 물약 상점은 오른쪽에 있어.";
        if (x < hubStorageX - hubSnapTolerance)
            return "물약 상점 앞이야. 창고는 그 오른쪽이야.";
        if (x < hubQuestBoardX - hubSnapTolerance)
            return "창고 옆에 있어. 퀘스트 게시판은 바로 오른쪽.";
        if (x < hubDomainPortalX - hubSnapTolerance)
            return "퀘스트 게시판 근처야. 영지로 가는 포탈은 바로 오른쪽이야.";
        // 영지 포탈을 넘어 오른쪽
        return "영지 포탈을 지난 동쪽이 스테이지 입구야. 계속 오른쪽으로 가자.";
    }

    // 🐺 시스템 프롬프트(부드러운 말투 + 장비 조언 + 동적 좌우 내비 + 허브 배치)
    private string BuildWolfSystemPrompt()
    {
        Vector3 playerPos = player != null ? player.transform.position : Vector3.zero;
        float px = playerPos.x;

        var rule = FindGearRule(currentDomain);
        string zone = GetPlayerHubZoneName(px);     // 특정 지점에 '서 있다'면 이름, 아니면 null
        string navHint = BuildNavHint(px);          // 현재 x기준 왼/오른쪽 힌트 1문장

        const string HUB_PORTALS =
            "허브(마을)는 왼쪽에서 오른쪽 순서로 ①무기/방어구 상점 → ②물약 상점 → ③창고 → ④퀘스트 게시판 → ⑤영지 포탈 이 배치되어 있다.";

        var sb = new StringBuilder();
        // 페르소나: 부드럽고 다정한 톤, 가끔 의성어
        sb.AppendLine($"너는 \"{petName}\"라는 이름의 늑대 펫이다. 주인의 곁을 지키는 부드럽고 다정한 동료다.");
        sb.AppendLine("항상 한국어로 1~2문장만 말하고, 늑대다운 간결한 말투를 쓰되 딱딱하지 않게 말해라.");
        sb.AppendLine("메타발언/이모지는 금지. 필요하면 아주 가끔 ‘킁’ 같은 짧은 의성어를 쓴다.");

        // 허브 배치 + 진행 규칙
        sb.AppendLine();
        sb.AppendLine("[허브 배치 / 진행 규칙]");
        sb.AppendLine($"- {HUB_PORTALS}");
        sb.AppendLine("- 월드는 기본적으로 왼쪽→오른쪽(동쪽) 진행이지만, 플레이어의 현재 위치를 보고 좌/우를 정확하게 안내하라.");
        sb.AppendLine("- 과설명/장문 금지. 친근하고 짧게.");

        // 현재 진행/장비
        sb.AppendLine();
        sb.AppendLine("[현재 진행]");
        sb.AppendLine($"- 영지: {DomainToKorean(currentDomain)} / 스테이지 {currentStageIndex} / 라운드 {currentRoundIndex} (3×3, 3라운드=미니보스)");

        sb.AppendLine();
        sb.AppendLine("[장비]");
        sb.AppendLine($"- 현재 장비 → 무기: {playerCurrentWeapon}, 방어구: {playerCurrentArmor}");
        sb.AppendLine($"- 권장 장비 → 무기: {rule.recommendedWeapon}, 방어구: {rule.recommendedArmor}");
        sb.AppendLine($"- 이유: {rule.reason}");
        sb.AppendLine("- 현재 장비가 권장과 다르면, 부드럽게 한 문장으로 교체를 권하라.");

        // 동적 내비 정보
        sb.AppendLine();
        sb.AppendLine("[현재 위치 기반 내비]");
        if (!string.IsNullOrEmpty(zone))
            sb.AppendLine($"- 지금 위치: {zone} 근처.");
        else
            sb.AppendLine("- 지금 위치: 허브 내 특정 지점과 정확히 일치하진 않음.");
        sb.AppendLine($"- 힌트: {navHint}");

        // 출력 규칙
        sb.AppendLine();
        sb.AppendLine("[출력]");
        sb.AppendLine("- 한국어 1~2문장, 부드럽고 다정한 늑대 톤.");
        sb.AppendLine("- 상황에 맞게 '오른쪽/왼쪽'을 정확히 써서 안내하거나, 장비 한 줄 조언으로 마무리한다.");

        return sb.ToString();
    }

    private DomainGearRule FindGearRule(DomainTerritory d)
    {
        foreach (var r in gearRules)
            if (r.domain == d) return r;
        return null;
    }

    private string DomainToKorean(DomainTerritory d)
    {
        switch (d)
        {
            case DomainTerritory.ForestGate: return "숲의 입구";
            case DomainTerritory.StoneGraves: return "돌 무덤";
            case DomainTerritory.FireSpiritPlay: return "화염 정령들의 놀이터";
            case DomainTerritory.FrozenMountain: return "얼어붙은 산";
            case DomainTerritory.AncientTemple: return "고대 신전";
            case DomainTerritory.FinalSanctum: return "최후의 신전";
            default: return d.ToString();
        }
    }
}
