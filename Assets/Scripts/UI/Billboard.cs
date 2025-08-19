using UnityEngine;

[DefaultExecutionOrder(50)] //나중에 실행(카메라 움직임 이후)
public class Billboard : MonoBehaviour
{
    [Header("Target (Main Camera)")]
    [SerializeField] private Transform target; // 비우면 Main Camera 자동 사용
    [SerializeField] private bool onlyYaw = true;   //true면 수평(Y축)만 회전

    [Header("Facing Fix")]
    [SerializeField] private bool flip180 = true;  // 앞/뒤 뒤집힘 보정(거울글자 방지)


    [Header("크기 유지 옵션 (선택)")]
    [SerializeField] private bool maintainScreenSize = true;    //거리와 상관없이 비슷한 화면 크기로 보이게
    [Range(0.01f, 1f)][SerializeField] private float sizeAt1m = 0.2f;   //1m 거리에서 보이는 높이(월드 단위)
    [SerializeField] private float minScale = 0.0008f;
    [SerializeField] private float maxScale = 0.01f;

    [Header("오프셋/보정")]
    [SerializeField] private Vector3 positionOffset;  // 회전/스케일 계산 기준 위치 보정
    [SerializeField] private Vector3 rotationOffset;  // 최종 회전 보정(Euler)

    // 내부 캐시
    private Transform _cam;
    private Vector3 _baseScale = Vector3.one;

    void Awake()
    {
        _baseScale = transform.localScale; // 초기 스케일 보존
        if (!target)
        {
            var main = Camera.main;
            if (main) _cam = main.transform;
        }
    }

    void OnEnable()
    {
        // 런타임에 카메라가 나중에 생기는 경우 대비
        if (!_cam)
        {
            var main = Camera.main;
            if (main) _cam = main.transform;
        }
    }


    void LateUpdate()
    {
        var camTr = target ? target : _cam;
        if (!camTr)
        {
            if (Camera.main) { _cam = Camera.main.transform; camTr = _cam; }
            if (!camTr) return;
        }

        Vector3 pivot = transform.position + positionOffset;

        // 카메라 쪽으로 향하게 하는 기본 회전 만들기
        Vector3 dir = camTr.position - pivot; // 대상→카메라
        if (onlyYaw) dir.y = 0f;
        if (dir.sqrMagnitude < 1e-6f) return;

        Quaternion lookRot = Quaternion.LookRotation(dir.normalized, Vector3.up);

        //  거울 반전 방지: 캔버스가 뒤집혀 보이면 켜두기(기본 on)
        if (flip180) lookRot *= Quaternion.Euler(0f, 180f, 0f);

        // 사용자 오프셋 적용
        transform.rotation = lookRot * Quaternion.Euler(rotationOffset);

        // 거리 기반 화면크기 유지
        if (maintainScreenSize)
        {
            float dist = Vector3.Distance(pivot, camTr.position);
            float k = Mathf.Clamp(sizeAt1m * dist, minScale, maxScale);
            transform.localScale = new Vector3(_baseScale.x * k, _baseScale.y * k, _baseScale.z * k);
        }
    }
}
