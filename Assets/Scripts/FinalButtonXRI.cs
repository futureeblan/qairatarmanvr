using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Фикс для VRButtonGripped (пакет VRInteractions работает только со SteamVR,
/// а в проекте используется OpenXR). Этот скрипт добавляет XRI-совместимое
/// взаимодействие и вызывает SimpleFinalButton.StartFinal() при нажатии.
///
/// КАК ИСПОЛЬЗОВАТЬ:
///   1. Выбери VRButtonGripped в Hierarchy
///   2. Add Component → FinalButtonXRI
///   3. Убедись что на XR Origin есть XRDirectInteractor (на руках)
/// </summary>
[RequireComponent(typeof(SimpleFinalButton))]
public class FinalButtonXRI : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Если true — срабатывает при ЛЮБОМ select (луч или рука). " +
             "Если false — только при прямом касании рукой.")]
    public bool allowRayInteraction = false;

    SimpleFinalButton   _finalButton;
    XRSimpleInteractable _interactable;

    void Awake()
    {
        _finalButton = GetComponent<SimpleFinalButton>();

        // Добавляем XRSimpleInteractable если его нет
        _interactable = GetComponent<XRSimpleInteractable>();
        if (_interactable == null)
            _interactable = gameObject.AddComponent<XRSimpleInteractable>();

        // Убедимся что коллайдер есть (XRI требует коллайдер для обнаружения)
        if (GetComponent<Collider>() == null)
        {
            var col = gameObject.AddComponent<BoxCollider>();
            col.size   = Vector3.one * 0.15f;
            col.center = Vector3.zero;
            Debug.Log("[FinalButtonXRI] Добавлен BoxCollider автоматически. " +
                      "Настрой размер в Inspector если нужно.");
        }
    }

    void OnEnable()
    {
        _interactable.selectEntered.AddListener(OnSelectEntered);
    }

    void OnDisable()
    {
        _interactable.selectEntered.RemoveListener(OnSelectEntered);
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        // Если allowRayInteraction = false — игнорируем Ray Interactor
        if (!allowRayInteraction && args.interactorObject is XRRayInteractor)
            return;

        _finalButton.StartFinal();
    }
}
