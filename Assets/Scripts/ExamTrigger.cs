using UnityEngine;
using System.Collections;

/// <summary>
/// Стартовая / финишная линия.
/// Работает через CarBordureDetector — проверяет пересечение капсулы машины с зоной.
/// Старт: ждёт пока машина полностью проедет линию + проверяет левый поворотник.
/// Финиш: машина въезжает + проверяет правый поворотник.
/// </summary>
public class ExamTrigger : MonoBehaviour
{
    public enum TriggerType { ExamStart, ExamFinish }

    [Header("Тип триггера")]
    public TriggerType triggerType = TriggerType.ExamStart;

    [Header("Стартовый триггер")]
    public bool requireLeftIndicator = true;

    [Header("Финишный триггер")]
    public bool requireRightIndicator = true;

    // Состояние
    private bool _triggered = false;
    private bool _carInside = false;
    private CarBordureDetector _detector;
    private CarIndicators _indicators;

    void Start()
    {
        // Находим машину автоматически
        _detector = FindFirstObjectByType<CarBordureDetector>();
        _indicators = FindFirstObjectByType<CarIndicators>();
    }

    void Update()
    {
        if (_triggered || _detector == null) return;

        bool carOverlaps = CheckCarOverlap();

        if (triggerType == TriggerType.ExamStart)
        {
            // Машина вошла в зону
            if (carOverlaps && !_carInside)
            {
                _carInside = true;
                Debug.Log("ExamTrigger: машина пересекает стартовую линию...");
            }

            // Машина полностью проехала — вышла из зоны
            if (!carOverlaps && _carInside)
            {
                _carInside = false;
                HandleStart();
            }
        }
        else // ExamFinish
        {
            if (carOverlaps && !_carInside)
            {
                _carInside = true;
                HandleFinish();
            }
        }
    }

    bool CheckCarOverlap()
    {
        if (_detector == null) return false;

        // Повторяем ту же капсулу что в CarBordureDetector
        Vector3 center = _detector.transform.position +
                         _detector.transform.up * _detector.centerOffsetY;
        Vector3 pointA = center + _detector.transform.forward * _detector.halfLength;
        Vector3 pointB = center - _detector.transform.forward * _detector.halfLength;

        // Проверяем пересечение капсулы машины с нашим Box Collider
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) return false;

        // Используем OverlapBox зоны и проверяем попадает ли капсула
        Collider[] hits = Physics.OverlapCapsule(pointA, pointB, _detector.capsuleRadius);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) return true;
        }
        return false;
    }

    void HandleStart()
    {
        if (ExamManager.Instance.State != ExamManager.ExamState.WaitingStart) return;

        // Проверяем левый поворотник
        if (requireLeftIndicator && (_indicators == null || !_indicators.LeftIndicatorOn))
        {
            Debug.Log("ExamTrigger: Старт без левого поворотника — предупреждение");
            // Не блокируем старт, просто лог
        }

        _triggered = true;
        ExamManager.Instance.StartExam();
        Debug.Log("ExamTrigger: Экзамен начался!");
    }

    void HandleFinish()
    {
        if (ExamManager.Instance.State != ExamManager.ExamState.InProgress) return;

        if (requireRightIndicator && (_indicators == null || !_indicators.RightIndicatorOn))
            ExamManager.Instance.AddError("Финиш: не включён правый поворотник");

        _triggered = true;
        ExamManager.Instance.FinishExam(true);
        Debug.Log("ExamTrigger: Финиш!");
    }

    void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) return;

        Gizmos.color = triggerType == TriggerType.ExamStart
            ? new Color(0f, 1f, 0f, 0.3f)
            : new Color(1f, 0f, 0f, 0.3f);

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(box.center, box.size);
        Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 1f);
        Gizmos.DrawWireCube(box.center, box.size);
    }
}