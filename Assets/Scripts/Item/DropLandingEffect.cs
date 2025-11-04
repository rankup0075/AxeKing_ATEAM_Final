using UnityEngine;

public class DropLandingEffect : MonoBehaviour
{
    private GameObject effectPrefab;
    private bool effectPlayed = false;

    public void Init(GameObject prefab)
    {
        effectPrefab = prefab;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (effectPlayed) return;

        // Ground나 Terrain 태그에 닿았을 때만 반짝임
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Terrain"))
        {
            effectPlayed = true;
            if (effectPrefab != null)
            {
                GameObject effect = Instantiate(effectPrefab, transform.position, Quaternion.identity);
                Destroy(effect, 1.5f); // 1.5초 후 자동 삭제
            }
        }
    }
}
