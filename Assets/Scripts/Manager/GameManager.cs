using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum TransitionKind
{
    None, FromStageSelect, PortalForward, PortalBackward, ReturnToTown, ShopToTown, TownToShop
}

[Serializable]
public class TransitionContext
{
    public TransitionKind kind = TransitionKind.None;
    public string fromScene = null;
    public string toScene = null;
    public string portalId = null;
    public string namedPoint = null;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Player Data")]
    public long gold = 0;
    public int currentTerritory = 1;
    public int currentStage = 1;
    public int currentRound = 1;

    [Header("Game Settings")]
    public bool isPaused = false;

    [Header("HUD / Pet / Player")]
    public GameObject playerHUDPrefab;
    private GameObject playerHUDInstance;

    public GameObject petPrefab;
    private GameObject petInstance;

    public GameObject playerPrefab;
    private GameObject playerInstance;

    private HashSet<string> clearedRounds = new HashSet<string>();
    private HashSet<string> defeatedBosses = new HashSet<string>();

    // 이동 컨텍스트
    private TransitionContext ctx = new TransitionContext();
    private bool isResolving = false;

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void BeginTransition(TransitionKind kind, string toScene, string portalId = null, string namedPoint = null)
    {
        ctx.kind = kind;
        ctx.fromScene = SceneManager.GetActiveScene().name;
        ctx.toScene = toScene;
        ctx.portalId = portalId;
        ctx.namedPoint = namedPoint;
    }

    private void ClearTransition()
    {
        ctx = new TransitionContext();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (this != Instance) return;
        if (isResolving) return;
        isResolving = true;
        StartCoroutine(SpawnFlow(scene));
    }

    private System.Collections.IEnumerator SpawnFlow(Scene scene)
    {
        // 씬의 루트 오브젝트가 모두 살아날 때까지 1프레임 대기
        yield return null;

        // 스폰 좌표 계산(한 번만)
        Vector3 spawnPos = ResolveSpawn(scene.name, ctx);
        Debug.Log($"[Spawn] scene={scene.name}, kind={ctx.kind}, from={ctx.fromScene}, to={ctx.toScene}, portalId={ctx.portalId}, named={ctx.namedPoint}");

        // 플레이어 생성/이동
        if (scene.name != "MainMenu")
        {
            if (playerInstance == null && playerPrefab != null)
            {
                playerInstance = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
                DontDestroyOnLoad(playerInstance);
            }
            else if (playerInstance != null)
            {
                playerInstance.transform.position = spawnPos;
            }

            // 이동 후 물리/컨트롤 초기화
            if (playerInstance != null)
            {
                //if (playerInstance.TryGetComponent<Rigidbody2D>(out var rb2d)) rb2d.velocity = Vector2.zero;
                if (playerInstance.TryGetComponent<Rigidbody>(out var rb3d))
                {
                    rb3d.linearVelocity = Vector3.zero;
                    rb3d.angularVelocity = Vector3.zero;
                    rb3d.Sleep();
                }

                var pc = playerInstance.GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.canControl = true;
                    pc.canMove = true;
                }
                var anim = playerInstance.GetComponent<Animator>();
                if (anim) anim.speed = 1f;
                Time.timeScale = 1f;
            }

            // HUD 생성/표시
            bool showHUD = true;
            if (playerHUDInstance == null && playerHUDPrefab != null && showHUD)
            {
                playerHUDInstance = Instantiate(playerHUDPrefab);
                DontDestroyOnLoad(playerHUDInstance);
                UIManager.Instance?.InitPlayerHUD();
            }
            if (playerHUDInstance != null) playerHUDInstance.SetActive(showHUD);

            // HUD 수치 갱신
            if (playerInstance != null)
            {
                var inv = playerInstance.GetComponent<PlayerInventory>();
                if (inv != null)
                    UIManager.Instance?.UpdateHUDPotions(inv.smallPotions, inv.mediumPotions, inv.largePotions);

                var hp = playerInstance.GetComponent<PlayerHealth>();
                if (hp != null)
                    UIManager.Instance?.UpdateHUDHealth(hp.currentHealth, hp.maxHealth);

                UIManager.Instance?.UpdateHUDGold(gold);
            }

            // 펫 생성/위치 조정
            if (petPrefab != null)
            {
                if (petInstance == null)
                {
                    Vector3 p = (playerInstance != null ? playerInstance.transform.position : spawnPos) + new Vector3(1f, 0f, -1f);
                    petInstance = Instantiate(petPrefab, p, Quaternion.identity);
                    DontDestroyOnLoad(petInstance);
                }
                else if (playerInstance != null)
                {
                    AdjustPetPosition(playerInstance.transform.position);
                }
            }
        }
        else
        {
            if (playerHUDInstance != null) playerHUDInstance.SetActive(false);
        }

        ClearTransition();
        isResolving = false;
        Portal.ClearBusy();
    }

    private Vector3 ResolveSpawn(string sceneName, TransitionContext c)
    {
        // 0) Portal/시스템에서 명시한 스폰 지점이 있으면 우선
        if (!string.IsNullOrEmpty(c.namedPoint))
            return Need(c.namedPoint);

        // 1) Town 기본 진입
        if (sceneName == "Town")
            return Need("TownStartSpawnPoint");

        // 2) Stage/Round 기본 진입
        if (sceneName.StartsWith("Stage") || sceneName.StartsWith("Round"))
            return Need("PlayerSpawnPoint");

        // 3) Shop/창고류 기본 진입
        if (sceneName.Contains("Shop") || sceneName.Contains("Warehouse"))
            return Need("PlayerSpawnPoint");

        return Vector3.zero;
    }

    private Vector3 Need(string name)
    {
        var scene = SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            var t = FindInChildren(root.transform, name);
            if (t != null) return t.position;
        }
        Debug.LogError($"[Spawn] '{name}' 없음 → (0,0,0)로 대체. scene={scene.name}");
        return Vector3.zero;
    }

    private Transform FindInChildren(Transform p, string name)
    {
        if (p.name == name) return p;
        for (int i = 0; i < p.childCount; i++)
        {
            var r = FindInChildren(p.GetChild(i), name);
            if (r != null) return r;
        }
        return null;
    }

    private void AdjustPetPosition(Vector3 basePos)
    {
        if (petInstance == null) return;
        Vector3 petPos = basePos + new Vector3(1f, 0f, -1f);
        petInstance.transform.position = petPos;

        if (petInstance.TryGetComponent<Rigidbody>(out var petRb))
        {
            petRb.linearVelocity = Vector3.zero;
            petRb.angularVelocity = Vector3.zero;
            petRb.Sleep();
        }
    }

    public long Gold => gold;

    public void AddGold(long amount)
    {
        gold += amount;
        UIManager.Instance?.UpdateGoldDisplay(gold);
        UIManager.Instance?.UpdateHUDGold(gold);
        SavePlayerData();
    }

    public bool SpendGold(long amount)
    {
        if (gold < amount) return false;
        gold -= amount;
        UIManager.Instance?.UpdateGoldDisplay(gold);
        UIManager.Instance?.UpdateHUDGold(gold);
        SavePlayerData();
        return true;
    }

    public void UnlockStage(int territoryId, int stageId, int roundId)
    {
        if (roundId > 3)
        {
            roundId = 1; stageId++;
            if (stageId > 5) { stageId = 1; territoryId++; }
        }
        if (territoryId > currentTerritory ||
            (territoryId == currentTerritory && stageId > currentStage) ||
            (territoryId == currentTerritory && stageId == currentStage && roundId > currentRound))
        {
            currentTerritory = territoryId;
            currentStage = stageId;
            currentRound = roundId;
            SavePlayerData();
        }
    }

    public void GameOver()
    {
        Time.timeScale = 0f;
        UIManager.Instance?.ShowGameOverUI();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Town");
    }

    public void SavePlayerData()
    {
        PlayerPrefs.SetString("Gold", gold.ToString("N0"));
        PlayerPrefs.SetInt("CurrentTerritory", currentTerritory);
        PlayerPrefs.SetInt("CurrentStage", currentStage);
        PlayerPrefs.SetInt("CurrentRound", currentRound);
        PlayerPrefs.Save();
    }

    public void LoadPlayerData()
    {
        string savedGold = PlayerPrefs.GetString("Gold", "0");
        if (!long.TryParse(savedGold, out gold)) gold = 0;
        currentTerritory = PlayerPrefs.GetInt("CurrentTerritory", 1);
        currentStage = PlayerPrefs.GetInt("CurrentStage", 1);
        currentRound = PlayerPrefs.GetInt("CurrentRound", 1);

        UIManager.Instance?.UpdateGoldDisplay(gold);
        UIManager.Instance?.UpdateHUDGold(gold);
    }

    public void SetRoundCleared(string roundId) => clearedRounds.Add(roundId);
    public bool IsRoundCleared(string roundId) => clearedRounds.Contains(roundId);
    public void SetBossDefeated(string bossId) => defeatedBosses.Add(bossId);
    public bool IsBossDefeated(string bossId) => defeatedBosses.Contains(bossId);
}