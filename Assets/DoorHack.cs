using UnityEngine;
using UnityEngine.Events;

public class DoorHack : MonoBehaviour
{
    public GameObject door; // Ссылка на модель двери, которую будем открывать
    public UnityEvent onHackSuccess; // Событие при успехе

    public void CheckCode(bool playerChoice)
    {
        // Наша задача — поставить door_access = true
        if (playerChoice == true)
        {
            Debug.Log("Взлом успешен!");
            OpenDoor();
        }
        else
        {
            Debug.Log("Ошибка! Сингулярность заметила тебя!");
            // Тут можно добавить эффект глитча или звук тревоги
        }
    }

    void OpenDoor()
    {
        // Простое открытие: поворот или деактивация
        door.SetActive(false);
        // Или анимация: door.GetComponent<Animator>().SetTrigger("Open");
    }
}