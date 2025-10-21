// CylindricalExplosion.cs
using UnityEngine;
using System.Collections;

public class CylindricalExplosion : MonoBehaviour
{
    [Header("Visuals")]
    public Transform preview;      // 범위 표시용 이펙트
    public GameObject explosion;   // 실제 폭발 (CapsuleCollider + DamageOnTrigger)
    public float autoDestroyAfter = 1f;

    void Awake()
    {
        // explosion은 기본 꺼진 상태로 시작
        if (explosion) explosion.SetActive(false);
        if (preview) preview.gameObject.SetActive(false);
    }

    public void ConfigureShape()
    {
        if (!preview || !explosion) return;

        // preview의 현재 스케일을 그대로 사용
        Vector3 scale = preview.localScale;

        // Collider를 preview 크기에 맞게 자동 세팅
        var cap = explosion.GetComponent<CapsuleCollider>();
        if (cap)
        {
            cap.isTrigger = true;
            cap.direction = 1; // Y축
            cap.radius = scale.x * 0.5f; // X스케일 기준 반경
            cap.height = scale.y;        // Y스케일 기준 높이
        }
    }

    public IEnumerator ShowThenExplode(float previewDuration)
    {
        if (preview) preview.gameObject.SetActive(true);
        yield return new WaitForSeconds(previewDuration);
        if (preview) preview.gameObject.SetActive(false);

        if (explosion) explosion.SetActive(true);
        Destroy(gameObject, autoDestroyAfter);
    }
}
