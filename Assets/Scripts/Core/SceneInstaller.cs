using UnityEngine;
using LifeQuest.NPCFlow.UI;
using LifeQuest.NPCFlow.NPC;        // Orchestrator / FSM ���ӽ����̽�
using LifeQuest.NPCFlow.Core;       // GameFlowController
using LifeQuest.NPCFlow.Utilities;  // Services

namespace LifeQuest.NPCFlow.Core
{
    public class SceneInstaller : MonoBehaviour
    {
        [Header("Core Refs (�巡�� ����)")]
        public NpcViewDriver view;
        public MicrophoneCapture mic;
        public VoiceActivityDetector vad;
        public TextToSpeechService tts;
        public SpeechToTextService stt;
        public NPCDialogueOrchestrator orchestrator;
        public GameFlowController flow;

        [Header("Controls")]
        public bool applyWiringOnAwake = true;
        public bool registerServices = true;
        public bool fillMissingFromScene = true;

        void Awake()
        {
            if (!applyWiringOnAwake) return;
            Apply();
        }

        [ContextMenu("DryRun: Check wiring")]
        void Apply()
        {
            // �ڵ� ����(������� ����)
            if (fillMissingFromScene)
            {
                view = Services.FindIfNull(view);
                mic = Services.FindIfNull(mic);
                vad = Services.FindIfNull(vad);
                tts = Services.FindIfNull(tts);
                stt = Services.FindIfNull(stt);
                orchestrator = Services.FindIfNull(orchestrator);
                flow = Services.FindIfNull(flow);
            }

            // ���� ����
            if (stt && mic) stt.SetMic(mic);

            // Orchestrator �ڵ� ���� ��(�ߺ� �ֵ� ����)
            if (orchestrator) orchestrator.autoStart = false;

            // (����) GameFlow�� orchestrator ���� ����
            if (flow && orchestrator)
            {
                // public/serialized�� �ν����ͷ� �����ص� �ǰ�, �Ʒ�ó�� �����ص� ��.
                var field = typeof(GameFlowController).GetField("orchestrator",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                if (field != null && field.GetValue(flow) == null) field.SetValue(flow, orchestrator);
            }

            if (registerServices)
                Services.Register(view, mic, vad, tts, stt, orchestrator, flow);

            Services.LogSnapshot(nameof(SceneInstaller));
        }
    }
}
