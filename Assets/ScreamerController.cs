using UnityEngine;

using System.Collections;



public class ScreamerController : MonoBehaviour

{

    public GameObject robot; // Сюда перетащим робота

    public AudioSource screamerSound; // Сюда перетащим звук (если есть)



    // Этот метод мы будем вызывать при нажатии на НЕПРАВИЛЬНУЮ кнопку

    public void ActivateScreamer()

    {

        // Запускаем процесс появления и исчезновения

        StartCoroutine(ScreamerProcess());

    }



    IEnumerator ScreamerProcess()

    {

        // 1. Показываем робота

        robot.SetActive(true);



        // 2. Играем звук (если он назначен)

        if (screamerSound != null)

        {

            screamerSound.Play();

        }



        // 3. Ждем 3.5 секунды

        yield return new WaitForSeconds(1f);



        // 4. Прячем робота обратно

        robot.SetActive(false);

    }

}