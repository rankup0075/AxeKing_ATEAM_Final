using UnityEngine;

public class RoundController : MonoBehaviour
{
    [Header("라운드 설정")]
    [Tooltip("GameManager에서 라운드 상태를 구분할 고유 ID (예: Stage101_R1)")]
    public string roundId;

    [Tooltip("이 라운드에 등장하는 적들을 씬에 배치 후 드래그 드롭")]
    public GameObject[] enemies;

    [Tooltip("라운드 클리어 후 활성화될 출구 포탈")]
    public Portal exitPortal;

    private int aliveCount;

    void Start()
    {
        // 이미 클리어된 라운드라면 → 포탈 즉시 활성화
        if (exitPortal != null)
        {
            bool cleared = GameManager.Instance.IsRoundCleared(roundId);
            exitPortal.SetActiveState(cleared);
        }

        // 적 개수 카운트
        aliveCount = enemies.Length;

        // 각 적에 이벤트 연결
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;

            var ec = enemy.GetComponent<EnemyController>();
            if (ec != null)
            {
                // 보스인지 여부에 따라 구분
                if (ec.isBoss)
                    ec.onDeath += OnBossDeath;
                else
                    ec.onDeath += OnEnemyDeath;
            }
        }
    }

    void OnEnemyDeath()
    {
        aliveCount--;
        if (aliveCount <= 0) ClearRound();
    }

    void OnBossDeath()
    {
        // 보스 처치 기록
        GameManager.Instance.SetBossDefeated(roundId);
        aliveCount--;
        if (aliveCount <= 0) ClearRound();
    }

    void ClearRound()
    {
        if (exitPortal != null)
        {
            exitPortal.SetActiveState(true);
            GameManager.Instance.SetRoundCleared(roundId);
            Debug.Log($"[RoundController] {roundId} 클리어 → 포탈 활성화");
        }
    }
}
