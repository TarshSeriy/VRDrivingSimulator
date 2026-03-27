using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

/// <summary>
/// Главный менеджер экзамена. Хранит состояние, таймер, ошибки.
/// Все остальные скрипты обращаются сюда через ExamManager.Instance
/// </summary>
public class ExamManager : MonoBehaviour
{
    public static ExamManager Instance { get; private set; }

    [Header("Настройки экзамена")]
    public float examDuration       = 1200f; // 20 минут
    public float parkingTimeLimit   = 135f;  // 2 мин 15 сек на парковку
    public float maxSpeedKmh        = 40f;   // максимальная скорость

    [Header("Ссылка на машину")]
    public Car car;

    // ——— Состояние ———
    public enum ExamState
    {
        WaitingStart,       // ждём включения поворотника на старте
        InProgress,         // экзамен идёт
        ParkingRearActive,  // выполняется парковка задним ходом
        ParkingParallelActive, // выполняется параллельная парковка
        EmergencyStopActive,// аварийная остановка
        Finished            // экзамен завершён
    }
    public ExamState State { get; private set; } = ExamState.WaitingStart;

    // ——— Таймеры ———
    public float ExamTimeLeft       { get; private set; }
    public float ParkingTimeUsed    { get; private set; }
    private float _parkingStartTime;

    // ——— Прогресс упражнений ———
    public bool RearParkingDone     { get; private set; }
    public bool ParallelParkingDone { get; private set; }
    public bool RailwayCrossingDone { get; private set; }
    public bool EmergencyStopDone   { get; private set; }

    // ——— Ошибки ———
    [HideInInspector] // Добавь это, чтобы Инспектор не падал
    public List<string> Errors { get; private set; } = new List<string>();
    public bool HasFailed => Errors.Count > 0 && IsFatalError();

    // ——— События ———
    public UnityEvent OnExamStart       = new UnityEvent();
    public UnityEvent OnExamFinish      = new UnityEvent();
    public UnityEvent<string> OnError   = new UnityEvent<string>();
    public UnityEvent<string> OnSuccess = new UnityEvent<string>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (State == ExamState.WaitingStart || State == ExamState.Finished) return;

        // Обратный отсчёт
        ExamTimeLeft -= Time.deltaTime;
        if (ExamTimeLeft <= 0f)
        {
            ExamTimeLeft = 0f;
            AddError("Истекло время экзамена (20 минут)");
            FinishExam(false);
            return;
        }

        // Таймер парковки
        if (State == ExamState.ParkingRearActive || State == ExamState.ParkingParallelActive)
        {
            ParkingTimeUsed = Time.time - _parkingStartTime;
            if (ParkingTimeUsed > parkingTimeLimit)
            {
                string name = State == ExamState.ParkingRearActive
                    ? "парковка задним ходом" : "параллельная парковка";
                AddError($"Превышено время на упражнение: {name}");
                FinishExam(false);
            }
        }

        // Контроль скорости
        if (car != null && car.rb != null)
        {
            float speedKmh = car.rb.linearVelocity.magnitude * 3.6f;
            if (speedKmh > maxSpeedKmh)
                AddError($"Превышение скорости (макс {maxSpeedKmh} км/ч)");
        }
    }

    // ——— Публичные методы ———

    public void StartExam()
    {
        if (State != ExamState.WaitingStart) return;
        State = ExamState.InProgress;
        ExamTimeLeft = examDuration;
        Errors.Clear();
        OnExamStart.Invoke();
        Debug.Log("ExamManager: Экзамен начался!");
    }

    public void StartParking(bool isParallel)
    {
        if (State != ExamState.InProgress) return;
        State = isParallel ? ExamState.ParkingParallelActive : ExamState.ParkingRearActive;
        _parkingStartTime = Time.time;
        ParkingTimeUsed = 0f;
        Debug.Log($"ExamManager: Начало парковки ({(isParallel ? "параллельная" : "задним ходом")})");
    }

    public void CompleteParking(bool isParallel)
    {
        if (isParallel)
            ParallelParkingDone = true;
        else
            RearParkingDone = true;

        State = ExamState.InProgress;
        string name = isParallel ? "Параллельная парковка" : "Парковка задним ходом";
        OnSuccess.Invoke(name);
        Debug.Log($"ExamManager: {name} — ЗАЧТЕНО");
    }

    public void CompleteRailwayCrossing()
    {
        RailwayCrossingDone = true;
        OnSuccess.Invoke("ЖД переезд");
    }

    public void StartEmergencyStop()
    {
        if (State != ExamState.InProgress) return;
        State = ExamState.EmergencyStopActive;
    }

    public void CompleteEmergencyStop()
    {
        EmergencyStopDone = true;
        State = ExamState.InProgress;
        OnSuccess.Invoke("Аварийная остановка");
    }

    public void AddError(string message)
    {
        if (Errors.Contains(message)) return; // не дублируем одну ошибку
        Errors.Add(message);
        OnError.Invoke(message);
        Debug.LogWarning($"ExamManager ОШИБКА: {message}");
    }

    public void FinishExam(bool success)
    {
        if (State == ExamState.Finished) return;
        State = ExamState.Finished;

        // Проверяем все ли упражнения выполнены
        if (!RearParkingDone)     AddError("Не выполнена парковка задним ходом");
        if (!ParallelParkingDone) AddError("Не выполнена параллельная парковка");
        if (!RailwayCrossingDone) AddError("Не выполнен ЖД переезд");
        if (!EmergencyStopDone)   AddError("Не выполнена аварийная остановка");

        OnExamFinish.Invoke();
        bool passed = success && Errors.Count == 0;
        Debug.Log($"ExamManager: Экзамен завершён. Результат: {(passed ? "СДАЛ" : "НЕ СДАЛ")}");
        if (!passed)
            foreach (var e in Errors)
                Debug.Log($"  — {e}");
    }

    bool IsFatalError() => Errors.Count > 0; // все ошибки фатальны пока
}
