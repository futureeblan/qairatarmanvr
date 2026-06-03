using UnityEngine;

public class HackingTrigger : MonoBehaviour
{
    public GameObject hackingCanvas;

    void Start()
    {
        if (hackingCanvas == null) return;

        // Если на Canvas есть ProximityCanvas — он сам управляет видимостью
        if (hackingCanvas.GetComponent<ProximityCanvas>() != null) return;

        hackingCanvas.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || hackingCanvas == null) return;

        ProximityCanvas proximity = hackingCanvas.GetComponent<ProximityCanvas>();
        if (proximity != null)
            proximity.Show();
        else
            hackingCanvas.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player") || hackingCanvas == null) return;

        ProximityCanvas proximity = hackingCanvas.GetComponent<ProximityCanvas>();
        if (proximity != null)
            proximity.Hide();
        else
            hackingCanvas.SetActive(false);
    }
}
