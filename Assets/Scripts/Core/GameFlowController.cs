using UnityEngine;
using LifeQuest.NPCFlow.UI; // NpcViewDriver 참조

namespace LifeQuest.NPCFlow.Core
{
    /// <summary>
    /// 씬 레벨 대면 흐름 총괄(시작/완료 신호와 UI 보조만 담당).
    /// 대사/전이는 NPCDialogueOrchestrator가 주도한다.
    /// </summary>
    public class GameFlowController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private NpcViewDriver view; // 말풍선/플레이어 HUD 제어(옵션)

        [Header("Flow")]
        [SerializeField] private NPCDialogueOrchestrator orchestrator; // <- 씬에서 드래그 (autoStart=false 권장)

        [Header("Options")]
        [SerializeField] private bool showDebugPanelOnStart = false; // 진입 시 디버그 패널 표시

        // 중복 시작 방지
        public bool InFaceToFace { get; private set; } = false;

        // (선택) 외부에서 구독 가능한 이벤트
        public System.Action OnFaceToFaceStarted;
        public System.Action OnQuestCompleted;

        void Awake()
        {
            // 부족 레퍼런스 자동 보정(있으면 유지)
            view ??= FindObjectOfType<NpcViewDriver>(includeInactive: true);
            orchestrator ??= FindObjectOfType<NPCDialogueOrchestrator>(includeInactive: true);

            // 시작 주도권은 오케스트레이터 하나만(중복 시작 방지)
            if (orchestrator) orchestrator.autoStart = false;
        }

        /// <summary>대면 경로 시작 (InteractionZone/버튼에서 호출)</summary>
        public void StartFaceToFaceFlow()
        {
            if (InFaceToFace) { Debug.Log("[GameFlow] Already in face-to-face."); return; }
            InFaceToFace = true;
            Debug.Log("[GameFlow] Face-to-face flow start");

            // HUD 디버그 패널만 켜주고, 인사/프롬프트는 Orchestrator가 출력
            if (view) view.SetDebugVisible(showDebugPanelOnStart);
            else Debug.LogWarning("[GameFlow] NpcViewDriver가 연결되지 않았습니다.", this);

            OnFaceToFaceStarted?.Invoke();

            // **오케스트레이터 시작(고정 인사 + 1단계 프롬프트는 내부에서)
            if (orchestrator)
            {
                if (!orchestrator.gameObject.activeSelf) orchestrator.gameObject.SetActive(true);
                orchestrator.StartDialogue();
            }
            else
            {
                Debug.LogError("[GameFlow] Orchestrator가 비어 있습니다. 씬에서 드래그해 주세요.", this);
            }
        }

        /// <summary>
        /// 오케스트레이터가 Success에 도달했을 때 호출(오케스트레이터에서 한 줄로 호출).
        /// </summary>
        public void CompleteQuest()
        {
            Debug.Log("[GameFlow] Quest Complete");
            OnQuestCompleted?.Invoke();

            // 필요 시 후속 처리(효과음/토스트/씬 전환 등) 추가 지점
            EndFaceToFaceFlow();
        }

        /// <summary>대면 흐름 종료(리셋 없이 종료)</summary>
        public void EndFaceToFaceFlow()
        {
            InFaceToFace = false;
            // 필요 시 최소 정리만 수행
            // view?.Clear();
        }

        /// <summary>대면 흐름 완전 리셋(재입장 대비)</summary>
        public void ResetFaceToFaceFlow()
        {
            InFaceToFace = false;
            view?.Clear();
            view?.SetDebugVisible(false);
        }

        // 에디터에서 우클릭 메뉴로 바로 테스트 가능
        [ContextMenu("SMOKE/Start face-to-face")]
        private void _Menu_StartFaceToFace() => StartFaceToFaceFlow();
    }
}