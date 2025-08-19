using UnityEngine;
using TMPro;

namespace LifeQuest.NPCFlow.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("LifeQuest/UI/NpcViewDriver")]
    public class NpcViewDriver : MonoBehaviour
    {
        // ���ڿ� ���߱�� (���� '��Ȯ��' �̸�/��η� �ʿ�� ����)
        const string PATH_BUBBLE_TEXT = "Bubble_Canvas/BubblePanel/BubbleText";
        const string PATH_PLAYER_SPEECH_TEXT = "PlayerHUD_Canvas/PlayerSpeech_Panel/PlayerSpeech_Text";
        const string PATH_BUBBLE_PANEL = "Bubble_Canvas/BubblePanel";
        const string PATH_PLAYER_PANEL = "PlayerHUD_Canvas/PlayerSpeech_Panel";
        const string NAME_DEBUG_PANEL = "DebugPanel";
        const string NAME_VOICE_SOURCE = "VoiceSource";

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI balloonText;        // NPC ��ǳ�� (BubbleText)
        [SerializeField] private TextMeshProUGUI playerSpeechText;   // �÷��̾� HUD (PlayerSpeech_Text)
        [SerializeField] private GameObject npcBubblePanel;          // BubblePanel
        [SerializeField] private GameObject playerSpeechPanel;       // PlayerSpeech_Panel
        [SerializeField] private GameObject debugPanel;              // (����) DebugPanel

        [Header("Audio (����)")]
        [SerializeField] private AudioSource voiceSource;            // NPC ���� ����� AudioSource
        [SerializeField] private TextToSpeechService tts;            // String -> AudioClip/���

        [Header("Player Speech (STT ǥ��)")]
        [SerializeField] private SpeechToTextService stt;            // �÷��̾� ���� -> �ؽ�Ʈ
        [Tooltip("������ �ֱ� ����ũ ����(��)")]
        [SerializeField, Range(0.5f, 5f)] private float sttWindowSec = 1.5f;
        [Tooltip("���� ǥ�� �� �ڵ����� ������")]
        [SerializeField] private bool autoClearPlayerHud = true;
        [SerializeField, Range(0.5f, 10f)] private float clearAfterSec = 3f;

        [Header("Options")]
        [SerializeField] private bool debugMode = false;

        Coroutine _clearHudCo;

        // ---------- Auto-wire ----------
        void Awake()
        {
            // ���/�̸� ��� �ڵ� ���� (Inspector�� ����� ����)
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

            // TTS�� ���� AudioSource�� ������ ����(�񵿱� Speak ��� ���)
            if (tts != null && voiceSource != null)
            {
                tts.SetVoiceSource(voiceSource);
            }

#if UNITY_EDITOR
            if (debugMode)
            {
                if (!balloonText)       Debug.LogWarning("[NpcViewDriver] BubbleText �̿���/�̹߰�", this);
                if (!playerSpeechText)  Debug.LogWarning("[NpcViewDriver] PlayerSpeech_Text �̿���/�̹߰�", this);
                if (!npcBubblePanel)    Debug.LogWarning("[NpcViewDriver] BubblePanel �̿���/�̹߰�", this);
                if (!playerSpeechPanel) Debug.LogWarning("[NpcViewDriver] PlayerSpeech_Panel �̿���/�̹߰�", this);
                if (!voiceSource)       Debug.LogWarning("[NpcViewDriver] VoiceSource(AudioSource) �̿���/�̹߰�", this);
                if (!stt)               Debug.LogWarning("[NpcViewDriver] STT(SpeechToTextService) �̿���", this);
                if (!tts)               Debug.LogWarning("[NpcViewDriver] TTS(TextToSpeechService) �̿���", this);
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
        /// <summary>NPC ��ǳ�� �ؽ�Ʈ ǥ��</summary>
        public void SetNpcText(string text)
        {
            if (!balloonText) return;
            balloonText.text = text ?? string.Empty;

            if (!balloonText.gameObject.activeInHierarchy)
                balloonText.gameObject.SetActive(true);

            // ���� �г��� �����ϰ� ����
            if (npcBubblePanel && !npcBubblePanel.activeSelf)
                npcBubblePanel.SetActive(true);
        }

        /// <summary>�÷��̾� HUD �ؽ�Ʈ ǥ��</summary>
        public void SetPlayerText(string text)
        {
            if (!playerSpeechText) return;
            playerSpeechText.text = text ?? string.Empty;

            if (!playerSpeechText.gameObject.activeInHierarchy)
                playerSpeechText.gameObject.SetActive(true);

            if (playerSpeechPanel && !playerSpeechPanel.activeSelf)
                playerSpeechPanel.SetActive(true);
        }

        /// <summary>NPC ��ǳ�� �г� ���̱�/�����</summary>
        public void ShowNpcBubble(bool on)
        {
            if (npcBubblePanel) npcBubblePanel.SetActive(on);
        }

        /// <summary>�÷��̾� HUD �г� ���̱�/�����</summary>
        public void ShowPlayerHUD(bool on)
        {
            if (playerSpeechPanel) playerSpeechPanel.SetActive(on);
        }

        /// <summary>����� �г� ǥ��/����</summary>
        public void SetDebugVisible(bool on)
        {
            if (debugPanel) debugPanel.SetActive(on);
        }

        /// <summary>TTS ���ڿ��� �޾Ƽ� ���(�񵿱�). TextToSpeechService.Speak ���</summary>
        public void PlayVoice(string text, bool interrupt = true)
        {
            if (!isActiveAndEnabled || tts == null || string.IsNullOrEmpty(text)) return;
            // GenerateClip ��� ��� ����/���� ��� �����ϴ� Speak ���
            StartCoroutine(tts.Speak(text, interrupt));
        }

        /// <summary>TTS ����� ��� (clip�� �غ�� ��쿡�� ���)</summary>
        public void PlayVoice(AudioClip clip, bool interrupt = true)
        {
            if (!voiceSource || !clip) return;
            if (interrupt && voiceSource.isPlaying) voiceSource.Stop();
            voiceSource.clip = clip;
            voiceSource.Play();
        }

        /// <summary>TTS ����� ����</summary>
        public void StopVoice()
        {
            if (voiceSource && voiceSource.isPlaying)
                voiceSource.Stop();
        }

        /// <summary>HUD/��ǳ�� �ؽ�Ʈ �ʱ�ȭ</summary>
        public void Clear()
        {
            if (balloonText) balloonText.text = string.Empty;
            if (playerSpeechText) playerSpeechText.text = string.Empty;
        }

        // ---------- Player Speech (STT ǥ��/Ʈ����) ----------
        /// <summary>�÷��̾� ���� ��� Ʈ���� (��ư/���ɽ�Ʈ�����Ϳ��� ȣ��)</summary>
        public void TriggerListenPlayer()
        {
            if (stt == null)
            {
                Debug.LogWarning("[NpcViewDriver] STT ������ �����ϴ�.");
                return;
            }
            StartCoroutine(stt.TranscribeRecent(sttWindowSec, null));
            SetPlayerText(" �����...");
        }

        /// <summary>STT �Ϸ� �ݹ��� �޾� HUD�� ǥ��</summary>
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
            if (!balloonText)       Debug.LogWarning("[NpcViewDriver] balloonText�� ����ֽ��ϴ�.", this);
            if (!playerSpeechText)  Debug.LogWarning("[NpcViewDriver] playerSpeechText�� ����ֽ��ϴ�.", this);
        }
#endif
    }
}
