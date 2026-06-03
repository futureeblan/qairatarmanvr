using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Terminal panel placed near a door.
/// Shows ACCESS DENIED (red) until the player hacks it, then ACCESS GRANTED (green)
/// and calls DoorController.UnlockAndOpen().
///
/// World Space Canvas setup:
///   - Attach this script to the Canvas root.
///   - Wire statusText, hackButton (and optionally codeInput) in the Inspector.
///   - Set targetDoor to the DoorController you want to unlock.
/// </summary>
public class TerminalHack : MonoBehaviour
{
    [Header("Target")]
    public DoorController targetDoor;

    [Header("UI References")]
    public Text      statusText;
    public Button    hackButton;
    public InputField codeInput;   // optional — leave empty to skip code check

    [Header("Settings")]
    [Tooltip("Leave empty to allow any button press to unlock")]
    public string correctCode = "";

    static readonly Color Red   = new Color(0.85f, 0.10f, 0.10f);
    static readonly Color Green = new Color(0.10f, 0.85f, 0.20f);

    void Start()
    {
        ShowDenied();
        if (hackButton != null)
            hackButton.onClick.AddListener(OnHackPressed);
    }

    void OnHackPressed()
    {
        if (targetDoor == null) return;

        // If a code is set, validate it; otherwise any press works
        bool codeOk = string.IsNullOrEmpty(correctCode) ||
                      (codeInput != null && codeInput.text.Trim() == correctCode);

        if (codeOk)
        {
            targetDoor.UnlockAndOpen();
            ShowGranted();
            if (hackButton != null) hackButton.interactable = false;
        }
        else
        {
            StartCoroutine(WrongCode());
        }
    }

    void ShowDenied()
    {
        if (statusText == null) return;
        statusText.text  = "ACCESS DENIED";
        statusText.color = Red;
    }

    void ShowGranted()
    {
        if (statusText == null) return;
        statusText.text  = "ACCESS GRANTED";
        statusText.color = Green;
    }

    IEnumerator WrongCode()
    {
        if (statusText == null) yield break;
        statusText.text  = "WRONG CODE";
        statusText.color = Red;
        yield return new WaitForSeconds(1.5f);
        ShowDenied();
    }
}
