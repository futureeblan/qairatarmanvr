using UnityEngine;
using TMPro;

public class DesktopHint : MonoBehaviour
{
    public TextMeshProUGUI monitorText; // Ссылка на текст экрана
    public string errorText = "SYSTEM ERROR: DATA CORRUPTED";
    public string hintText = "LOG: Next Door ID = 7734"; // Твоя подсказка

    void Start()
    {
        // В начале игры показываем ошибку
        monitorText.text = errorText;
        monitorText.color = Color.red;
    }

    // Метод для кнопки
    public void RevealHint()
    {
        // При нажатии показываем пароль/подсказку
        monitorText.text = hintText;
        monitorText.color = Color.green;

        Debug.Log("Игрок нашел подсказку на компьютере!");

        // Тут можно добавить звук "удачного взлома"
    }
}