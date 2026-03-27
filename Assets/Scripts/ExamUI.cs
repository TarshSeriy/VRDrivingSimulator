using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Events;

public class ExamUI : MonoBehaviour
{
    [Header("Top Bar")]
    public TextMeshProUGUI modeLabel;
    public TextMeshProUGUI timerText;

    [Header("Progress Dots (4 штуки, по порядку задач)")]
    public Image[] progressDots;
    public Color dotPending = new Color(0.3f, 0.5f, 0.7f, 0.4f);
    public Color dotActive = new Color(0.24f, 0.71f, 1f, 1f);
    public Color dotDone = new Color(0.24f, 1f, 0.63f, 1f);

    [Header("Notification")]
    public GameObject notifPanel;
    public TextMeshProUGUI notifText;
    public Color notifColorWarn = new Color(1f, 0.6f, 0.24f, 1f);
    public Color notifColorSuccess = new Color(0.24f, 1f, 0.63f, 1f);
    public Color notifColorError = new Color(1f, 0.3f, 0.3f, 1f);
    public float notifDuration = 3f;

    [Header("Dashboard — Speed")]
    public TextMeshProUGUI speedText;
    public Image speedGaugeArc;
    public Color arcNormal = new Color(0.24f, 0.71f, 1f, 1f);
    public Color arcWarning = new Color(1f, 0.6f, 0.24f, 1f);
    public Color arcDanger = new Color(1f, 0.3f, 0.3f, 1f);

    [Header("Dashboard — RPM")]
    public TextMeshProUGUI rpmText;
    public Image rpmBarFill;
    public Color rpmBarNormal = new Color(0.24f, 0.71f, 1f, 1f);
    public Color rpmBarHot = new Color(1f, 0.3f, 0.3f, 1f);

    [Header("Dashboard — Gear")]
    public TextMeshProUGUI gearText;
    public Image gearBadgeBorder;
    public Color gearNormalColor = new Color(0.24f, 0.71f, 1f, 1f);
    public Color gearSwitchColor = new Color(1f, 0.6f, 0.24f, 1f);

    [Header("Indicators")]
    public Image leftArrowIcon;
    public Image rightArrowIcon;
    public Image hazardIcon;
    public Image brakeLightIcon;
    public Color indicatorOnColor = new Color(1f, 0.6f, 0.24f, 1f);
    public Color indicatorOffColor = new Color(1f, 0.6f, 0.24f, 0.15f);
    public Color brakeOnColor = new Color(1f, 0.3f, 0.3f, 1f);
    public Color brakeOffColor = new Color(1f, 0.3f, 0.3f, 0.15f);

    [Header("Finish Screen")]
    public GameObject finishScreen;
    public TextMeshProUGUI finishTitle;
    public TextMeshProUGUI finishTimeText;
    public TextMeshProUGUI finishErrorsCount;
    public TextMeshProUGUI finishErrorsList;
    public Color passColor = new Color(0.24f, 1f, 0.63f, 1f);
    public Color failColor = new Color(1f, 0.3f, 0.3f, 1f);

    [Header("Ссылки")]
    public CarIndicators carIndicators;
    public Car car;

    private static readonly string[] ModeNames = {
        "В ожидании старта",
        "В движении",
        "Парковка задним ходом",
        "Параллельная парковка",
        "Аварийная остановка",
        "Экзамен завершён"
    };

    private Coroutine _notifCoroutine;
    private ExamManager _exam;

    private UnityAction<string> _errorHandler;
    private UnityAction<string> _successHandler;

    void Awake()
    {
        if (notifPanel) notifPanel.SetActive(false);
        if (finishScreen) finishScreen.SetActive(false);
        SetDotColors();

        _errorHandler = msg => ShowNotification(msg, NotifType.Error);
        _successHandler = msg => ShowNotification("✓ " + msg + " — зачтено", NotifType.Success);
    }

    void Start()
    {
        BindExam();
    }

    void BindExam()
    {
        _exam = ExamManager.Instance;
        if (_exam == null)
        {
            Debug.LogError("ExamUI: ExamManager.Instance == null");
            return;
        }

        _exam.OnExamStart.AddListener(OnExamStart);
        _exam.OnExamFinish.AddListener(OnExamFinish);
        _exam.OnError.AddListener(_errorHandler);
        _exam.OnSuccess.AddListener(_successHandler);
    }

    void OnDestroy()
    {
        if (_exam == null) return;

        _exam.OnExamStart.RemoveListener(OnExamStart);
        _exam.OnExamFinish.RemoveListener(OnExamFinish);
        _exam.OnError.RemoveListener(_errorHandler);
        _exam.OnSuccess.RemoveListener(_successHandler);
    }

    void Update()
    {
        if (_exam == null) return;

        UpdateTopBar();
        UpdateDashboard();
        UpdateIndicators();
    }

    void UpdateTopBar()
    {
        if (modeLabel)
            modeLabel.text = ModeNames[(int)_exam.State];

        if (timerText)
        {
            float t = _exam.ExamTimeLeft;
            int m = Mathf.FloorToInt(t / 60f);
            int s = Mathf.FloorToInt(t % 60f);
            timerText.text = $"{m:D2}:{s:D2}";

            if (t < 60f) timerText.color = failColor;
            else if (t < 300f) timerText.color = arcWarning;
            else timerText.color = Color.white;
        }

        UpdateDots();
    }

    void UpdateDots()
    {
        if (progressDots == null || progressDots.Length < 4) return;

        bool[] done = {
            _exam.RearParkingDone,
            _exam.ParallelParkingDone,
            _exam.RailwayCrossingDone,
            _exam.EmergencyStopDone
        };

        for (int i = 0; i < 4; i++)
        {
            if (progressDots[i] == null) continue;
            progressDots[i].color = done[i] ? dotDone : dotPending;
        }

        if (_exam.State == ExamManager.ExamState.ParkingRearActive && progressDots[0])
            progressDots[0].color = dotActive;
        if (_exam.State == ExamManager.ExamState.ParkingParallelActive && progressDots[1])
            progressDots[1].color = dotActive;
        if (_exam.State == ExamManager.ExamState.EmergencyStopActive && progressDots[3])
            progressDots[3].color = dotActive;
    }

    void SetDotColors()
    {
        if (progressDots == null) return;
        foreach (var d in progressDots)
            if (d) d.color = dotPending;
    }

    void UpdateDashboard()
    {
        if (car == null || car.rb == null) return;

        float speedKmh = car.rb.linearVelocity.magnitude * 3.6f;
        float rpm = car.e != null ? car.e.getRPM() : 0f;
        int gear = car.e != null ? car.e.getCurrentGear() : 1;
        bool switching = car.e != null && car.e.isSwitchingGears();

        if (speedText) speedText.text = Mathf.RoundToInt(speedKmh).ToString("000");

        if (speedGaugeArc)
        {
            speedGaugeArc.fillAmount = Mathf.Clamp01(speedKmh / 80f);
            speedGaugeArc.color = speedKmh > 40f ? arcDanger
                                : speedKmh > 30f ? arcWarning
                                : arcNormal;
        }

        if (rpmText) rpmText.text = Mathf.RoundToInt(rpm).ToString();
        if (rpmBarFill)
        {
            float maxRpm = car.e != null ? car.e.maxRPM : 7000f;
            float idleRpm = car.e != null ? car.e.idleRPM : 2400f;
            float frac = Mathf.Clamp01((rpm - idleRpm) / (maxRpm - idleRpm));
            rpmBarFill.fillAmount = frac;
            rpmBarFill.color = rpm > maxRpm * 0.85f ? rpmBarHot : rpmBarNormal;
        }

        if (gearText) gearText.text = gear.ToString();
        if (gearBadgeBorder)
            gearBadgeBorder.color = switching ? gearSwitchColor : gearNormalColor;
    }

    void UpdateIndicators()
    {
        if (carIndicators == null) return;

        if (brakeLightIcon)
            brakeLightIcon.color = car && car.isBraking > 0.1f ? brakeOnColor : brakeOffColor;

        if (leftArrowIcon && carIndicators.leftUIIcon == null)
            leftArrowIcon.color = carIndicators.LeftIndicatorOn || carIndicators.HazardLightsOn
                ? indicatorOnColor : indicatorOffColor;

        if (rightArrowIcon && carIndicators.rightUIIcon == null)
            rightArrowIcon.color = carIndicators.RightIndicatorOn || carIndicators.HazardLightsOn
                ? indicatorOnColor : indicatorOffColor;

        if (hazardIcon)
            hazardIcon.color = carIndicators.HazardLightsOn ? indicatorOnColor : indicatorOffColor;
    }

    public enum NotifType { Warn, Success, Error }

    public void ShowNotification(string message, NotifType type = NotifType.Warn)
    {
        if (_notifCoroutine != null) StopCoroutine(_notifCoroutine);
        _notifCoroutine = StartCoroutine(NotifRoutine(message, type));
    }

    IEnumerator NotifRoutine(string message, NotifType type)
    {
        if (notifPanel) notifPanel.SetActive(true);
        if (notifText)
        {
            notifText.text = message;
            notifText.color = type switch
            {
                NotifType.Success => notifColorSuccess,
                NotifType.Error => notifColorError,
                _ => notifColorWarn
            };
        }

        yield return new WaitForSeconds(notifDuration);

        if (notifPanel) notifPanel.SetActive(false);
    }

    void OnExamStart()
    {
        ShowNotification("Экзамен начался! Удачи", NotifType.Success);
    }

    void OnExamFinish()
    {
        if (finishScreen == null) return;
        finishScreen.SetActive(true);

        bool passed = _exam.Errors.Count == 0;

        if (finishTitle)
        {
            finishTitle.text = passed ? "СДАЛ" : "НЕ СДАЛ";
            finishTitle.color = passed ? passColor : failColor;
        }

        if (finishTimeText)
        {
            float used = _exam.examDuration - _exam.ExamTimeLeft;
            finishTimeText.text = $"{Mathf.FloorToInt(used / 60f):D2}:{Mathf.FloorToInt(used % 60f):D2}";
        }

        if (finishErrorsCount)
            finishErrorsCount.text = _exam.Errors.Count.ToString();

        if (finishErrorsList)
            finishErrorsList.text = _exam.Errors.Count > 0 ? string.Join("\n", _exam.Errors) : "";
    }
}