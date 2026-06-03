using UnityEngine;

public class HandController : MonoBehaviour
{
    private Animator anim;
    public bool isLeftHand = true;

    void Start()
    {
        anim = GetComponent<Animator>();

        if (anim == null)
        {
            Debug.LogError($"❌ Нет Animator на {gameObject.name}");
            enabled = false;
            return;
        }

        Debug.Log($"✅ {gameObject.name} готов");
    }

    void Update()
    {
        // Тест на ПК
        bool grip = isLeftHand ?
            Input.GetKey(KeyCode.LeftControl) :
            Input.GetKey(KeyCode.RightControl);

        anim.SetBool("isFist", grip);
    }
}
