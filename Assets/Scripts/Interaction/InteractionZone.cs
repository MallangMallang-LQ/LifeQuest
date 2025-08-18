using UnityEngine;
using LifeQuest.NPCFlow.Core; // GameFlowController 네임스페이스

namespace LifeQuest.NPCFlow.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class InteractionZone : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameFlowController flow;

        [Header("Player Filter")]
        [SerializeField] private string playerTag = "Player"; // 플레이어 태그

        [Header("Behavior")]
        [SerializeField] private bool startOnEnter = true;     // 진입 즉시 흐름 시작
        [SerializeField] private bool singleUse = false;       // 한 번만 동작
        [SerializeField] private float reenterCooldown = 1.0f; // 재진입 쿨다운(초)

        private bool _used = false;
        private float _lastEnterTime = -999f;

        void Awake()
        {
            // 자동 참조(배선 깜빡 방지)
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
                Debug.LogWarning("[InteractionZone] Collider.isTrigger를 자동으로 켰습니다.", this);
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
                Debug.LogWarning("[InteractionZone] GameFlowController 참조가 없습니다.", this);
                return;
            }
            // 이미 대면 중이면 무시(중복 시작 방지)
            if (flow.InFaceToFace) return;

            flow.StartFaceToFaceFlow();

            if (singleUse)
            {
                _used = true;
                // 원치 않으면 이 줄은 주석 처리
                var col = GetComponent<Collider>();
                if (col) col.enabled = false;
            }
        }

        private bool IsPlayer(Collider other)
        {
            // 태그가 지정되어 있으면 태그 기반으로 폭넓게 판별(자식 콜라이더/XR 리그 대응)
            if (!string.IsNullOrEmpty(playerTag))
            {
                if (other.CompareTag(playerTag)) return true;
                if (other.attachedRigidbody && other.attachedRigidbody.CompareTag(playerTag)) return true;
                if (other.transform.root && other.transform.root.CompareTag(playerTag)) return true;
                return false;
            }

            // 태그 미지정이면 대표 컴포넌트로 추정(최후의 수단)
            return other.GetComponent<CharacterController>() || other.attachedRigidbody;
        }
    }
}
