using UnityEngine;
using UnityEngine.UI; // Обязательно добавляем для работы с Image

/// <summary>
/// Расширение для CarController — добавляет поворотники, скорость и направление.
/// ВАЖНО: этот скрипт нужно прикрепить к той же машине что и основной CarController,
/// либо объединить с ним если у тебя уже есть CarController.
/// </summary>
public class CarIndicators : MonoBehaviour
{
    [Header("Настройки кнопок")]
    public KeyCode leftIndicatorKey = KeyCode.Z;
    public KeyCode rightIndicatorKey = KeyCode.C;
    public KeyCode hazardLightKey = KeyCode.X; // аварийка

    [Header("Световые объекты 3D (Фары)")]
    public GameObject leftIndicatorLight;
    public GameObject rightIndicatorLight;

    [Header("UI Иконки на экране (Canvas)")]
    public Image leftUIIcon;
    public Image rightUIIcon;
    public Image hazardUIIcon;

    [Header("Настройки мигания")]
    public float blinkInterval = 0.5f;
    public Color iconOffColor = new Color(1f, 1f, 1f, 0.3f); // Выключено (полупрозрачный)
    public Color iconOnColor = new Color(1f, 1f, 1f, 1f);    // Включено (яркий)

    // Публичное состояние
    public bool LeftIndicatorOn { get; private set; }
    public bool RightIndicatorOn { get; private set; }
    public bool HazardLightsOn { get; private set; }

    private float _blinkTimer;
    private bool _blinkState;

    // Ссылка на основной скрипт машины для получения скорости
    private Car _carScript;
    public float SpeedKmh => _carScript != null && _carScript.rb != null ? _carScript.rb.linearVelocity.magnitude * 3.6f : 0f;
    public bool IsReversing => _carScript != null && _carScript.rb != null && Vector3.Dot(_carScript.rb.linearVelocity, transform.forward) < -0.5f;

    void Awake()
    {
        _carScript = GetComponent<Car>();
        if (_carScript == null) _carScript = GetComponentInParent<Car>();

        // Устанавливаем иконки в выключенное состояние при старте
        ResetUIColors();
    }

    void Update()
    {
        HandleInput();
        HandleBlink();
    }

    void HandleInput()
    {
        // Левый поворотник
        if (Input.GetKeyDown(leftIndicatorKey))
        {
            if (LeftIndicatorOn) TurnOffLeft();
            else { TurnOffRight(); TurnOnLeft(); }
        }

        // Правый поворотник
        if (Input.GetKeyDown(rightIndicatorKey))
        {
            if (RightIndicatorOn) TurnOffRight();
            else { TurnOffLeft(); TurnOnRight(); }
        }

        // Аварийка
        if (Input.GetKeyDown(hazardLightKey))
        {
            if (HazardLightsOn) TurnOffHazard();
            else TurnOnHazard();
        }
    }

    void HandleBlink()
    {
        if (!LeftIndicatorOn && !RightIndicatorOn && !HazardLightsOn) return;

        _blinkTimer += Time.deltaTime;
        if (_blinkTimer >= blinkInterval)
        {
            _blinkTimer = 0f;
            _blinkState = !_blinkState;

            // Определяем текущий цвет для UI
            Color currentUIColor = _blinkState ? iconOnColor : iconOffColor;

            // Левая сторона (и 3D свет, и UI)
            if (LeftIndicatorOn || HazardLightsOn)
            {
                if (leftIndicatorLight != null) leftIndicatorLight.SetActive(_blinkState);
                if (leftUIIcon != null) leftUIIcon.color = currentUIColor;
            }

            // Правая сторона (и 3D свет, и UI)
            if (RightIndicatorOn || HazardLightsOn)
            {
                if (rightIndicatorLight != null) rightIndicatorLight.SetActive(_blinkState);
                if (rightUIIcon != null) rightUIIcon.color = currentUIColor;
            }

            // Центральная иконка аварийки
            if (HazardLightsOn)
            {
                if (hazardUIIcon != null) hazardUIIcon.color = currentUIColor;
            }
        }
    }

    // --- Методы включения/выключения ---

    public void TurnOnLeft()
    {
        LeftIndicatorOn = true;
        _blinkTimer = 0f;
        _blinkState = true; // Сразу зажигаем при включении
        ForceUpdateLights();
        Debug.Log("CarIndicators: Левый поворотник ВКЛ");
    }

    public void TurnOffLeft()
    {
        LeftIndicatorOn = false;
        if (leftIndicatorLight != null) leftIndicatorLight.SetActive(false);
        if (leftUIIcon != null) leftUIIcon.color = iconOffColor;
        Debug.Log("CarIndicators: Левый поворотник ВЫКЛ");
    }

    public void TurnOnRight()
    {
        RightIndicatorOn = true;
        _blinkTimer = 0f;
        _blinkState = true;
        ForceUpdateLights();
        Debug.Log("CarIndicators: Правый поворотник ВКЛ");
    }

    public void TurnOffRight()
    {
        RightIndicatorOn = false;
        if (rightIndicatorLight != null) rightIndicatorLight.SetActive(false);
        if (rightUIIcon != null) rightUIIcon.color = iconOffColor;
        Debug.Log("CarIndicators: Правый поворотник ВЫКЛ");
    }

    public void TurnOnHazard()
    {
        HazardLightsOn = true;
        LeftIndicatorOn = false;
        RightIndicatorOn = false;
        _blinkTimer = 0f;
        _blinkState = true;
        ForceUpdateLights();
        Debug.Log("CarIndicators: Аварийка ВКЛ");
    }

    public void TurnOffHazard()
    {
        HazardLightsOn = false;
        if (leftIndicatorLight != null) leftIndicatorLight.SetActive(false);
        if (rightIndicatorLight != null) rightIndicatorLight.SetActive(false);
        ResetUIColors();
        Debug.Log("CarIndicators: Аварийка ВЫКЛ");
    }

    // Вспомогательный метод для сброса всех UI иконок
    private void ResetUIColors()
    {
        if (leftUIIcon != null) leftUIIcon.color = iconOffColor;
        if (rightUIIcon != null) rightUIIcon.color = iconOffColor;
        if (hazardUIIcon != null) hazardUIIcon.color = iconOffColor;
    }

    // Моментальное обновление при нажатии кнопки
    private void ForceUpdateLights()
    {
        Color currentUIColor = _blinkState ? iconOnColor : iconOffColor;

        if (LeftIndicatorOn || HazardLightsOn)
        {
            if (leftIndicatorLight != null) leftIndicatorLight.SetActive(_blinkState);
            if (leftUIIcon != null) leftUIIcon.color = currentUIColor;
        }
        if (RightIndicatorOn || HazardLightsOn)
        {
            if (rightIndicatorLight != null) rightIndicatorLight.SetActive(_blinkState);
            if (rightUIIcon != null) rightUIIcon.color = currentUIColor;
        }
        if (HazardLightsOn && hazardUIIcon != null)
        {
            hazardUIIcon.color = currentUIColor;
        }
    }
}