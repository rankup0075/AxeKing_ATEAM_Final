using UnityEngine;

public class RoundController : MonoBehaviour
{
    [Header("라운드 설정")]
    [Tooltip("GameManager에서 라운드 상태를 구분할 고유 ID (예: Stage101_R1)")]
    public string roundId;

    [Tooltip("이 라운드에 등장하는 적들을 씬에서 자동으로 찾거나 직접 지정")]
    public GameObject[] enemies;

    [Tooltip("라운드 클리어 후 활성화될 출구 포탈")]
    public Portal exitPortal;

    private int aliveCount;

    void Start()
    {
        // 1. 씬 내에서 적 탐색
        if (enemies == null || enemies.Length == 0)
        {
            var foundEnemies = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            var list = new System.Collections.Generic.List<GameObject>();

            foreach (var comp in foundEnemies)
            {
                if (comp is EnemyController || comp is SlimeController || comp is GoblinController ||
                    comp is TurtleShellController || comp is IceSpiritController) // ✅ 추가
                    list.Add(comp.gameObject);
            }
            enemies = list.ToArray();
        }

        // 2. 클리어 여부 확인
        if (exitPortal != null)
        {
            bool cleared = GameManager.Instance.IsRoundCleared(roundId);
            exitPortal.SetActiveState(cleared);
        }

        aliveCount = enemies.Length;

        // 3. 사망 이벤트 등록
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;

            if (enemy.TryGetComponent(out EnemyController ec))
            {
                if (ec.isBoss) ec.onDeath += OnBossDeath;
                else ec.onDeath += OnEnemyDeath;
                continue;
            }

            if (enemy.TryGetComponent(out SlimeController sc))
            {
                sc.onDeath += OnEnemyDeath;
                continue;
            }

            if (enemy.TryGetComponent(out GoblinController gc))
            {
                gc.onDeath += OnEnemyDeath;
                continue;
            }

            if (enemy.TryGetComponent(out TurtleShellController tc))
            {
                tc.onDeath += OnEnemyDeath;
                continue;
            }

            if (enemy.TryGetComponent(out IceSpiritController ic)) // ✅ 추가
            {
                ic.onDeath += OnEnemyDeath;
                continue;
            }
        }

        Debug.Log($"[RoundController] 라운드 시작: {roundId}, 적 {aliveCount}명 등록 완료");
    }

    // ===================
    // 이벤트 콜백
    // ===================
    void OnEnemyDeath()
    {
        aliveCount--;
        Debug.Log($"[RoundController] 적 처치됨 → 남은 수 {aliveCount}");

        if (aliveCount <= 0)
            ClearRound();
    }

    void OnBossDeath()
    {
        GameManager.Instance.SetBossDefeated(roundId);
        aliveCount--;
        Debug.Log($"[RoundController] 보스 처치됨 → 남은 수 {aliveCount}");

        if (aliveCount <= 0)
            ClearRound();
    }

    // ===================
    // 클리어 처리
    // ===================
    void ClearRound()
    {
        if (exitPortal != null)
        {
            exitPortal.SetActiveState(true);
            GameManager.Instance.SetRoundCleared(roundId);
            Debug.Log($"[RoundController] {roundId} 클리어 → 포탈 활성화 완료");

            var sm = StageManager.Instance;
            if (sm != null)
            {
                string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                var stage = sm.stages.Find(s => s.sceneName == sceneName);
                if (stage != null)
                {
                    sm.CompleteStage(stage.stageId);
                    Debug.Log($"[RoundController] StageManager에 '{stage.stageName}' 클리어 반영");
                }
            }
        }
        else
        {
            Debug.LogWarning($"[RoundController] {roundId} 클리어 → 포탈이 연결되지 않음");
        }
    }
}
