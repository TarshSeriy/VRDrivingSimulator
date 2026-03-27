using UnityEngine;
using System.Collections;

/// <summary>
/// ЖД переезд.
/// Машина въезжает в триггер → должна остановиться → подождать поезд → проехать.
/// </summary>
public class RailwayCrossing : MonoBehaviour
{
    [Header("Настройки")]
    public float trainPassTime   = 5f;   // время прохождения поезда
    public float maxStopSpeed    = 0.5f; // скорость «стоит»
    public float stopWaitTime    = 2f;   // сколько ждать после остановки

    [Header("Объект поезда (необязательно)")]
    public GameObject trainObject;
    public Transform  trainStart;
    public Transform  trainEnd;
    public float      trainSpeed = 20f;

    private bool _completed  = false;
    private bool _carInZone  = false;
    private Rigidbody _carRb;

    void Start()
    {
        if (trainObject != null)
            trainObject.SetActive(false);

        // Запускаем поезд по расписанию
        StartCoroutine(TrainLoop());
    }

    IEnumerator TrainLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(8f, 20f));
            yield return StartCoroutine(RunTrain());
        }
    }

    IEnumerator RunTrain()
    {
        if (trainObject == null) yield break;

        trainObject.SetActive(true);
        if (trainStart != null)
            trainObject.transform.position = trainStart.position;

        float elapsed = 0f;
        while (elapsed < trainPassTime)
        {
            elapsed += Time.deltaTime;
            if (trainEnd != null)
                trainObject.transform.position = Vector3.MoveTowards(
                    trainObject.transform.position,
                    trainEnd.position,
                    trainSpeed * Time.deltaTime);
            yield return null;
        }

        trainObject.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (_completed) return;
        if (other.GetComponentInParent<CarBordureDetector>() == null) return;

        _carRb = other.GetComponentInParent<Rigidbody>();
        _carInZone = true;
        StartCoroutine(CheckCrossing());
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<CarBordureDetector>() == null) return;
        _carInZone = false;
    }

    IEnumerator CheckCrossing()
    {
        Debug.Log("RailwayCrossing: машина подъехала к переезду");

        // Ждём остановки — максимум 5 секунд
        float elapsed = 0f;
        bool stopped = false;

        while (elapsed < 5f)
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
        {
            ExamManager.Instance?.AddError("ЖД переезд: не остановился перед стоп-знаком");
            yield break;
        }

        Debug.Log("RailwayCrossing: машина остановилась — ждём поезд...");

        float standTimer = 0f;
        while (standTimer < stopWaitTime)
        {
            // Уехал из зоны раньше времени — ошибка
            if (!_carInZone)
            {
                ExamManager.Instance?.AddError("ЖД переезд: уехал не дождавшись разрешения");
                Debug.Log("RailwayCrossing: ОШИБКА — уехал раньше времени");
                yield break;
            }

            if (_carRb != null && _carRb.linearVelocity.magnitude > maxStopSpeed)
                standTimer = 0f; // тронулся — сброс таймера
            else
                standTimer += Time.deltaTime;

            yield return null;
        }

        _completed = true;
        ExamManager.Instance?.CompleteRailwayCrossing();
        Debug.Log("RailwayCrossing: ЖД переезд — ЗАЧТЕНО ✓");
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
