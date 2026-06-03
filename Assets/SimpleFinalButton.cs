using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleFinalButton : MonoBehaviour
{
    public GameObject mech;
    public GlitchOverlay glitch;

    // Эту функцию ты выбираешь в инспекторе (Select Entered)
    public void StartFinal()
    {
        // ЭТА СТРОЧКА ВЫВЕДЕТ ТЕКСТ В КОНСОЛЬ
        Debug.Log("<color=red>ИГРА ЗАКОНЧИЛАСЬ</color>");

        if (mech != null) mech.SetActive(false);
        if (glitch != null) glitch.SetGlitch(true);

        // Даем игроку 3 секунды посмотреть на глитч перед выходом
        Invoke("RestartGame", 3f);
    }

    void RestartGame()
    {
        // Загружает первую сцену из Build Settings (обычно меню)
        SceneManager.LoadScene(0);
    }
}