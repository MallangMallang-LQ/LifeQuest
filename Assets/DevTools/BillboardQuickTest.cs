using UnityEngine;

[DisallowMultipleComponent]
public class BillboardQuickTest : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Billboard billboard;   // 테스트할 Billboard 컴포넌트
    [SerializeField] private Transform dummyCam;    // 보통 Main Camera

    [Header("Move Cam (m)")]
    [SerializeField] private float step = 0.5f;

    private Vector3 _camStartPos;

    void Awake()
    {
        if (!dummyCam && Camera.main) dummyCam = Camera.main.transform;
        if (dummyCam) _camStartPos = dummyCam.position;
    }

    void OnEnable()
    {
        if (!billboard)
            Debug.LogWarning("[BillboardQuickTest] Billboard 참조가 없습니다. 인스펙터에 할당하세요.", this);
        if (!dummyCam)
            Debug.LogWarning("[BillboardQuickTest] dummyCam(Main Camera) 참조가 없습니다.", this);
    }

    void OnGUI()
    {
        const int w = 220;
        int x = 12, y = 12, h = 28, gap = 6;

        GUI.Box(new Rect(x - 4, y - 6, w + 8, 8 * (h + gap) + 40), "Billboard Quick Test");

        if (GUI.Button(new Rect(x, y, w, h), "Toggle Yaw Only (Y)"))
        {
            ToggleBoolField("onlyYaw");
        }
        y += h + gap;

        if (GUI.Button(new Rect(x, y, w, h), "Toggle Maintain Size (S)"))
        {
            ToggleBoolField("maintainScreenSize");
        }
        y += h + gap;

        if (GUI.Button(new Rect(x, y, w, h), "Cam +X"))
        {
            MoveCam(Vector3.right * step);
        }
        y += h + gap;

        if (GUI.Button(new Rect(x, y, w, h), "Cam -X"))
        {
            MoveCam(Vector3.left * step);
        }
        y += h + gap;

        if (GUI.Button(new Rect(x, y, w, h), "Cam +Z"))
        {
            MoveCam(Vector3.forward * step);
        }
        y += h + gap;

        if (GUI.Button(new Rect(x, y, w, h), "Cam -Z"))
        {
            MoveCam(Vector3.back * step);
        }
        y += h + gap;

        if (GUI.Button(new Rect(x, y, w, h), "Reset Cam Pos (R)"))
        {
            if (dummyCam) dummyCam.position = _camStartPos;
        }
        y += h + gap;

        // 실시간 정보 표시
        string info = BuildInfo();
        GUI.Label(new Rect(x, y + 4, w, 40), info);
    }

    void Update()
    {
        // 단축키
        if (Input.GetKeyDown(KeyCode.Y)) ToggleBoolField("onlyYaw");
        if (Input.GetKeyDown(KeyCode.S)) ToggleBoolField("maintainScreenSize");
        if (Input.GetKeyDown(KeyCode.R) && dummyCam) dummyCam.position = _camStartPos;

        if (Input.GetKey(KeyCode.RightArrow)) MoveCam(Vector3.right * step * Time.deltaTime * 4f);
        if (Input.GetKey(KeyCode.LeftArrow)) MoveCam(Vector3.left * step * Time.deltaTime * 4f);
        if (Input.GetKey(KeyCode.UpArrow)) MoveCam(Vector3.forward * step * Time.deltaTime * 4f);
        if (Input.GetKey(KeyCode.DownArrow)) MoveCam(Vector3.back * step * Time.deltaTime * 4f);

        // 크기 민감도 조정 [+/-]
        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.Plus)) AddToFloatField("sizeAt1m", +0.02f);
        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.Underscore)) AddToFloatField("sizeAt1m", -0.02f);
    }

    private void MoveCam(Vector3 delta)
    {
        if (!dummyCam) return;
        dummyCam.position += delta;
    }

    private void ToggleBoolField(string fieldName)
    {
        if (!billboard) return;
        var fi = typeof(Billboard).GetField(fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        if (fi != null && fi.FieldType == typeof(bool))
        {
            bool current = (bool)fi.GetValue(billboard);
            fi.SetValue(billboard, !current);
        }
    }

    private void AddToFloatField(string fieldName, float delta)
    {
        if (!billboard) return;
        var fi = typeof(Billboard).GetField(fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        if (fi != null && fi.FieldType == typeof(float))
        {
            float v = (float)fi.GetValue(billboard);
            fi.SetValue(billboard, Mathf.Max(0.001f, v + delta));
        }
    }

    private string BuildInfo()
    {
        if (!billboard || !dummyCam) return "Refs not set";
        float dist = Vector3.Distance(billboard.transform.position, dummyCam.position);
        var onlyYaw = GetBool("onlyYaw");
        var keep = GetBool("maintainScreenSize");
        float sizeAt1m = GetFloat("sizeAt1m");

        return $"Dist: {dist:F2}m\nYawOnly:{onlyYaw}  KeepSize:{keep}\nsizeAt1m:{sizeAt1m:F2} (+/-)";
    }

    private bool GetBool(string fieldName)
    {
        var fi = typeof(Billboard).GetField(fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        return fi != null && (bool)fi.GetValue(billboard);
    }

    private float GetFloat(string fieldName)
    {
        var fi = typeof(Billboard).GetField(fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        return fi != null ? (float)fi.GetValue(billboard) : 0f;
    }
}
