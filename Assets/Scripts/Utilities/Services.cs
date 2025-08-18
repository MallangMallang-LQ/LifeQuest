using UnityEngine;
using LifeQuest.NPCFlow.UI;    // NpcViewDriver
using LifeQuest.NPCFlow.Core;  // GameFlowController
using LifeQuest.NPCFlow.NPC;   // NPCDialogueOrchestrator

namespace LifeQuest.NPCFlow.Utilities
{
    /// <summary>���� ���� ������Ʈ��(�б� ����). ����� SceneInstaller�� ����.</summary>
    public static class Services
    {
        public static NpcViewDriver View { get; private set; }
        public static MicrophoneCapture Mic { get; private set; }
        public static VoiceActivityDetector VAD { get; private set; }
        public static TextToSpeechService TTS { get; private set; }
        public static SpeechToTextService STT { get; private set; }
        public static NPCDialogueOrchestrator Orchestrator { get; private set; }
        public static GameFlowController Flow { get; private set; }

        public static void Register(
            NpcViewDriver view,
            MicrophoneCapture mic,
            VoiceActivityDetector vad,
            TextToSpeechService tts,
            SpeechToTextService stt,
            NPCDialogueOrchestrator orchestrator,
            GameFlowController flow)
        {
            View = view ?? View;
            Mic = mic ?? Mic;
            VAD = vad ?? VAD;
            TTS = tts ?? TTS;
            STT = stt ?? STT;
            Orchestrator = orchestrator ?? Orchestrator;
            Flow = flow ?? Flow;

            LogSnapshot("Services.Register");
        }

        // �̸� ����(���̸� "null" ���ڿ�)
        static string N(Object o) => o != null ? o.name : "null";

        public static void LogSnapshot(string tag = "Services")
        {
            Debug.Log(
                $"[{tag}] View={N(View)}, Mic={N(Mic)}, VAD={N(VAD)}, " +
                $"TTS={N(TTS)}, STT={N(STT)}, Orchestrator={N(Orchestrator)}, Flow={N(Flow)}");
        }

        // �� �ڵ� ����
        public static T FindIfNull<T>(T current) where T : Object =>
            current != null ? current : Object.FindObjectOfType<T>(includeInactive: true);
    }
}