using UnityEngine;
using System.Collections;
using System.Text;
using UnityEngine.Networking;
using System;

public interface ITextToSpeech
{
    // ��� �Ϸ���� ���
    IEnumerator Speak(string text, bool interrupt = true);
    void Stop();
    bool IsSpeaking { get; }
    AudioClip GenerateClip(string text);  // ���ڿ� -> Ŭ�� ��ȯ ����
}

public class TextToSpeechService : MonoBehaviour, ITextToSpeech
{
    [Header("Output")]
    [SerializeField] private AudioSource voiceSource;   // NPC ���� VoiceSource

    [Header("Mode")]
    public bool useMock = true;                        // true=����, false=���� TTS

    [Header("Mock Synthesis")]
    public int mockSampleRate = 24000;
    public float mockFreq = 650f;
    [Range(0f, 0.5f)] public float mockAmp = 0.08f;
    public float durationPerChar = 0.05f;
    public Vector2 mockDurationClamp = new Vector2(0.3f, 2.5f);

    [Header("OpenAI TTS")]
    [Tooltip("�𵨸� (��: gpt-4o-mini-tts)")]
    public string model = "gpt-4o-mini-tts";
    [Tooltip("���̽� �̸� (��: alloy)")]
    public string voice = "alloy";
    [Tooltip("OpenAI �⺻ ��������Ʈ (/v1 ���� ����)")]
    public string baseUrl = "https://api.openai.com/v1";

    private Coroutine speakingRoutine;
    public bool IsSpeaking => voiceSource && voiceSource.isPlaying;

    /// <summary>���ڿ��� AudioClip���� ��ȯ (���� �Ǵ� ���� TTS ���)</summary>
    public AudioClip GenerateClip(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;

        if (useMock)
        {
            float dur = Mathf.Clamp(text.Length * durationPerChar, mockDurationClamp.x, mockDurationClamp.y);
            return GenerateSineClip(dur, mockSampleRate, mockFreq, mockAmp);
        }
        else
        {
            // ���� TTS�� �񵿱� ȣ��� Clip�� �޾ƿ��Ƿ� ���⼱ null ��ȯ
            // ����� Speak()���� FetchClipFromAPI�� ó��
            return null;
        }
    }

    /// <summary>���ڿ��� �޾� ���(�Ϸ���� ���). interrupt=false�� ���� ����� ���� ������ ��� �� ���.</summary>
    public IEnumerator Speak(string text, bool interrupt = true)
    {
        if (string.IsNullOrEmpty(text) || voiceSource == null) yield break;

        if (interrupt) Stop();
        else { while (IsSpeaking) yield return null; }

        if (useMock)
        {
            var clip = GenerateClip(text);
            if (!clip) yield break;
            speakingRoutine = StartCoroutine(PlayRoutine(clip));
            yield return speakingRoutine;
        }
        else
        {
            // ���� TTS: API ȣ�� -> WAV ����Ʈ -> AudioClip -> ���
            yield return FetchClipFromAPI(text, autoPlay: true);
        }
    }

    public void Stop()
    {
        if (speakingRoutine != null)
        {
            StopCoroutine(speakingRoutine);
            speakingRoutine = null;
        }
        if (voiceSource && voiceSource.isPlaying)
            voiceSource.Stop();
    }

    /// <summary>��Ÿ�ӿ� VoiceSource �ٲٰ� ���� �� ���(����)</summary>
    public void SetVoiceSource(AudioSource src) => voiceSource = src;

    private IEnumerator PlayRoutine(AudioClip clip)
    {
        if (!voiceSource || !clip) yield break;
        voiceSource.clip = clip;
        voiceSource.Play();
        // ���� �߰��� ���: length + �ణ�� ����
        yield return new WaitForSeconds(clip.length + 0.02f);
        speakingRoutine = null;
    }

    // === OpenAI TTS ȣ��� (UnityWebRequest) ===
    private IEnumerator FetchClipFromAPI(string text, bool autoPlay)
    {
        // 0) Ű Ȯ�� (ȯ�溯��)
        string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("[TTS] OPENAI_API_KEY ȯ�溯���� �������� �ʾҽ��ϴ�.");
            yield break;
        }

        // 1) URL ���� (/v1 ����)
        string baseFixed = (baseUrl ?? "https://api.openai.com/v1").TrimEnd('/');
        if (!baseFixed.EndsWith("/v1")) baseFixed += "/v1";
        string url = baseFixed + "/audio/speech";

        // 2) ��û JSON (�ݵ�� WAV�� �޵��� "format":"wav")
        string bodyJson = "{\"model\":\"" + model + "\",\"voice\":\"" + voice +
                          "\",\"format\":\"wav\",\"input\":\"" + EscapeJson(text) + "\"}";

        using (var req = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJson);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + apiKey);
            // Ÿ�Ӿƿ� �ʿ��: req.timeout = 15;

            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool failed = req.result != UnityWebRequest.Result.Success;
#else
            bool failed = req.isHttpError || req.isNetworkError;
#endif
            if (failed)
            {
                Debug.LogError($"[TTS] HTTP {(int)req.responseCode}: {req.error}\n{req.downloadHandler.text}");
                yield break;
            }

            // 3) WAV ����Ʈ -> AudioClip
            byte[] audioData = req.downloadHandler.data;
            AudioClip clip = WavUtility.ToAudioClip(audioData, "tts_result");  // <- ��ȯ ����
            if (clip == null)
            {
                Debug.LogError("[TTS] WAV -> AudioClip ��ȯ ����");
                yield break;
            }

            if (autoPlay)
                speakingRoutine = StartCoroutine(PlayRoutine(clip));
        }
    }

    // JSON �����ϰ� ����� (�ֵ���ǥ/��������/���� �̽�������)
    private static string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
    }

    private AudioClip GenerateSineClip(float seconds, int rate, float freq, float amp)
    {
        int samples = Mathf.CeilToInt(rate * seconds);
        var clip = AudioClip.Create("mock_tts", samples, 1, rate, false);

        var buf = new float[samples];
        float twoPiF = 2f * Mathf.PI * freq;
        for (int i = 0; i < samples; i++)
            buf[i] = Mathf.Sin(twoPiF * i / rate) * amp;

        clip.SetData(buf, 0);
        return clip;
    }
}
