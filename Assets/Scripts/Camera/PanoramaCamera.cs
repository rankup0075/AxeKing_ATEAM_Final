using UnityEngine;

public class PanoramaCamera : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 10f; // 초당 회전 속도

    void Update()
    {
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
    }
}
