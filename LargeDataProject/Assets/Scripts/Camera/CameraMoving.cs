using UnityEngine;

public class CameraMoving : MonoBehaviour
{
    public float moveSpeed = 10.0f;          // 카메라 이동 속도
    public float verticalSpeed = 5.0f;       // 위아래 이동 속도
    public float mouseSensitivity = 10.0f;    // 마우스 감도

    private float rotationX = 0f;
    private float rotationY = 0f;

    void Update()
    {
        Move();
        MouseLook();
    }

    void Move()
    {
        // WASD 또는 화살표키 이동
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 direction = (transform.forward * v + transform.right * h).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;

        // 스페이스바를 누르고 있는 동안 위로 이동
        if (Input.GetKey(KeyCode.Space))
        {
            transform.position += Vector3.up * verticalSpeed * Time.deltaTime;
        }
    }

    void MouseLook()
    {
        rotationX += Input.GetAxis("Mouse X") * mouseSensitivity;
        rotationY += Input.GetAxis("Mouse Y") * mouseSensitivity;

        // 수직 회전 각도 제한 (위 아래 너무 많이 못돌게)
        rotationY = Mathf.Clamp(rotationY, -90f, 90f);

        transform.localRotation = Quaternion.Euler(-rotationY, rotationX, 0);
    }
}
