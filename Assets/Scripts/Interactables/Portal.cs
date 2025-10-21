using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    [Header("Portal Settings")]
    public string targetScene;
    public string portalID;
    public PortalType portalType;
    public bool isUnlocked = true;
    public PortalDirection portalDirection = PortalDirection.Forward;

    [Header("Shop Portal Settings")]
    public PotionShopUI potionShopUI;
    public EquipmentShopUI equipmentShopUI;
    public WarehouseUI warehouseUI;

    [Header("Stage Clear Options")]
    [SerializeField] private bool completeStageOnUse = false;
    [SerializeField] private string stageIdToComplete = "";

    [Header("Portal Visuals")]
    [SerializeField] private ParticleSystem portalParticles;
    [SerializeField] private Light portalLight;

    [Header("Spawn Point")]
    public Transform spawnPoint;

    [Header("Spawn Override")]
    public string targetSpawnName;        // 예: "PlayerSpawnPoint", "ReturnPoint", etc.

    [Header("Shop → Town Exit")]
    public string townExitPointName;      // 예: "EquipmentShopExitPoint" / "AlchemistShopExitPoint" / "WarehouseExitPoint"

    public enum PortalDirection { Forward, Backward }

    public static void ClearBusy() { busy = false; }
    public enum PortalType
    {
        SceneTransition,
        StageSelect,
        Shop,
        QuestBoard,
        PotionShop,
        EquipmentShop,
        WareHouseChest,
        SaveUIEntryPortal,
        RoundTransition,
        StageClear
    }

    private bool playerInRange = false;
    private static bool busy = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            ShowInteractionUI(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            ShowInteractionUI(false);
        }
    }

    public void SetActiveState(bool active)
    {
        isUnlocked = active;
        if (portalParticles != null)
        {
            if (active && !portalParticles.isPlaying) portalParticles.Play();
            else if (!active && portalParticles.isPlaying) portalParticles.Stop();
        }
        if (portalLight != null) portalLight.enabled = active;
    }

    public void Interact()
    {
        if (!playerInRange || !isUnlocked || busy) return;
        busy = true;

        var player = PlayerController.Instance;
        if (player != null)
        {
            player.canControl = false;
            player.StopImmediately();
        }

        switch (portalType)
        {
            case PortalType.SceneTransition:
                TransitionToScene();
                break;

            case PortalType.StageSelect:
                UIManager.Instance?.OpenStageSelectPanel();
                busy = false;
                break;

            case PortalType.QuestBoard:
                UIManager.Instance?.OpenQuestBoardUI();
                busy = false;
                break;

            case PortalType.PotionShop:
                if (potionShopUI != null) potionShopUI.OpenShop();
                else Debug.LogWarning("[Portal] PotionShopUI 미연결");
                busy = false;
                break;

            case PortalType.EquipmentShop:
                if (equipmentShopUI != null) equipmentShopUI.OpenShop();
                else Debug.LogWarning("[Portal] EquipmentShopUI 미연결");
                busy = false;
                break;

            case PortalType.SaveUIEntryPortal:
                UIManager.Instance?.OpenSaveSelectUI(true); // true = 저장모드
                busy = false;
                break;

            case PortalType.WareHouseChest:
                if (warehouseUI != null) warehouseUI.OpenWarehouse();
                else Debug.LogWarning("[Portal] warehouseUI 미연결");
                busy = false;
                break;

            case PortalType.RoundTransition:
                {
                    string spawnName;
                    if (portalDirection == PortalDirection.Forward)
                        spawnName = $"{targetScene}EntryPortal"; // 예: Stage101_R2EntryPortal
                    else
                    {
                        // 역방향 → 현재(출발) 라운드 EntryPortal
                        string currentScene = SceneManager.GetActiveScene().name;
                        spawnName = $"{currentScene}EntryPortal"; // ex: Stage101_R3EntryPortal
                    }

                    var kind = (portalDirection == PortalDirection.Backward)
                        ? TransitionKind.PortalBackward
                        : TransitionKind.PortalForward;

                    GameManager.Instance.BeginTransition(kind, targetScene, portalID, spawnName);
                    Time.timeScale = 1f;
                    SceneManager.LoadScene(targetScene);
                }
                break;

            case PortalType.StageClear:
                if (completeStageOnUse && StageManager.Instance != null && !string.IsNullOrEmpty(stageIdToComplete))
                {
                    StageManager.Instance.CompleteStage(stageIdToComplete);
                    GameManager.Instance?.SavePlayerData();
                }
                UIManager.Instance?.OpenStageSelectPanel();
                busy = false;
                break;
        }

        busy = false;
    }

    void TransitionToScene()
    {
        if (string.IsNullOrEmpty(targetScene)) { busy = false; return; }

        string spawnName = null;

        // Scene 간 포탈은 모두 이 분기로 통일
        if (portalType == PortalType.SceneTransition)
        {
            if (targetScene == "Town" && !string.IsNullOrEmpty(townExitPointName))
                spawnName = townExitPointName; // → Town으로 복귀
            else if (!string.IsNullOrEmpty(targetSpawnName))
                spawnName = targetSpawnName;   // Town → Equipment, Alchemist, Warehouse 등
        }

        GameManager.Instance.BeginTransition(
            portalDirection == PortalDirection.Backward
                ? TransitionKind.PortalBackward
                : TransitionKind.PortalForward,
            targetScene,
            portalID,
            spawnName
        );
        SceneManager.LoadScene(targetScene);
    }


    void ShowInteractionUI(bool show)
    {
        string promptText = GetInteractionPrompt();
        UIManager.Instance?.ShowInteractionPrompt(show, promptText);
    }

    string GetInteractionPrompt()
    {
        switch (portalType)
        {
            case PortalType.PotionShop: return "↑ 키를 눌러 포션 상점 열기";
            case PortalType.EquipmentShop: return "↑ 키를 눌러 장비 상점 열기";
            case PortalType.QuestBoard: return "↑ 키를 눌러 퀘스트 확인";
            case PortalType.StageSelect: return "↑ 키를 눌러 스테이지 선택";
            default: return "↑ 키를 눌러 상호작용";
        }
    }
}
