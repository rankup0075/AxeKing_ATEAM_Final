// ScreenLaser.cs
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider))]
public class ScreenLaser : MonoBehaviour
{
    public enum Axis { Horizontal, Vertical }

    public Axis axis = Axis.Horizontal;
    public float thickness = 1.0f;      // 레이저 굵기
    public float previewTime = 1.0f;    // 프리뷰 표시 시간
    public float fireTime = 0.6f;       // 실제 피해 시간
    public bool continuous = false;     // true=지속틱, false=한번만

    public Transform preview;           // 프리뷰 비주얼
    public Transform beam;              // 실제 발사 비주얼
    public int damage = 20;
    public float tickInterval = 0.2f;

    BoxCollider box;
    float lastTick = -999f;
    bool firing;

    void Awake()
    {
        box = GetComponent<BoxCollider>();
        box.isTrigger = true;
        preview.gameObject.SetActive(false);
        beam.gameObject.SetActive(false);
    }

    public void SetupToCameraBounds(float worldWidth = 25f)
    {
        // worldWidth: 화면을 가로지르는 전체 폭 (직접 지정)
        // 2.5D 기준 20~30 정도면 대부분 카메라 폭 이상

        if (axis == Axis.Horizontal)
        {
            // 중앙 정렬
            Vector3 pos = transform.position;
            pos.x = 0f;
            transform.position = pos;

            box.size = new Vector3(worldWidth, thickness, 1f);
            box.center = Vector3.zero;

            if (preview) preview.localScale = new Vector3(worldWidth, thickness , 1f );
            if (beam) beam.localScale = new Vector3(worldWidth, thickness, 1f );
        }
        else
        {
            box.size = new Vector3(thickness, worldWidth, 1f);
            box.center = Vector3.zero;

            if (preview) preview.localScale = new Vector3(thickness, worldWidth ,1f );
            if (beam) beam.localScale = new Vector3(thickness, worldWidth, 1f );
        }
    }


    public IEnumerator FireSequence()
    {
        if (preview) preview.gameObject.SetActive(false);
        if (beam) beam.gameObject.SetActive(true);
        firing = true;
        yield return new WaitForSeconds(fireTime);
        firing = false;
        if (beam) beam.gameObject.SetActive(false);
        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!firing || continuous) return;
        if (other.TryGetComponent<PlayerHealth>(out var ph))
            ph.TakeDamage(damage);
    }

    void OnTriggerStay(Collider other)
    {
        if (!firing || !continuous) return;
        if (!other.TryGetComponent<PlayerHealth>(out var ph)) return;

        if (Time.time - lastTick >= tickInterval)
        {
            ph.TakeDamage(damage);
            lastTick = Time.time;
        }
    }
}
