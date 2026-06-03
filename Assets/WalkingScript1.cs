using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Walking Script ������ ���� �������� �����:
public class WalkingScript1 : MonoBehaviour
{
    public Animator anim;
    public float moveSpeed = 3f;

    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // ��������
        Vector3 move = new Vector3(horizontal, 0, vertical);
        transform.position += move * moveSpeed * Time.deltaTime;

        // ��������
        bool isWalking = move.magnitude > 0.1f;
        anim.SetBool("isWalking", isWalking);
    }
}
