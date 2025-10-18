using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraFollow : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform target;
    public float smoothSpeed = 0.125f;
    public Vector3 offset = new Vector3(0, 5, -10);

    [Header("Bounds")]
    public bool useBounds = false;
    public float minX, maxX, minY, maxY;

    private Transform defaultTarget;
    private Vector3 defaultOffset;
    private Vector3 customOffset;

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        //AssignPlayerAsTarget();
        defaultOffset = offset;
    }

    void LateUpdate()
    {
        if (target == null && PlayerController.Instance != null)
            target = PlayerController.Instance.transform;

        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;

        if (useBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
        }

        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }

    // ============ 새로 추가 ============
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AssignPlayerAsTarget();
    }

    private void AssignPlayerAsTarget()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            target = player.transform;
            defaultTarget = target;
            Debug.Log($"[CameraFollow] Player 다시 타겟팅: {player.name} ({SceneManager.GetActiveScene().name})");
        }
        else
        {
            Debug.LogWarning($"[CameraFollow] {SceneManager.GetActiveScene().name} 씬에서 Player를 찾지 못함");
        }
    }
    // ===================================

    public void SetTarget(Transform newTarget, bool isNPC = false, bool isStorage = false)
    {
        target = newTarget;

        if (isStorage)
        {
            offset = new Vector3(defaultOffset.x - 5, 1f, defaultOffset.z + 4.8f);
            transform.rotation = Quaternion.Euler(9, 90, 0);
        }
        else if (isNPC)
        {
            offset = new Vector3(+0.8f, defaultOffset.y, defaultOffset.z + 3);
            transform.rotation = Quaternion.identity;
        }
        else
        {
            offset = defaultOffset;
            transform.rotation = Quaternion.identity;
        }
    }

    public void ResetTarget()
    {
        target = defaultTarget;
        offset = defaultOffset;
        transform.rotation = Quaternion.Euler(0, 0, 0);
    }
}
