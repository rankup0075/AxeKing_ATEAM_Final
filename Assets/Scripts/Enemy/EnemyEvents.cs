using System;

public static class EnemyEvents
{
    public static event Action<EnemyHealth> OnEnemyFocus;
    public static event Action<EnemyHealth> OnEnemyDeath;

    public static void FocusEnemy(EnemyHealth e) => OnEnemyFocus?.Invoke(e);
    public static void EnemyDied(EnemyHealth e) => OnEnemyDeath?.Invoke(e);
}
