using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Минималистичная панель статуса экзамена (Планшет).
/// Показывает только список упражнений и статус их выполнения (чекбоксы).
/// </summary>
public class StatusPanel : MonoBehaviour
{
    [Header("Экран планшета")]
    public GameObject statusPanel;

    [Header("Тексты упражнений")]
    public TextMeshProUGUI rearParkingText;
    public TextMeshProUGUI parallelParkingText;
    public TextMeshProUGUI railwayText;
    public TextMeshProUGUI emergencyText;

    [Header("Иконки чекбоксов (Image)")]
    public Image rearParkingCheckbox;
    public Image parallelParkingCheckbox;
    public Image railwayCheckbox;
    public Image emergencyCheckbox;

    [Header("Настройки картинок")]
    public Sprite checkmarkSprite; // Перетащи сюда скачанную зеленую галочку
    public Sprite emptyBoxSprite;  // Сюда можно кинуть стандартный UISprite (или оставить пустым для простого квадрата)

    [Header("Цвета текста и пустых чекбоксов")]
    public Color textDoneColor = Color.white;
    public Color textNotDoneColor = new Color(0.8f, 0.8f, 0.8f); // Тусклый белый
    public Color boxNotDoneColor = new Color(1f, 1f, 1f, 0.2f);  // Полупрозрачный пустой квадрат

    void Start()
    {
        if (statusPanel != null)
            statusPanel.SetActive(true);
    }

    void Update()
    {
        if (statusPanel != null && statusPanel.activeSelf)
        {
            UpdatePanel();
        }
    }

    void UpdatePanel()
    {
        if (ExamManager.Instance == null) return;

        var exam = ExamManager.Instance;

        // Обновляем только упражнения
        SetStatus(rearParkingText, rearParkingCheckbox, exam.RearParkingDone);
        SetStatus(parallelParkingText, parallelParkingCheckbox, exam.ParallelParkingDone);
        SetStatus(railwayText, railwayCheckbox, exam.RailwayCrossingDone);
        SetStatus(emergencyText, emergencyCheckbox, exam.EmergencyStopDone);
    }

    // Логика переключения галочки и цвета текста
    void SetStatus(TextMeshProUGUI label, Image checkbox, bool done)
    {
        // Меняем цвет текста
        if (label != null)
        {
            label.color = done ? textDoneColor : textNotDoneColor;
        }

        // Меняем картинку в чекбоксе
        if (checkbox != null)
        {
            if (done)
            {
                if (checkmarkSprite != null) checkbox.sprite = checkmarkSprite;
                checkbox.color = Color.white; // Чтобы зеленая галочка отображалась в своих родных цветах
            }
            else
            {
                checkbox.sprite = emptyBoxSprite;
                checkbox.color = boxNotDoneColor; // Делаем пустой квадрат полупрозрачным
            }
        }
    }
}