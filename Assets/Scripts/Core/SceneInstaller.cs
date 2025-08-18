using UnityEngine;
using LifeQuest.NPCFlow.UI;
using LifeQuest.NPCFlow.NPC;        // Orchestrator / FSM 네임스페이스
using LifeQuest.NPCFlow.Core;       // GameFlowController
using LifeQuest.NPCFlow.Utilities;  // Services

namespace LifeQuest.NPCFlow.Core
{
    public class SceneInstaller : MonoBehaviour
    {
        [Header("Core Refs (드래그 가능)")]
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
            // 자동 보조(비어있을 때만)
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

            // 의존 주입
            if (stt && mic) stt.SetMic(mic);

            // Orchestrator 자동 시작 끔(중복 주도 방지)
            if (orchestrator) orchestrator.autoStart = false;

            // (선택) GameFlow에 orchestrator 참조 보충
            if (flow && orchestrator)
            {
                // public/serialized면 인스펙터로 연결해도 되고, 아래처럼 보충해도 됨.
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
