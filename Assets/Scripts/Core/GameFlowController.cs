using UnityEngine;
using LifeQuest.NPCFlow.UI; // NpcViewDriver ����

namespace LifeQuest.NPCFlow.Core
{
    /// <summary>
    /// �� ���� ��� �帧 �Ѱ�(����/�Ϸ� ��ȣ�� UI ������ ���).
    /// ���/���̴� NPCDialogueOrchestrator�� �ֵ��Ѵ�.
    /// </summary>
    public class GameFlowController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private NpcViewDriver view; // ��ǳ��/�÷��̾� HUD ����(�ɼ�)

        [Header("Flow")]
        [SerializeField] private NPCDialogueOrchestrator orchestrator; // <- ������ �巡�� (autoStart=false ����)

        [Header("Options")]
        [SerializeField] private bool showDebugPanelOnStart = false; // ���� �� ����� �г� ǥ��

        // �ߺ� ���� ����
        public bool InFaceToFace { get; private set; } = false;

        // (����) �ܺο��� ���� ������ �̺�Ʈ
        public System.Action OnFaceToFaceStarted;
        public System.Action OnQuestCompleted;

        void Awake()
        {
            // ���� ���۷��� �ڵ� ����(������ ����)
            view ??= FindObjectOfType<NpcViewDriver>(includeInactive: true);
            orchestrator ??= FindObjectOfType<NPCDialogueOrchestrator>(includeInactive: true);

            // ���� �ֵ����� ���ɽ�Ʈ������ �ϳ���(�ߺ� ���� ����)
            if (orchestrator) orchestrator.autoStart = false;
        }

        /// <summary>��� ��� ���� (InteractionZone/��ư���� ȣ��)</summary>
        public void StartFaceToFaceFlow()
        {
            if (InFaceToFace) { Debug.Log("[GameFlow] Already in face-to-face."); return; }
            InFaceToFace = true;
            Debug.Log("[GameFlow] Face-to-face flow start");

            // HUD ����� �гθ� ���ְ�, �λ�/������Ʈ�� Orchestrator�� ���
            if (view) view.SetDebugVisible(showDebugPanelOnStart);
            else Debug.LogWarning("[GameFlow] NpcViewDriver�� ������� �ʾҽ��ϴ�.", this);

            OnFaceToFaceStarted?.Invoke();

            // **���ɽ�Ʈ������ ����(���� �λ� + 1�ܰ� ������Ʈ�� ���ο���)
            if (orchestrator)
            {
                if (!orchestrator.gameObject.activeSelf) orchestrator.gameObject.SetActive(true);
                orchestrator.StartDialogue();
            }
            else
            {
                Debug.LogError("[GameFlow] Orchestrator�� ��� �ֽ��ϴ�. ������ �巡���� �ּ���.", this);
            }
        }

        /// <summary>
        /// ���ɽ�Ʈ�����Ͱ� Success�� �������� �� ȣ��(���ɽ�Ʈ�����Ϳ��� �� �ٷ� ȣ��).
        /// </summary>
        public void CompleteQuest()
        {
            Debug.Log("[GameFlow] Quest Complete");
            OnQuestCompleted?.Invoke();

            // �ʿ� �� �ļ� ó��(ȿ����/�佺Ʈ/�� ��ȯ ��) �߰� ����
            EndFaceToFaceFlow();
        }

        /// <summary>��� �帧 ����(���� ���� ����)</summary>
        public void EndFaceToFaceFlow()
        {
            InFaceToFace = false;
            // �ʿ� �� �ּ� ������ ����
            // view?.Clear();
        }

        /// <summary>��� �帧 ���� ����(������ ���)</summary>
        public void ResetFaceToFaceFlow()
        {
            InFaceToFace = false;
            view?.Clear();
            view?.SetDebugVisible(false);
        }

        // �����Ϳ��� ��Ŭ�� �޴��� �ٷ� �׽�Ʈ ����
        [ContextMenu("SMOKE/Start face-to-face")]
        private void _Menu_StartFaceToFace() => StartFaceToFaceFlow();
    }
}