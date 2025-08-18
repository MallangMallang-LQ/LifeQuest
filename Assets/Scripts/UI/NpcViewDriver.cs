using UnityEngine;
using TMPro;

namespace LifeQuest.NPCFlow.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("LifeQuest/UI/NpcViewDriver")]
    public class NpcViewDriver : MonoBehaviour
    {
        // 문자열 맞추기용 (씬의 '정확한' 이름/경로로 필요시 수정)
        const string PATH_BUBBLE_TEXT = "Bubble_Canvas/BubblePanel/BubbleText";
        const string PATH_PLAYER_SPEECH_TEXT = "PlayerHUD_Canvas/PlayerSpeech_Panel/PlayerSpeech_Text";
        const string PATH_BUBBLE_PANEL = "Bubble_Canvas/BubblePanel";
        const string PATH_PLAYER_PANEL = "PlayerHUD_Canvas/PlayerSpeech_Panel";
        const string NAME_DEBUG_PANEL = "DebugPanel";
        const string NAME_VOICE_SOURCE = "VoiceSource";

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI balloonText;        // NPC 말풍선 (BubbleText)
        [SerializeField] private TextMeshProUGUI playerSpeechText;   // 플레이어 HUD (PlayerSpeech_Text)
        [SerializeField] private GameObject npcBubblePanel;          // BubblePanel
        [SerializeField] private GameObject playerSpeechPanel;       // PlayerSpeech_Panel
        [SerializeField] private GameObject debugPanel;              // (선택) DebugPanel

        [Header("Audio (선택)")]
        [SerializeField] private AudioSource voiceSource;            // NPC 음성 재생용 AudioSource
        [SerializeField] private TextToSpeechService tts;            // String -> AudioClip/재생

        [Header("Player Speech (STT 표시)")]
        [SerializeField] private SpeechToTextService stt;            // 플레이어 음성 -> 텍스트
        [Tooltip("전사할 최근 마이크 구간(초)")]
        [SerializeField, Range(0.5f, 5f)] private float sttWindowSec = 1.5f;
        [Tooltip("전사 표시 후 자동으로 지울지")]
        [SerializeField] private bool autoClearPlayerHud = true;
        [SerializeField, Range(0.5f, 10f)] private float clearAfterSec = 3f;

        [Header("Options")]
        [SerializeField] private bool debugMode = false;

        Coroutine _clearHudCo;

        // ---------- Auto-wire ----------
        void Awake()
        {
            // 경로/이름 기반 자동 연결 (Inspector가 비었을 때만)
            if (!balloonText)
                balloonText = GameObject.Find(PATH_BUBBLE_TEXT)?.GetComponent<TextMeshProUGUI>();

            if (!playerSpeechText)
                playerSpeechText = GameObject.Find(PATH_PLAYER_SPEECH_TEXT)?.GetComponent<TextMeshProUGUI>();

            if (!npcBubblePanel)
                npcBubblePanel = GameObject.Find(PATH_BUBBLE_PANEL);

            if (!playerSpeechPanel)
                playerSpeechPanel = GameObject.Find(PATH_PLAYER_PANEL);

            if (!debugPanel)
                debugPanel = GameObject.Find(NAME_DEBUG_PANEL);

            if (!voiceSource)
                voiceSource = GameObject.Find(NAME_VOICE_SOURCE)?.GetComponent<AudioSource>();

            // TTS가 별도 AudioSource를 쓰도록 주입(비동기 Speak 사용 대비)
            if (tts != null && voiceSource != null)
            {
                tts.SetVoiceSource(voiceSource);
            }

#if UNITY_EDITOR
            if (debugMode)
            {
                if (!balloonText)       Debug.LogWarning("[NpcViewDriver] BubbleText 미연결/미발견", this);
                if (!playerSpeechText)  Debug.LogWarning("[NpcViewDriver] PlayerSpeech_Text 미연결/미발견", this);
                if (!npcBubblePanel)    Debug.LogWarning("[NpcViewDriver] BubblePanel 미연결/미발견", this);
                if (!playerSpeechPanel) Debug.LogWarning("[NpcViewDriver] PlayerSpeech_Panel 미연결/미발견", this);
                if (!voiceSource)       Debug.LogWarning("[NpcViewDriver] VoiceSource(AudioSource) 미연결/미발견", this);
                if (!stt)               Debug.LogWarning("[NpcViewDriver] STT(SpeechToTextService) 미연결", this);
                if (!tts)               Debug.LogWarning("[NpcViewDriver] TTS(TextToSpeechService) 미연결", this);
            }
#endif
        }

        void OnEnable()
        {
            if (stt != null) stt.OnTranscriptReady += HandlePlayerTranscript;
        }

        void OnDisable()
        {
            if (stt != null) stt.OnTranscriptReady -= HandlePlayerTranscript;
        }

        // ---------- Public API ----------
        /// <summary>NPC 말풍선 텍스트 표시</summary>
        public void SetNpcText(string text)
        {
            if (!balloonText) return;
            balloonText.text = text ?? string.Empty;

            if (!balloonText.gameObject.activeInHierarchy)
                balloonText.gameObject.SetActive(true);

            // 상위 패널을 안전하게 켜줌
            if (npcBubblePanel && !npcBubblePanel.activeSelf)
                npcBubblePanel.SetActive(true);
        }

        /// <summary>플레이어 HUD 텍스트 표시</summary>
        public void SetPlayerText(string text)
        {
            if (!playerSpeechText) return;
            playerSpeechText.text = text ?? string.Empty;

            if (!playerSpeechText.gameObject.activeInHierarchy)
                playerSpeechText.gameObject.SetActive(true);

            if (playerSpeechPanel && !playerSpeechPanel.activeSelf)
                playerSpeechPanel.SetActive(true);
        }

        /// <summary>NPC 말풍선 패널 보이기/숨기기</summary>
        public void ShowNpcBubble(bool on)
        {
            if (npcBubblePanel) npcBubblePanel.SetActive(on);
        }

        /// <summary>플레이어 HUD 패널 보이기/숨기기</summary>
        public void ShowPlayerHUD(bool on)
        {
            if (playerSpeechPanel) playerSpeechPanel.SetActive(on);
        }

        /// <summary>디버그 패널 표시/숨김</summary>
        public void SetDebugVisible(bool on)
        {
            if (debugPanel) debugPanel.SetActive(on);
        }

        /// <summary>TTS 문자열을 받아서 재생(비동기). TextToSpeechService.Speak 사용</summary>
        public void PlayVoice(string text, bool interrupt = true)
        {
            if (!isActiveAndEnabled || tts == null || string.IsNullOrEmpty(text)) return;
            // GenerateClip 방식 대신 실제/모의 모두 대응하는 Speak 사용
            StartCoroutine(tts.Speak(text, interrupt));
        }

        /// <summary>TTS 오디오 재생 (clip이 준비된 경우에만 사용)</summary>
        public void PlayVoice(AudioClip clip, bool interrupt = true)
        {
            if (!voiceSource || !clip) return;
            if (interrupt && voiceSource.isPlaying) voiceSource.Stop();
            voiceSource.clip = clip;
            voiceSource.Play();
        }

        /// <summary>TTS 오디오 정지</summary>
        public void StopVoice()
        {
            if (voiceSource && voiceSource.isPlaying)
                voiceSource.Stop();
        }

        /// <summary>HUD/말풍선 텍스트 초기화</summary>
        public void Clear()
        {
            if (balloonText) balloonText.text = string.Empty;
            if (playerSpeechText) playerSpeechText.text = string.Empty;
        }

        // ---------- Player Speech (STT 표시/트리거) ----------
        /// <summary>플레이어 음성 듣기 트리거 (버튼/오케스트레이터에서 호출)</summary>
        public void TriggerListenPlayer()
        {
            if (stt == null)
            {
                Debug.LogWarning("[NpcViewDriver] STT 참조가 없습니다.");
                return;
            }
            StartCoroutine(stt.TranscribeRecent(sttWindowSec, null));
            SetPlayerText(" 듣는중...");
        }

        /// <summary>STT 완료 콜백을 받아 HUD에 표시</summary>
        private void HandlePlayerTranscript(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            SetPlayerText(text);

            if (autoClearPlayerHud)
            {
                if (_clearHudCo != null) StopCoroutine(_clearHudCo);
                _clearHudCo = StartCoroutine(ClearPlayerHudAfter(clearAfterSec));
            }
        }

        private System.Collections.IEnumerator ClearPlayerHudAfter(float sec)
        {
            yield return new WaitForSeconds(sec);
            SetPlayerText(string.Empty);
            _clearHudCo = null;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (!debugMode) return;
            if (!balloonText)       Debug.LogWarning("[NpcViewDriver] balloonText가 비어있습니다.", this);
            if (!playerSpeechText)  Debug.LogWarning("[NpcViewDriver] playerSpeechText가 비어있습니다.", this);
        }
#endif
    }
}
