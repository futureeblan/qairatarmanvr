using UnityEngine;

public class DoorOpener : MonoBehaviour
{
    // Сюда в инспекторе перетащи объект своей двери (две створки)
    public GameObject doorObject;

    // Эту функцию мы свяжем с кнопкой
    public void OpenTheDoor()
    {
        if (doorObject != null)
        {
            doorObject.SetActive(false); // Дверь просто исчезает
            Debug.Log("Дверь открыта!");

            // Опционально: закрываем сам терминал после открытия
            gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("Забыл перетащить дверь в поле Door Object!");
        }
    }
}