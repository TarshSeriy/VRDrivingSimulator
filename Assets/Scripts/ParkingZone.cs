using UnityEngine;

/// <summary>
/// Линия фиксации парковки.
/// Проверяет реальные позиции колёс через Car.GetWheelPosition().
/// 
/// Задняя парковка:  оба задних колеса (индексы 0 и 3) над линией
/// Параллельная:     оба правых (0 и 1) или оба левых (2 и 3) над линией
/// </summary>
public class ParkingZone : MonoBehaviour
{
    public enum ParkingType { Rear, Parallel }
    public enum ParallelSide { Right, Left }

    [Header("Тип парковки")]
    public ParkingType parkingType = ParkingType.Rear;

    [Header("Для параллельной — какая сторона")]
    public ParallelSide parallelSide = ParallelSide.Right;

    [Header("Настройки")]
    public float holdTime       = 2.5f;
    public float maxSpeedToHold = 0.3f;

    // Индексы колёс: 0=RR, 1=FR, 2=FL, 3=RL
    // Задняя парковка: 0 и 3
    // Параллельная правая: 0 и 1
    // Параллельная левая: 2 и 3
    private int _wheel1Index;
    private int _wheel2Index;

    private bool _completed  = false;
    private bool _carOnLine  = false;
    private float _holdTimer = 0f;
    public float checkRadius = 3f;
    private Car _car;
    private Rigidbody _carRb;
    private BoxCollider _box;

    void Start()
    {
        _car  = FindFirstObjectByType<Car>();
        _carRb = _car != null ? _car.rb : null;
        _box  = GetComponent<BoxCollider>();

        // Определяем индексы колёс
        switch (parkingType)
        {
            case ParkingType.Rear:
                _wheel1Index = 0; // заднее правое
                _wheel2Index = 3; // заднее левое
                break;
                
            case ParkingType.Parallel:
                if (parallelSide == ParallelSide.Right)
                {
                    _wheel1Index = 0; // заднее правое
                    _wheel2Index = 1; // переднее правое
                }
                else
                {
                    _wheel1Index = 2; // переднее левое
                    _wheel2Index = 3; // заднее левое
                }
                break;
        }
    }

    void Update()
    {
        if (_completed || _car == null) return;
        if (ExamManager.Instance == null) return;

        Vector3 w1 = _car.GetWheelPosition(_wheel1Index);
        Vector3 w2 = _car.GetWheelPosition(_wheel2Index);

        if (parkingType == ParkingType.Parallel)
        {
            Vector3 localW1 = transform.InverseTransformPoint(w1);
            Vector3 localW2 = transform.InverseTransformPoint(w2);
            float halfX = transform.localScale.x * 0.5f + 0.3f;
            float halfZ = transform.localScale.z * 0.5f + 0.3f;
            bool w1in = Mathf.Abs(localW1.x) < halfX && Mathf.Abs(localW1.z) < halfZ;
            bool w2in = Mathf.Abs(localW2.x) < halfX && Mathf.Abs(localW2.z) < halfZ;
            _carOnLine = w1in && w2in;
        }
        else
        {
            float dist1 = Vector3.Distance(new Vector3(w1.x, 0, w1.z),
                                           new Vector3(transform.position.x, 0, transform.position.z));
            float dist2 = Vector3.Distance(new Vector3(w2.x, 0, w2.z),
                                           new Vector3(transform.position.x, 0, transform.position.z));
            _carOnLine = dist1 < checkRadius && dist2 < checkRadius;
        }

        if (_carOnLine)
        {
            bool isStanding = _carRb == null || _carRb.linearVelocity.magnitude <= maxSpeedToHold;
            if (isStanding)
            {
                _holdTimer += Time.deltaTime;
                if (_holdTimer >= holdTime) CompleteParking();
            }
            else _holdTimer = 0f;
        }
        else _holdTimer = 0f;
    }

    bool IsPointInBox(Vector3 worldPoint)
    {
        if (_box == null) return false;
        // Переводим в локальное пространство бокса
        Vector3 local = transform.InverseTransformPoint(worldPoint) - _box.center;
        Vector3 half  = _box.size * 0.5f;
        // Добавляем допуск по Y — колесо может быть чуть выше/ниже линии
        return Mathf.Abs(local.x) <= half.x &&
               Mathf.Abs(local.y) <= half.y + 0.5f &&
               Mathf.Abs(local.z) <= half.z;
    }

    void CompleteParking()
    {
        _completed = true;
        bool isParallel = parkingType == ParkingType.Parallel;
        ExamManager.Instance?.CompleteParking(isParallel);
        Debug.Log($"ParkingZone: {(isParallel ? "Параллельная" : "Задним ходом")} — ЗАЧТЕНО ✓");
    }

    // Визуализация в редакторе
    void OnDrawGizmos()
    {
        if (_box == null) _box = GetComponent<BoxCollider>();

        Gizmos.color = _completed
            ? new Color(0f, 1f, 0f, 0.5f)
            : (_carOnLine
                ? new Color(1f, 1f, 0f, 0.5f)
                : new Color(0f, 0.5f, 1f, 0.3f));

        Gizmos.matrix = transform.localToWorldMatrix;
        if (_box != null)
        {
            Gizmos.DrawCube(_box.center, _box.size);
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 1f);
            Gizmos.DrawWireCube(_box.center, _box.size);
        }

        // Показываем какие колёса проверяем
        if (Application.isPlaying && _car != null)
        {
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = _carOnLine ? Color.green : Color.red;
            Gizmos.DrawSphere(_car.GetWheelPosition(_wheel1Index), 0.15f);
            Gizmos.DrawSphere(_car.GetWheelPosition(_wheel2Index), 0.15f);
        }
    }
}
