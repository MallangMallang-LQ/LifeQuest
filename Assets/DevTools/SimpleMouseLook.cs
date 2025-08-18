using UnityEngine;

// PC 테스트 전용, 우클릭 유지 시 마우스 룩 + WASD 이동
[DisallowMultipleComponent]
public class SimpleMouseLook : MonoBehaviour
{
    public float lookSensitivity = 120f;   // deg/s
    public float moveSpeed = 3f;
    public float sprint = 2f;
    public float pitchMin = -85f, pitchMax = 85f;

    float yaw, pitch;
    bool cursorLocked;

    void OnEnable()
    {
        var euler = transform.eulerAngles;
        yaw = euler.y;
        pitch = euler.x > 180 ? euler.x - 360f : euler.x;
    }

    void Update()
    {
        // 우클릭으로 마우스 룩
        if (Input.GetMouseButtonDown(1)) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; cursorLocked = true; }
        if (Input.GetMouseButtonUp(1)) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; cursorLocked = false; }

        if (cursorLocked)
        {
            float mx = Input.GetAxis("Mouse X");
            float my = Input.GetAxis("Mouse Y");
            yaw += mx * lookSensitivity * Time.deltaTime;
            pitch -= my * lookSensitivity * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        // WASD/Space/C 이동
        Vector3 dir = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        if (Input.GetKey(KeyCode.Space)) dir.y += 1f;
        if (Input.GetKey(KeyCode.C)) dir.y -= 1f;

        float spd = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprint : 1f);
        transform.position += transform.TransformDirection(dir.normalized) * spd * Time.deltaTime;

        // Esc로 커서 해제
        if (Input.GetKeyDown(KeyCode.Escape)) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; cursorLocked = false; }
    }
}
