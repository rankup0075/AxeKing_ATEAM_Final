using UnityEngine;
using System;
using System.Text;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class WolfPetFollower2_5D : MonoBehaviour
{
    public enum DomainTerritory
    {
        ForestGate,
        StoneGraves,
        FireSpiritPlay,
        FrozenMountain,
        AncientTemple,
        FinalSanctum
    }

    [Header("Progress / World State")]
    public DomainTerritory currentDomain = DomainTerritory.ForestGate;
    [Range(1, 3)] public int currentStageIndex = 1;
    [Range(1, 3)] public int currentRoundIndex = 1;

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
    public float walkFollowSpeed = 3f;
    public float runFollowSpeed = 7f;
    public float accel = 10f;
    public float decel = 14f;

    [Header("Animation")]
    public Animator anim;
    public string speedParam = "Speed";

    [Header("Dialogue (AI Only)")]
    public string petName = "Wolf";
    public float talkDistance = 2.0f;
    public KeyCode talkKey = KeyCode.Return;

    // === 내부 필드 ===
    private Rigidbody rb;
    private float vx;
    private float zLock;

    [SerializeField] private PlayerController player; // 👈 인스펙터 수동 연결 가능
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

        // 처음 시도
        TryResolvePlayer();

        // 💡 0.3초마다 자동 탐색 시도 (씬 전환, 생성 지연 대응)
        InvokeRepeating(nameof(AutoRetryFindPlayer), 0.3f, 0.3f);
    }

    private void Start()
    {
        // 💡 혹시 Awake 시점에 Player가 아직 생성되지 않은 경우 대비
        InvokeRepeating(nameof(TryResolvePlayer), 0.5f, 0.5f);
    }

    private void AutoRetryFindPlayer()
    {
        if (player == null)
        {
            TryResolvePlayer();
        }
        else
        {
            CancelInvoke(nameof(AutoRetryFindPlayer));
            Debug.Log($"[WolfPetFollower2_5D] Player 지속 감지 성공 ✅ ({player.name})");
        }
    }

    private void TryResolvePlayer()
    {
        try
        {
            if (player != null) return;

            // 1️⃣ 현재 Scene 이름
            string currentScene = SceneManager.GetActiveScene().name;

            // 2️⃣ Scene 내부의 PlayerController만 탐색
            var allPlayers = GameObject.FindObjectsOfType<PlayerController>(true);
            foreach (var p in allPlayers)
            {
                // 씬 내부 또는 DontDestroyOnLoad 내부 PlayerController 모두 허용
                if (p.gameObject.scene.name == currentScene || p.gameObject.scene.name == "DontDestroyOnLoad")
                {
                    player = p;
                    playerAnim = p.GetComponent<Animator>();
                    playerRb = p.GetComponent<Rigidbody>();
                    Debug.Log($"[WolfPetFollower2_5D] PlayerRoot 연결 성공 ✅ ({p.name}, Scene={p.gameObject.scene.name})");
                    return;
                }
            }

            // 3️⃣ 이름 기반 탐색 시에도 DontDestroyOnLoad은 제외
            var allObjs = GameObject.FindObjectsOfType<GameObject>();
            foreach (var obj in allObjs)
            {
                if (obj.name.Contains("PlayerRoot") && obj.scene.name == currentScene)
                {
                    player = obj.GetComponent<PlayerController>();
                    playerAnim = obj.GetComponent<Animator>();
                    playerRb = obj.GetComponent<Rigidbody>();
                    Debug.Log($"[WolfPetFollower2_5D] 이름 기반 Scene PlayerRoot 연결 성공 ✅ ({obj.name}, Scene={currentScene})");
                    return;
                }
            }

            Debug.LogWarning($"[WolfPetFollower2_5D] 현재 Scene({currentScene})에서 Player를 찾을 수 없습니다 ❌");

            // ✅ 여기 추가 (player가 null이 아닐 경우 위치 확인용 로그)
            if (player != null)
            {
                Debug.Log($"[디버그] PlayerRoot 위치: {player.transform.position}, Scene: {player.gameObject.scene.name}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[WolfPetFollower2_5D] TryResolvePlayer 예외: {e.Message}");
        }
    }


    public void ForceAssignPlayer(PlayerController target)
    {
        if (target == null) return;
        player = target;
        playerRb = target.GetComponent<Rigidbody>();
        playerAnim = target.GetComponent<Animator>();
        Debug.Log($"[WolfPetFollower2_5D] ForceAssignPlayer 완료 → {target.name}");
    }

    private void Update()
    {
        if (player == null) TryResolvePlayer();
        if (player == null) return;

        var dm = DialogueManager.Instance;

        // Esc 누르면 대화 닫기
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (dm != null && dm.IsOpen)
            {
                dm.ForceClose();
                return;
            }
        }

        // 대화 시작
        if (Input.GetKeyDown(talkKey))
        {
            if (dm == null)
            {
                dm = FindFirstObjectByType<DialogueManager>();
                if (dm == null)
                {
                    Debug.LogError("[WolfPetFollower2_5D] DialogueManager 없음!");
                    return;
                }
            }

            if (dm.IsOpen) return;

            if (UIManager.Instance != null)
            {
                var cg = UIManager.Instance.settingsPanel_InGame?.GetComponent<CanvasGroup>();
                if (cg != null && cg.alpha > 0.5f) return;
            }

            float sqrDist = (player.transform.position - transform.position).sqrMagnitude;
            if (sqrDist <= talkDistance * talkDistance)
            {
                string prompt = BuildWolfSystemPrompt();
                dm.StartAIDialogue(petName, prompt, null);
            }
        }
    }

    private void FixedUpdate()
    {
        if (player == null || playerAnim == null || playerRb == null) return;

        bool dialogueOpen = DialogueManager.Instance != null && DialogueManager.Instance.IsOpen;
        if (dialogueOpen)
        {
            FaceTowardsPlayerSlow();
            if (anim) anim.SetFloat(speedParam, 0f);
            return;
        }

        float vxPlayer = Mathf.Abs(playerRb.linearVelocity.x);
        float runCutoff = Mathf.Max(0.6f * player.runSpeed, player.walkSpeed + 0.1f);
        bool playerRunningNow = vxPlayer >= runCutoff;

        bool groundedNow = Mathf.Abs(playerRb.linearVelocity.y) < 0.01f;
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

    // =============================
    // 🧠 GPT 시스템 프롬프트 생성
    // =============================
    private string BuildWolfSystemPrompt()
    {
        var rule = FindGearRule(currentDomain);
        var sb = new StringBuilder();

        // 펫의 기본 성격
        sb.AppendLine($"너는 \"{petName}\"라는 이름의 늑대 펫이다. 주인의 곁을 지키는 다정하고 충직한 동료다.");
        sb.AppendLine("항상 한국어로 1~2문장만 대답하고, 늑대다운 간결한 말투를 유지하되 부드럽고 자연스럽게 말한다.");
        sb.AppendLine("이모지, 메타발언, 농담은 절대 하지 않는다. 필요할 때는 짧게 '킁' 같은 의성어를 써라.");

        // 게임 진행 정보
        sb.AppendLine();
        sb.AppendLine("[현재 진행]");
        sb.AppendLine($"- 현재 영지: {DomainToKorean(currentDomain)}");
        sb.AppendLine($"- 스테이지: {currentStageIndex}, 라운드: {currentRoundIndex}");

        // 장비 정보
        sb.AppendLine();
        sb.AppendLine("[장비 정보]");
        sb.AppendLine($"- 플레이어 장비 → 무기: {playerCurrentWeapon}, 방어구: {playerCurrentArmor}");
        sb.AppendLine($"- 권장 장비 → 무기: {rule.recommendedWeapon}, 방어구: {rule.recommendedArmor}");
        sb.AppendLine($"- 이유: {rule.reason}");

        // 현재 씬 / 위치 전달
        string scene = SceneManager.GetActiveScene().name;
        float posX = player != null ? player.transform.position.x : 0f;

        sb.AppendLine();
        sb.AppendLine("[현재 게임 상태]");
        sb.AppendLine($"- 현재 씬: {scene}");
        sb.AppendLine($"- 플레이어 위치 X좌표: {posX:F1}");

        // 주요 지점
        float blacksmithX = -17f;
        float alchemistX = -5f;
        float storageX = 5f;
        float questX = 14f;
        float worldMapX = 22f;

        // 📍 방향 계산 함수
        string Dir(float target)
        {
            if (Mathf.Abs(target - posX) < 1f)
                return "바로 근처";
            else if (target > posX)
                return "오른쪽";
            else
                return "왼쪽";
        }

        // 📍 플레이어 기준 방향 요약
        sb.AppendLine();
        sb.AppendLine("[위치 요약]");
        sb.AppendLine($"- 대장장이의 방은 플레이어 기준으로 {Dir(blacksmithX)}에 있어요.");
        sb.AppendLine($"- 연금술사의 방은 플레이어 기준으로 {Dir(alchemistX)}에 있어요.");
        sb.AppendLine($"- 은신처는 플레이어 기준으로 {Dir(storageX)}에 있어요.");
        sb.AppendLine($"- 퀘스트 게시판은 플레이어 기준으로 {Dir(questX)}에 있어요.");
        sb.AppendLine($"- 월드 이동 지도는 플레이어 기준으로 {Dir(worldMapX)}에 있어요.");

        // 🎯 GPT 대화 스타일
        sb.AppendLine();
        sb.AppendLine("[대화 스타일]");
        sb.AppendLine("- X좌표 숫자를 말하지 말고, ‘왼쪽’, ‘오른쪽’, ‘가까이’, ‘조금 더 가면’ 같은 말로 방향을 표현하라.");
        sb.AppendLine("- 예: ‘조금 왼쪽이에요.’, ‘바로 오른쪽이에요.’, ‘조금만 더 가면 보여요.’");
        sb.AppendLine("- 감정이나 모험 관련 질문에는 따뜻하고 자연스럽게 반응하라.");
        sb.AppendLine("- 항상 짧고 현실감 있는 톤으로, 1~2문장만 답한다.");

        Debug.Log($"[Prompt] Player 위치 감지 성공 ✅ X={posX:F1}");
        return sb.ToString();
    }

    private DomainGearRule FindGearRule(DomainTerritory d)
    {
        foreach (var r in gearRules)
            if (r.domain == d) return r;
        return gearRules.Length > 0 ? gearRules[0] : null;
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
