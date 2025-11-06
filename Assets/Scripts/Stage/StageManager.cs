using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance;

    [Header("Regions & Stages")]
    public List<RegionData> regions = new List<RegionData>();
    public List<StageData> stages = new List<StageData>();
    private Vector3 pendingSpawnPos = Vector3.zero;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    public void RefreshStageList()
    {
        stages.Clear();
        foreach (var region in regions)
        {
            if (region != null && region.stages != null)
                stages.AddRange(region.stages);
        }
        Debug.Log($"[StageManager] 전역 스테이지 리스트 갱신 완료. 총 {stages.Count}개 스테이지");
    }

    public void CompleteStage(string stageId)
    {
        var (region, stageIndex) = FindStage(stageId);
        if (region == null || stageIndex < 0) return;

        var stage = region.stages[stageIndex];
        stage.isCompleted = true;

        Debug.Log($"[StageManager] 스테이지 클리어: {stage.stageName}");

        if (stageIndex + 1 < region.stages.Count)
        {
            var nextStage = region.stages[stageIndex + 1];
            nextStage.isUnlocked = true;
            Debug.Log($"[StageManager] 다음 스테이지 해금: {nextStage.stageName}");
        }
        else
        {
            var currentRegionIndex = regions.IndexOf(region);
            if (currentRegionIndex + 1 < regions.Count)
            {
                var nextRegion = regions[currentRegionIndex + 1];
                if (nextRegion.stages.Count > 0)
                {
                    nextRegion.stages[0].isUnlocked = true;
                    nextRegion.isUnlocked = true;
                    Debug.Log($"[StageManager] 다음 영지 해금: {nextRegion.regionName} - {nextRegion.stages[0].stageName}");
                }
            }
        }
        RefreshStageList();
    }

    private (RegionData, int) FindStage(string stageId)
    {
        foreach (var region in regions)
        {
            for (int i = 0; i < region.stages.Count; i++)
            {
                if (region.stages[i].stageId == stageId)
                    return (region, i);
            }
        }
        return (null, -1);
    }

    public void EnterStage(string stageId)
    {
        if (stages == null || stages.Count == 0) RefreshStageList();

        StageData stage = stages.Find(s => s.stageId == stageId);
        if (stage == null)
        {
            Debug.LogError($"[StageManager] StageId '{stageId}'를 찾을 수 없음");
            return;
        }

        // StageSelect → Stage 진입은 항상 PlayerSpawnPoint 기준
        GameManager.Instance.BeginTransition(
            TransitionKind.FromStageSelect,
            stage.sceneName,
            null,
            "PlayerSpawnPoint"
        );

        if (string.IsNullOrEmpty(stage.sceneName))
            Debug.LogError($"[StageManager] Stage '{stage.stageName}' has invalid scene name!");

        pendingSpawnPos = stage.spawnPosition;

        SceneManager.LoadScene(stage.sceneName);
        Debug.Log($"[StageManager] EnterStage 호출됨 → {stage.stageName}");
    }

    public void ReturnToTown()
    {
        Debug.Log("[StageManager] 마을로 귀환");
        // 보스 클리어 후 귀환은 항상 Town의 ReturnPoint 기준
        GameManager.Instance.BeginTransition(TransitionKind.ReturnToTown, "Town", null, "ReturnPoint");
        SceneManager.LoadScene("Town");
    }

    public void ResetForNewGame()
    {
        if (regions == null) return;

        // 전체 초기화
        foreach (var r in regions)
        {
            r.isUnlocked = false;
            if (r?.stages == null) continue;
            foreach (var s in r.stages)
            {
                s.isUnlocked = false;
                s.isCompleted = false;
            }
        }

        // 시작 영지/스테이지만 해금 (필요에 맞게 조정)
        // === 1영지 1스테이지만 기본 해금 ===
        if (regions.Count > 0 && regions[0].stages.Count > 0)
        {
            regions[0].isUnlocked = true;
            regions[0].stages[0].isUnlocked = true;
        }

        // === 6영지는 항상 열려 있도록 설정 ===
        if (regions.Count >= 6 && regions[5].stages != null && regions[5].stages.Count > 0)
        {
            regions[5].isUnlocked = true;
            regions[5].stages[0].isUnlocked = true;
        }

        RefreshStageList();
    }

}
