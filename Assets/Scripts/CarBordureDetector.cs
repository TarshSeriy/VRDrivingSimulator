using UnityEngine;

public class CarBordureDetector : MonoBehaviour
{
    [Tooltip("Радиус капсулы — полуширина машины")]
    public float capsuleRadius = 0.85f;
    [Tooltip("Смещение центра вниз")]
    public float centerOffsetY = -0.7f;
    [Tooltip("Половина длины машины (от центра до края)")]
    public float halfLength = 1.8f;
    public float minSpeed = 0.5f;

    private float _lastErrorTime = -10f;
    private float _errorCooldown = 2f;
    private Rigidbody _rb;

    void Start()
    {
        _rb = GetComponentInParent<Rigidbody>();
        if (_rb == null) _rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (Time.time - _lastErrorTime < _errorCooldown) return;
        if (_rb == null || _rb.linearVelocity.magnitude < minSpeed) return;

        // Две точки капсулы — спереди и сзади машины
        Vector3 center = transform.position + transform.up * centerOffsetY;
        Vector3 pointA = center + transform.forward * halfLength;
        Vector3 pointB = center - transform.forward * halfLength;

        Collider[] hits = Physics.OverlapCapsule(pointA, pointB, capsuleRadius);

        foreach (var hit in hits)
        {
            if (!hit.gameObject.name.StartsWith("Bordure_")) continue;
            _lastErrorTime = Time.time;
            ExamManager.Instance?.AddError("Касание бордюра");
            Debug.Log($"CarBordureDetector: касание {hit.gameObject.name}");
            break;
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector3 center = transform.position + transform.up * centerOffsetY;
        Vector3 pointA = center + transform.forward * halfLength;
        Vector3 pointB = center - transform.forward * halfLength;

        Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
        Gizmos.DrawWireSphere(pointA, capsuleRadius);
        Gizmos.DrawWireSphere(pointB, capsuleRadius);
        Gizmos.DrawLine(pointA + transform.right * capsuleRadius,
                        pointB + transform.right * capsuleRadius);
        Gizmos.DrawLine(pointA - transform.right * capsuleRadius,
                        pointB - transform.right * capsuleRadius);
    }
}