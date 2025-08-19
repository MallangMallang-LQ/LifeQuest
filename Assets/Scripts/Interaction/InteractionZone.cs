using UnityEngine;
using LifeQuest.NPCFlow.Core; // GameFlowController ���ӽ����̽�

namespace LifeQuest.NPCFlow.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class InteractionZone : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameFlowController flow;

        [Header("Player Filter")]
        [SerializeField] private string playerTag = "Player"; // �÷��̾� �±�

        [Header("Behavior")]
        [SerializeField] private bool startOnEnter = true;     // ���� ��� �帧 ����
        [SerializeField] private bool singleUse = false;       // �� ���� ����
        [SerializeField] private float reenterCooldown = 1.0f; // ������ ��ٿ�(��)

        private bool _used = false;
        private float _lastEnterTime = -999f;

        void Awake()
        {
            // �ڵ� ����(�輱 ���� ����)
            if (!flow) flow = FindObjectOfType<GameFlowController>(includeInactive: true);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0, 1, 1, 0.25f);
            var col = GetComponent<Collider>();
            if (col is BoxCollider b) Gizmos.DrawCube(transform.position + b.center, b.size);
            else if (col) Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col) col.isTrigger = true;
        }

        private void OnValidate()
        {
            var col = GetComponent<Collider>();
            if (col && !col.isTrigger)
            {
                col.isTrigger = true;
                Debug.LogWarning("[InteractionZone] Collider.isTrigger�� �ڵ����� �׽��ϴ�.", this);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other)) return;
            if (singleUse && _used) return;
            if (Time.time - _lastEnterTime < reenterCooldown) return;

            _lastEnterTime = Time.time;
            Debug.Log("[InteractionZone] Player entered", this);

            if (!startOnEnter) return;
            if (!flow)
            {
                Debug.LogWarning("[InteractionZone] GameFlowController ������ �����ϴ�.", this);
                return;
            }
            // �̹� ��� ���̸� ����(�ߺ� ���� ����)
            if (flow.InFaceToFace) return;

            flow.StartFaceToFaceFlow();

            if (singleUse)
            {
                _used = true;
                // ��ġ ������ �� ���� �ּ� ó��
                var col = GetComponent<Collider>();
                if (col) col.enabled = false;
            }
        }

        private bool IsPlayer(Collider other)
        {
            // �±װ� �����Ǿ� ������ �±� ������� ���а� �Ǻ�(�ڽ� �ݶ��̴�/XR ���� ����)
            if (!string.IsNullOrEmpty(playerTag))
            {
                if (other.CompareTag(playerTag)) return true;
                if (other.attachedRigidbody && other.attachedRigidbody.CompareTag(playerTag)) return true;
                if (other.transform.root && other.transform.root.CompareTag(playerTag)) return true;
                return false;
            }

            // �±� �������̸� ��ǥ ������Ʈ�� ����(������ ����)
            return other.GetComponent<CharacterController>() || other.attachedRigidbody;
        }
    }
}
