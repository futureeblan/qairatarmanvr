using UnityEngine;
using UnityEngine.UI;

// Вешай этот скрипт на GameObject у которого есть Canvas.
// Canvas будет виден только когда игрок подходит близко.
// Проверка идёт не каждый кадр а раз в несколько — нагрузки почти ноль.
[RequireComponent(typeof(Canvas))]
public class ProximityCanvas : MonoBehaviour
{
    [Header("Расстояние активации")]
    public float activationDistance = 3f;

    [Header("Как часто проверять (кадры)")]
    [Tooltip("1 = каждый кадр, 4 = каждые 4 кадра. Для UI хватает 4-6")]
    public int checkEveryFrames = 4;

    Canvas _canvas;
    GraphicRaycaster _raycaster;
    Transform _playerTransform;
    int _frameOffset;
    bool _isVisible;

    void Awake()
    {
        _canvas = GetComponent<Canvas>();
        _raycaster = GetComponent<GraphicRaycaster>();

        // Спрятать сразу при старте
        SetVisible(false);

        // Распределяем offset чтобы все Canvas не проверялись в один кадр
        _frameOffset = GetInstanceID() % checkEveryFrames;
    }

    void Start()
    {
        FindPlayer();
    }

    void FindPlayer()
    {
        // Ищем XR Origin или объект с тегом Player
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
            if (xrOrigin != null) playerObj = xrOrigin;
        }
        if (playerObj != null)
            _playerTransform = playerObj.transform;
    }

    void Update()
    {
        // Пропускаем кадры — проверяем только раз в checkEveryFrames
        if ((Time.frameCount + _frameOffset) % checkEveryFrames != 0) return;

        if (_playerTransform == null)
        {
            FindPlayer();
            return;
        }

        float sqrDist = (_playerTransform.position - transform.position).sqrMagnitude;
        float sqrThreshold = activationDistance * activationDistance;

        bool shouldBeVisible = sqrDist <= sqrThreshold;

        if (shouldBeVisible != _isVisible)
            SetVisible(shouldBeVisible);
    }

    void SetVisible(bool visible)
    {
        _isVisible = visible;
        _canvas.enabled = visible;

        // Отключаем рейкастер когда не видно — он дорого стоит даже на скрытом Canvas
        if (_raycaster != null)
            _raycaster.enabled = visible;
    }

    // Показать / скрыть из другого скрипта если нужно
    public void Show() => SetVisible(true);
    public void Hide() => SetVisible(false);

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, activationDistance);
    }
}
