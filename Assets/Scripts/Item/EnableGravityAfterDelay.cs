using UnityEngine;

public class EnableGravityAfterDelay : MonoBehaviour
{
    [SerializeField] private float delay = 0.05f;

    private void OnEnable()
    {
        var rb = GetComponent<Rigidbody>();
        if (rb != null) StartCoroutine(EnableLater(rb));
    }

    private System.Collections.IEnumerator EnableLater(Rigidbody rb)
    {
        yield return new WaitForSeconds(delay);
        rb.useGravity = true;
    }
}
