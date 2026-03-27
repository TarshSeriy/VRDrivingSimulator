using UnityEngine;
using System.Collections;

/// <summary>
/// Случайная аварийная остановка на линии разгона.
/// Срабатывает в случайный момент когда машина въезжает в зону.
/// Студент должен: остановиться + включить аварийку + подождать разрешения.
/// </summary>
public class EmergencyStop : MonoBehaviour
{
    [Header("Настройки")]
    public float minDelay     = 3f;   // минимальная задержка после въезда
    public float maxDelay     = 10f;  // максимальная задержка
    public float maxStopTime  = 5f;   // сколько ждать пока остановится
    public float maxStopSpeed = 0.5f; // скорость «стоит»
    public float resumeDelay  = 3f;   // через сколько разрешаем продолжить

    private bool _triggered  = false;
    private bool _completed  = false;
    private CarIndicators _carIndicators;
    private Rigidbody     _carRb;

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"EmergencyStop: вошёл объект {other.gameObject.name}");
        if (_triggered || _completed) return;
        if (other.GetComponentInParent<CarBordureDetector>() == null)
        {
            Debug.Log("EmergencyStop: нет CarBordureDetector — пропускаем");
            return;
        }
        // остальной код...
        if (_triggered || _completed) return;
        if (other.GetComponentInParent<CarBordureDetector>() == null) return;

        _carIndicators = other.GetComponentInParent<CarIndicators>();
        _carRb         = other.GetComponentInParent<Rigidbody>();

        _triggered = true;
        StartCoroutine(TriggerEmergency());
    }

    IEnumerator TriggerEmergency()
    {
        float delay = Random.Range(minDelay, maxDelay);
        yield return new WaitForSeconds(delay);

        if (ExamManager.Instance == null ||
            ExamManager.Instance.State != ExamManager.ExamState.InProgress)
            yield break;

        Debug.Log("EmergencyStop: АВАРИЙНАЯ ОСТАНОВКА!");
        ExamManager.Instance.StartEmergencyStop();

        // Ждём остановки
        float elapsed = 0f;
        bool stopped  = false;
        while (elapsed < maxStopTime)
        {
            if (_carRb != null && _carRb.linearVelocity.magnitude <= maxStopSpeed)
            {
                stopped = true;
                break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!stopped)
            ExamManager.Instance.AddError("Аварийная остановка: не остановился вовремя");

        yield return new WaitForSeconds(0.5f);

        if (_carIndicators != null && !_carIndicators.HazardLightsOn)
            ExamManager.Instance.AddError("Аварийная остановка: не включил аварийные огни");

        yield return new WaitForSeconds(resumeDelay);

        _completed = true;
        ExamManager.Instance.CompleteEmergencyStop();
        Debug.Log("EmergencyStop: Можно продолжать движение");
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 1f, 0.2f);
        Collider col = GetComponent<Collider>();
        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(1f, 0f, 1f, 0.8f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
