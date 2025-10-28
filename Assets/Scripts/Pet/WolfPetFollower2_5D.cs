using UnityEngine;
using System;
using System.Text;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class WolfPetFollower2_5D : MonoBehaviour
{
    public enum DomainTerritory
    {
        ForestGate,       // 1
        StoneGraves,      // 2
        FireSpiritPlay,   // 3
        FrozenMountain,   // 4
        AncientTemple,    // 5
        FinalSanctum      // 6
    }

    [Header("Progress / World State")]
    public DomainTerritory currentDomain = DomainTerritory.ForestGate;
    [Range(0, 3)] public int currentStageIndex = 0; // Town=0, Stage=1~3
    [Range(0, 3)] public int currentRoundIndex = 0; // Town=0, Round=1~3

    [Header("Player Equipment (fallback)")]
    public string playerCurrentWeapon = "부러진 도끼";
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
            recommendedWeapon = "신성한 도끼",
            recommendedArmor = "신성한 갑옷",
            reason = "종합적인 최종 패턴에 대응하기 위해 상위 등급 장비가 필요."
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

    [SerializeField] private PlayerController player; // 인스펙터 수동 연결 가능
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
        InvokeRepeating(nameof(AutoRetryFindPlayer), 0.3f, 0.3f);
    }

    private void Start()
    {
        InvokeRepeating(nameof(TryResolvePlayer), 0.5f, 0.5f);
    }

    private void AutoRetryFindPlayer()
    {
        if (player == null) TryResolvePlayer();
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

            string currentScene = SceneManager.GetActiveScene().name;

            var allPlayers = GameObject.FindObjectsOfType<PlayerController>(true);
            foreach (var p in allPlayers)
            {
                if (p.gameObject.scene.name == currentScene || p.gameObject.scene.name == "DontDestroyOnLoad")
                {
                    player = p;
                    playerAnim = p.GetComponent<Animator>();
                    playerRb = p.GetComponent<Rigidbody>();
                    Debug.Log($"[WolfPetFollower2_5D] PlayerRoot 연결 성공 ✅ ({p.name}, Scene={p.gameObject.scene.name})");
                    return;
                }
            }

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

        // 인벤토리에서 현재 장비 동기화
        var inv = player.GetComponent<PlayerInventory>();
        if (inv != null)
        {
            playerCurrentWeapon = inv.GetEquippedWeaponName();
            playerCurrentArmor = inv.GetEquippedArmorName();
        }

        var dm = DialogueManager.Instance;

        // Esc로 대화 닫기
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
                dm = UnityEngine.Object.FindFirstObjectByType<DialogueManager>();
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
                if (string.IsNullOrEmpty(prompt))
                {
                    Debug.LogWarning("[WolfPetFollower2_5D] BuildWolfSystemPrompt()가 비었습니다. 기본 프롬프트만 사용.");
                    prompt = "(현재 상황 정보를 불러올 수 없습니다.)";
                }
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
    // 진행/씬 동기화
    // =============================
    private void SyncPlayerProgress()
    {
        if (GameManager.Instance != null)
        {
            // GameManager는 1~6/1~3/1~3 기준. Town일 때 0으로 처리
            int terr = Mathf.Clamp(GameManager.Instance.currentTerritory, 1, 6);
            currentDomain = (DomainTerritory)(terr - 1);
            currentStageIndex = Mathf.Clamp(GameManager.Instance.currentStage, 0, 3);
            currentRoundIndex = Mathf.Clamp(GameManager.Instance.currentRound, 0, 3);
        }
    }

    private void AutoDetectDomainFromScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        // Town
        if (sceneName == "Town")
        {
            currentStageIndex = 0;
            currentRoundIndex = 0;
            // currentDomain는 GameManager 진행도 기준 유지(가장 최근 진행 지역)
            Debug.Log("[AutoDetect] 🏠 현재 위치: 마을 (Town)");
            return;
        }

        // 보스 프롤로그 / 보스
        if (sceneName.StartsWith("Stage_Before"))
        {
            currentDomain = DomainTerritory.FinalSanctum;
            currentStageIndex = 0;
            currentRoundIndex = 0;
            Debug.Log("[AutoDetect] ⚔️ 최후의 신전 (보스 직전 스테이지)");
            return;
        }
        if (sceneName.StartsWith("Stage_Boss"))
        {
            currentDomain = DomainTerritory.FinalSanctum;
            currentStageIndex = 0;
            currentRoundIndex = 0;
            Debug.Log("[AutoDetect] 👑 최후의 신전 (보스전)");
            return;
        }
        // Final boss (Stage601_R1 / Stage601_R2)
        if (sceneName.StartsWith("Stage601_"))
        {
            currentDomain = DomainTerritory.FinalSanctum; // 최후의 신전
            currentStageIndex = 1;                        // 보스전이니 1로 고정(표시용)
            currentRoundIndex = sceneName.EndsWith("_R2") ? 2 : 1; // R1=1페, R2=2페
            Debug.Log($"[AutoDetect] FinalSanctum Boss: {sceneName} (Phase {currentRoundIndex})");
            return;
        }


        // 포탈/상점/은신처/퀘스트/월드맵 방
        if (sceneName.Contains("EquipmentShop"))
        {
            currentStageIndex = 0; currentRoundIndex = 0;
            Debug.Log("[AutoDetect] 🛠️ 대장장이의 방");
            return;
        }
        if (sceneName.Contains("AlchemistShop"))
        {
            currentStageIndex = 0; currentRoundIndex = 0;
            Debug.Log("[AutoDetect] 🧪 연금술사의 방");
            return;
        }
        if (sceneName.Contains("WareHouse") || sceneName.Contains("Warehouse"))
        {
            currentStageIndex = 0; currentRoundIndex = 0;
            Debug.Log("[AutoDetect] 📦 은신처");
            return;
        }
        if (sceneName.Contains("QuestBoard"))
        {
            currentStageIndex = 0; currentRoundIndex = 0;
            Debug.Log("[AutoDetect] 📜 퀘스트 게시판");
            return;
        }
        if (sceneName.Contains("StageSelect"))
        {
            currentStageIndex = 0; currentRoundIndex = 0;
            Debug.Log("[AutoDetect] 🗺️ 월드 이동 지도");
            return;
        }

        // 스테이지: StageXXX_RY
        if (!sceneName.StartsWith("Stage")) return;

        try
        {
            string[] parts = sceneName.Split('_');
            string mainPart = parts[0].Replace("Stage", "");
            string roundPart = parts.Length > 1 ? parts[1].Replace("R", "") : "1";

            int mainValue = int.Parse(mainPart); // 101, 205, 503, 601 등
            int roundValue = int.Parse(roundPart); // 1~3

            int domainId = mainValue / 100;   // 1~6
            int stageIndex = mainValue % 100; // 01~03 (FinalSanctum라도 01~ 가능성 대응)

            if (domainId < 1) domainId = 1;
            if (domainId > 6) domainId = 6;
            if (stageIndex < 1 || stageIndex > 3) stageIndex = 1;
            if (roundValue < 1 || roundValue > 3) roundValue = 1;

            currentDomain = (DomainTerritory)(domainId - 1);
            currentStageIndex = stageIndex;
            currentRoundIndex = roundValue;

            Debug.Log($"[AutoDetect] Scene={sceneName} → Domain={currentDomain}, Stage={currentStageIndex}, Round={currentRoundIndex}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AutoDetectDomainFromScene] 파싱 실패: {sceneName}, 예외={e.Message}");
        }
    }
    // === Final Boss (Stage601) scene helpers ===
    private bool IsFinalBossScene()
    {
        string n = SceneManager.GetActiveScene().name;
        return n.StartsWith("Stage601_");   // Stage601_R1 / Stage601_R2
    }

    private int GetFinalBossPhase()
    {
        string n = SceneManager.GetActiveScene().name;
        if (n.EndsWith("_R1")) return 1;
        if (n.EndsWith("_R2")) return 2;
        return 0; // 알 수 없음
    }


    // =============================
    // 🧠 GPT 시스템 프롬프트 생성
    // =============================
    private string BuildWolfSystemPrompt()
    {
        SyncPlayerProgress();        // GameManager 진행도 반영
        AutoDetectDomainFromScene(); // 씬 이름 기반 자동 감지

        var rule = FindGearRule(currentDomain);
        var sb = new StringBuilder();

        // 펫 기본 성격/금칙
        sb.AppendLine($"너는 \"{petName}\"라는 이름의 늑대 펫이다. 주인의 곁을 지키는 다정하고 충직한 동료다.");
        sb.AppendLine("항상 한국어로 1~2문장만 대답하고, 늑대다운 간결한 말투지만 부드럽게 말한다.");
        sb.AppendLine("이모지나 메타발언, 과장 설정 추가는 금지다.");

        // 현재 진행
        sb.AppendLine();
        sb.AppendLine("[현재 진행]");
        sb.AppendLine($"- 현재 영지: {DomainToKorean(currentDomain)}");
        if (IsTownLike(SceneManager.GetActiveScene().name))
            sb.AppendLine("- 현재 장면: 마을");
        else
            sb.AppendLine($"- 스테이지: {currentStageIndex}, 라운드: {currentRoundIndex}");

        // 장비
        sb.AppendLine();
        sb.AppendLine("[장비 정보]");
        sb.AppendLine($"- 플레이어 장비 → 무기: {playerCurrentWeapon}, 방어구: {playerCurrentArmor}");
        sb.AppendLine($"- 권장 장비 → 무기: {rule.recommendedWeapon}, 방어구: {rule.recommendedArmor}");
        sb.AppendLine($"- 이유: {rule.reason}");

        // 단계 비교 (더 낮을 때만 강력 권고)
        int weaponLevel = GetEquipmentLevel(playerCurrentWeapon);
        int armorLevel = GetEquipmentLevel(playerCurrentArmor);
        int recommendedWeaponLevel = GetEquipmentLevel(rule.recommendedWeapon);
        int recommendedArmorLevel = GetEquipmentLevel(rule.recommendedArmor);

        bool weaponTooWeak = weaponLevel < recommendedWeaponLevel;
        bool armorTooWeak = armorLevel < recommendedArmorLevel;

        if (weaponTooWeak || armorTooWeak)
        {
            sb.AppendLine();
            sb.AppendLine("[장비 비교]");
            if (weaponTooWeak) sb.AppendLine("- 현재 무기는 권장보다 한 단계 낮습니다.");
            if (armorTooWeak) sb.AppendLine("- 현재 방어구는 권장보다 낮습니다.");
            sb.AppendLine("- 말투는 명령이 아니라 걱정스러운 조언으로, 대장장이 방문/교체를 권유하라.");
            sb.AppendLine("- 예: '이대로는 버티기 힘들어요. 대장장이한테 가보는 게 좋겠어요.'");
        }

        // ==== 포션 정보 주입 (연금술사 구매/키 사용/보스전 팁) ====
        int small = 0, medium = 0, large = 0;
        var invForPot = player != null ? player.GetComponent<PlayerInventory>() : null;
        if (invForPot != null)
        {
            small = invForPot.smallPotions;
            medium = invForPot.mediumPotions;
            large = invForPot.largePotions;
        }

        sb.AppendLine();
        sb.AppendLine("[포션 정보]");
        sb.AppendLine("- 종류와 효과: 소형 = 체력 30 회복, 중형 = 체력 50 회복, 대형 = 체력 완전 회복.");
        sb.AppendLine("- 사용 키: 소형(1키), 중형(2키), 대형(3키).");
        sb.AppendLine("- 구매처: 연금술사의 방에서 골드로 구매 가능.");
        sb.AppendLine($"- 현재 보유: 소형 {small}개, 중형 {medium}개, 대형 {large}개.");

        // 안내 규칙(포션 관련)
        sb.AppendLine();
        sb.AppendLine("[포션 안내 규칙]");
        sb.AppendLine("- 플레이어가 회복/포션/보스전을 물으면, 위 포션 효과와 사용 키를 간단히 알려준다.");
        sb.AppendLine("- 보스전/장시간 전투 전에는 대형 포션 보유를 권한다. 포션이 부족하면 연금술사 방문을 제안한다.");
        sb.AppendLine("- 숫자 좌표는 말하지 말고, 연금술사는 '마을에서만 방향 안내' 원칙을 따른다.");


        // 씬/위치 안내
        string scene = SceneManager.GetActiveScene().name;
        float posX = player != null ? player.transform.position.x : 0f;

        // 추가: 보스/마을 플래그
        bool isFinalBoss = IsFinalBossScene();   // Stage601_R1/R2 일 때만 true
        bool isTown = IsTownLike(scene);         // Town이면 true
        int bossPhase = GetFinalBossPhase();     // 0/1/2

        // ==== 씬별 기능/제한 설명 추가 ====
        string placeDesc = "";
        string responseLimit = "";

        // ⚠️ 여기서 scene 이름 패턴은 프로젝트에 맞춰 필요하면 더 추가/수정하세요.
        if (scene == "Town")
        {
            placeDesc = "- 현재 위치: 마을(중앙 광장).";
            responseLimit =
                "- 마을에서는 시설(대장장이/연금술사/은신처/퀘스트/월드지도) 안내가 가능하다.\n" +
                "- 플레이어가 특정 시설을 묻지 않아도 방향 안내가 가능하다.";
        }
        else if (scene.Contains("AlchemistShop"))
        {
            placeDesc = "- 현재 위치: 연금술사의 방 — 포션을 골드로 구매할 수 있다.";
            responseLimit =
                "- 이 방에서는 포션 구매/효과/사용만 말한다.\n" +
                "- 장비 구매/교체(대장장이/은신처) 이야기는 **플레이어가 먼저 물을 때만** 짧게 언급한다(예: '그건 대장장이/은신처에서 가능해요.').\n" +
                "- 방향 안내는 하지 않는다(마을에서만 방향 안내).";
        }
        else if (scene.Contains("EquipmentShop"))
        {
            placeDesc = "- 현재 위치: 대장장이의 방 — 무기/방어구를 골드로 구매할 수 있다.";
            responseLimit =
                "- 이 방에서는 장비 구매/강화만 말한다.\n" +
                "- 포션/퀘스트/월드 이동은 언급하지 않는다(플레이어가 먼저 물을 때만 짧게 장소명만).\n" +
                "- 방향 안내는 하지 않는다(마을에서만 방향 안내).";
        }
        else if (scene.Contains("WareHouse") || scene.Contains("Warehouse"))
        {
            placeDesc = "- 현재 위치: 은신처 — 무기/방어구 교체와 게임 저장이 가능하다.";
            responseLimit =
                "- 이 방에서는 장비 교체/저장만 말한다.\n" +
                "- 구매(대장장이/연금술사)나 퀘스트 보상은 언급하지 않는다(플레이어가 먼저 물을 때만 장소명만).\n" +
                "- 방향 안내는 하지 않는다.";
        }
        else if (scene.Contains("QuestBoard"))
        {
            placeDesc = "- 현재 위치: 퀘스트 게시판 — 사냥 아이템 보상/교환.";
            responseLimit =
                "- 이 방에서는 퀘스트 수락/완료/보상 교환만 말한다.\n" +
                "- 장비/포션/저장은 언급하지 않는다(질문 받으면 장소명만).\n" +
                "- 방향 안내는 하지 않는다.";
        }
        else if (scene.Contains("StageSelect"))
        {
            placeDesc = "- 현재 위치: 월드 이동 지도 — 원하는 영지/스테이지로 이동.";
            responseLimit =
                "- 이 공간에서는 이동/선택만 말한다.\n" +
                "- 장비/포션/저장은 언급하지 않는다(질문 시 장소명만).\n" +
                "- 방향 안내는 하지 않는다.";
        }
        else if (scene.StartsWith("Stage601_"))
        {
            int phase = GetFinalBossPhase();
            placeDesc = $"- 현재 위치: 최후의 신전 보스전 (페이즈 {phase}).";
            responseLimit =
                "- 전투 관련 짧은 회피 팁만 말한다. 마을 시설은 언급하지 않는다.\n" +
                "- 방향 안내는 하지 않는다.";
        }
        else if (scene.StartsWith("Stage_Before"))
        {
            placeDesc = "- 현재 위치: 최후의 신전 보스 전(프롤로그 구역).";
            responseLimit =
                "- 전투 준비/다음 진행만 말한다. 마을 시설은 언급하지 않는다.\n" +
                "- 방향 안내는 하지 않는다.";
        }
        else if (scene.StartsWith("Stage_Boss"))
        {
            placeDesc = "- 현재 위치: 최후의 신전 보스전.";
            responseLimit =
                "- 전투 관련 짧은 조언만 말한다. 마을 시설은 언급하지 않는다.\n" +
                "- 방향 안내는 하지 않는다.";
        }
        else if (scene.StartsWith("Stage"))
        {
            placeDesc = "- 현재 위치: 전투 지역(스테이지).";
            responseLimit =
                "- 전투/지형/회복 조언만 말한다. 마을 시설은 언급하지 않는다.\n" +
                "- 방향 안내는 하지 않는다.";
        }
        else
        {
            placeDesc = "- 현재 위치: 기타 장면.";
            responseLimit =
                "- 필요 없는 시설 언급을 하지 않는다.\n" +
                "- 방향 안내는 하지 않는다.";
        }

        // 프롬프트에 주입
        sb.AppendLine();
        sb.AppendLine("[현재 위치 설명]");
        sb.AppendLine(placeDesc);

        sb.AppendLine();
        sb.AppendLine("[응답 제한]");
        sb.AppendLine(responseLimit);


        sb.AppendLine();
        sb.AppendLine("[현재 게임 상태]");
        sb.AppendLine($"- 현재 씬: {scene}");

        // 마을 전용 좌표(대략) – 마을에서만 사용
        float blacksmithX = -17f;
        float alchemistX = -5f;
        float storageX = 5f;
        float questX = 14f;
        float worldMapX = 22f;

        string Dir(float target)
        {
            if (Mathf.Abs(target - posX) < 1f) return "바로 근처";
            return (target > posX) ? "오른쪽" : "왼쪽";
        }

        sb.AppendLine();
        sb.AppendLine("[위치 요약]");
        if (scene == "Town")
        {
            sb.AppendLine($"- 대장장이의 방은 플레이어 기준으로 {Dir(blacksmithX)}에 있어요.");
            sb.AppendLine($"- 연금술사의 방은 플레이어 기준으로 {Dir(alchemistX)}에 있어요.");
            sb.AppendLine($"- 은신처는 플레이어 기준으로 {Dir(storageX)}에 있어요.");
            sb.AppendLine($"- 퀘스트 게시판은 플레이어 기준으로 {Dir(questX)}에 있어요.");
            sb.AppendLine($"- 월드 이동 지도는 플레이어 기준으로 {Dir(worldMapX)}에 있어요.");
        }
        else
        {
            // 마을 외 지역: 시설 없음 명시
            sb.AppendLine("- 이 지역에는 마을 시설(대장장이/연금술사/은신처/퀘스트/월드지도)이 없습니다.");
            sb.AppendLine("- 시설 위치를 묻더라도, 마을에 있을 때만 방향을 알려 줍니다.");
        }

        // 세계 상호작용 규칙 (명확하게)
        sb.AppendLine();
        sb.AppendLine("[세계 상호작용 규칙]");
        sb.AppendLine("- 대장장이의 방: 무기/방어구를 골드로 구매.");
        sb.AppendLine("- 연금술사의 방: 물약을 골드로 구매.");
        sb.AppendLine("- 은신처: 무기·방어구 교체 및 게임 저장 가능.");
        sb.AppendLine("- 퀘스트 게시판: 사냥 아이템을 골드로 교환/보상 수령.");
        sb.AppendLine("- 월드 이동 지도: 원하는 영지/스테이지로 이동.");

        // 안내 규칙
        sb.AppendLine();
        sb.AppendLine("[안내 규칙]");
        sb.AppendLine("- 물건 구매 → 대장장이(장비) / 연금술사(포션)로 안내.");
        sb.AppendLine("- 장비 교체/저장 → 은신처로 안내.");
        sb.AppendLine("- 퀘스트/보상 → 퀘스트 게시판 언급.");
        sb.AppendLine("- 지역 이동 원함 → 월드 이동 지도 언급.");
        sb.AppendLine("- 시설 방향 안내는 마을에서만. 숫자 좌표는 말하지 않는다.");

        // 스토리 제약(없는 설정 금지)
        sb.AppendLine();
        sb.AppendLine("[스토리 제약]");
        sb.AppendLine("- 전설의 도끼와 산신령 관련 이야기까지만 언급한다.");
        sb.AppendLine("- '어둠의 군주' 같은 추가 설정은 만들지 않는다.");

        // 🕹 조작 안내 (질문받았을 때만 사용)
        sb.AppendLine();
        sb.AppendLine("[조작 안내]");
        sb.AppendLine("- 플레이어가 조작/키/컨트롤을 물을 때만 아래 정보를 간단히 알려준다. 다른 상황에서는 언급하지 않는다.");
        sb.AppendLine("- 이동: 왼쪽/오른쪽 방향키");
        sb.AppendLine("- 점프: C 키");
        sb.AppendLine("- 공격: Z 키");
        sb.AppendLine("- 대화: 펫과 충분히 가까이에서 Enter 키");

        // (선택) 대화 톤 보강—조작 질문일 때의 답변 예시
        sb.AppendLine("- 조작을 물으면 1문장으로 짧게: 예) '이동은 방향키, 점프는 C, 공격은 Z, 대화는 가까이서 Enter예요.'");

        // 🧠 보스 패턴 가이드 (Phase 1 전용)
        sb.AppendLine();
        sb.AppendLine("[보스 패턴 가이드]");
        sb.AppendLine("- 이 섹션은 플레이어가 '보스/패턴/공격'을 물을 때만 사용한다.");
        sb.AppendLine($"- 상태 플래그: isFinalBoss={(isFinalBoss ? "true" : "false")}, isTown={(isTown ? "true" : "false")}, phase={bossPhase}.");
        sb.AppendLine("- '지금은 보스전이에요.'라는 문장은 isFinalBoss==true일 때만 사용한다.");
        sb.AppendLine("- isFinalBoss==false이면 '지금은 보스전은 아니지만,'으로 시작해 간단히 설명한다.");
        sb.AppendLine("- 한 번에 1~2문장, 가장 시급한 회피 팁만 말한다.");
        sb.AppendLine("- 숫자는 꼭 필요한 것만: 10 피해 / 0.5초당 1 또는 5 피해 / 2초 지연.");
        sb.AppendLine("- 절대 새로운 패턴을 지어내지 말고(2페이즈 언급 금지), 허구의 설정을 추가하지 말 것.");

        // 패턴 데이터 (Phase 1, 4종)
        sb.AppendLine("패턴1) 기본 공격: 전방 짧은 사거리, 10 피해. 접근 시 사거리 밖을 유지하거나 예측 후 후퇴.");
        sb.AppendLine("패턴2) 폭발/독 장판: 발밑 표시 → 약 2초 후 폭발, 이후 독(0.5초마다 1 피해). 표시면 즉시 이탈, 독 장판은 밟지 않기.");
        sb.AppendLine("패턴3) 투사체(큰돌/작은돌): 플레이어를 향해 투척. 타이밍 맞춰 점프로 넘기기. 큰돌이 먼저면 착지 지점 확보.");
        sb.AppendLine("패턴4) 레이저: 조준 후 0.5초마다 5 피해. 발사 타이밍에 맞춰 '정확히' 점프.");

        // (선택) 보스전일 때 경고 톤 힌트
        if (isFinalBoss)
        {
            sb.AppendLine("- 응답 서두 예시: '지금은 보스전이에요. 바닥 표시면 2초 뒤 터져요—바로 빠져요.'");
        }
        else
        {
            sb.AppendLine("- 응답 서두 예시: '지금은 보스전은 아니지만, 레이저는 타이밍 점프가 핵심이에요.'");
        }

        // 대화 스타일
        sb.AppendLine();
        sb.AppendLine("[대화 스타일]");
        sb.AppendLine("- 좌표/수치 대신 ‘왼쪽’, ‘오른쪽’, ‘가까이’, ‘조금 더 가면’ 식으로 말한다.");
        sb.AppendLine("- 예: ‘조금 왼쪽이에요.’, ‘바로 오른쪽이에요.’, ‘조금만 더 가면 보여요.’");
        sb.AppendLine("- 항상 1~2문장, 현실감 있는 톤.");

        return sb.ToString();
    }

    private bool IsTownLike(string sceneName)
    {
        if (sceneName == "Town") return true;
        // 상점/은신처/퀘스트/월드맵 방도 "마을 파생 공간"으로 처리하고 싶다면 true로 바꿔도 됨
        return false;
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

    // 장비 단계: 돌1, 철2, 화염3, 얼음4, 신성5, 궁극6
    private int GetEquipmentLevel(string name)
    {
        if (string.IsNullOrEmpty(name)) return 0;
        string n = name.Replace(" ", "");
        if (n.Contains("돌")) return 1;
        if (n.Contains("철")) return 2;
        if (n.Contains("화염")) return 3;
        if (n.Contains("얼음")) return 4;
        if (n.Contains("신성")) return 5;
        if (n.Contains("궁극")) return 6;
        // 그 외: 부러진 도끼/천 갑옷 등은 0으로 취급
        return 0;
    }
}
