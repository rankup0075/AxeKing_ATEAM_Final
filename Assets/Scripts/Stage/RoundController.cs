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
        // 1 씬 내 모든 EnemyController / SlimeController / GoblinController / TurtleShellController 자동 탐색
        if (enemies == null || enemies.Length == 0)
        {
            var foundEnemies = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            var list = new System.Collections.Generic.List<GameObject>();

            foreach (var comp in foundEnemies)
            {
                if (comp is EnemyController || comp is SlimeController || comp is GoblinController || comp is TurtleShellController)
                    list.Add(comp.gameObject);
            }
            enemies = list.ToArray();
        }

        // 2 이미 클리어된 라운드라면 포탈 즉시 열기
        if (exitPortal != null)
        {
            bool cleared = GameManager.Instance.IsRoundCleared(roundId);
            exitPortal.SetActiveState(cleared);
        }

        aliveCount = enemies.Length;

        //  3 적마다 onDeath 이벤트 연결
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;

            // EnemyController
            var ec = enemy.GetComponent<EnemyController>();
            if (ec != null)
            {
                if (ec.isBoss)
                    ec.onDeath += OnBossDeath;
                else
                    ec.onDeath += OnEnemyDeath;
                continue;
            }

            // SlimeController
            var sc = enemy.GetComponent<SlimeController>();
            if (sc != null)
            {
                sc.onDeath += OnEnemyDeath;
                continue;
            }

            // GoblinController
            var gc = enemy.GetComponent<GoblinController>();
            if (gc != null)
            {
                gc.onDeath += OnEnemyDeath;
                continue;
            }

            // TurtleShellController
            var tc = enemy.GetComponent<TurtleShellController>();
            if (tc != null)
            {
                tc.onDeath += OnEnemyDeath;
                continue;
            }
        }

        Debug.Log($"[RoundController] 라운드 시작: {roundId}, 적 {aliveCount}명 등록 완료");
    }

    // 일반 적 사망 시
    void OnEnemyDeath()
    {
        aliveCount--;
        Debug.Log($"[RoundController] 적 처치됨 → 남은 수 {aliveCount}");

        if (aliveCount <= 0)
            ClearRound();
    }

    // 보스 사망 시
    void OnBossDeath()
    {
        GameManager.Instance.SetBossDefeated(roundId);
        aliveCount--;
        Debug.Log($"[RoundController] 보스 처치됨 → 남은 수 {aliveCount}");

        if (aliveCount <= 0)
            ClearRound();
    }

    // 포탈 활성화
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
