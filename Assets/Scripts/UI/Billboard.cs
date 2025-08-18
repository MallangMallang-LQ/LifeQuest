using UnityEngine;

[DefaultExecutionOrder(50)] //���߿� ����(ī�޶� ������ ����)
public class Billboard : MonoBehaviour
{
    [Header("Target (Main Camera)")]
    [SerializeField] private Transform target; // ���� Main Camera �ڵ� ���
    [SerializeField] private bool onlyYaw = true;   //true�� ����(Y��)�� ȸ��

    [Header("Facing Fix")]
    [SerializeField] private bool flip180 = true;  // ��/�� ������ ����(�ſ���� ����)


    [Header("ũ�� ���� �ɼ� (����)")]
    [SerializeField] private bool maintainScreenSize = true;    //�Ÿ��� ������� ����� ȭ�� ũ��� ���̰�
    [Range(0.01f, 1f)][SerializeField] private float sizeAt1m = 0.2f;   //1m �Ÿ����� ���̴� ����(���� ����)
    [SerializeField] private float minScale = 0.0008f;
    [SerializeField] private float maxScale = 0.01f;

    [Header("������/����")]
    [SerializeField] private Vector3 positionOffset;  // ȸ��/������ ��� ���� ��ġ ����
    [SerializeField] private Vector3 rotationOffset;  // ���� ȸ�� ����(Euler)

    // ���� ĳ��
    private Transform _cam;
    private Vector3 _baseScale = Vector3.one;

    void Awake()
    {
        _baseScale = transform.localScale; // �ʱ� ������ ����
        if (!target)
        {
            var main = Camera.main;
            if (main) _cam = main.transform;
        }
    }

    void OnEnable()
    {
        // ��Ÿ�ӿ� ī�޶� ���߿� ����� ��� ���
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

        // ī�޶� ������ ���ϰ� �ϴ� �⺻ ȸ�� �����
        Vector3 dir = camTr.position - pivot; // ����ī�޶�
        if (onlyYaw) dir.y = 0f;
        if (dir.sqrMagnitude < 1e-6f) return;

        Quaternion lookRot = Quaternion.LookRotation(dir.normalized, Vector3.up);

        //  �ſ� ���� ����: ĵ������ ������ ���̸� �ѵα�(�⺻ on)
        if (flip180) lookRot *= Quaternion.Euler(0f, 180f, 0f);

        // ����� ������ ����
        transform.rotation = lookRot * Quaternion.Euler(rotationOffset);

        // �Ÿ� ��� ȭ��ũ�� ����
        if (maintainScreenSize)
        {
            float dist = Vector3.Distance(pivot, camTr.position);
            float k = Mathf.Clamp(sizeAt1m * dist, minScale, maxScale);
            transform.localScale = new Vector3(_baseScale.x * k, _baseScale.y * k, _baseScale.z * k);
        }
    }
}
