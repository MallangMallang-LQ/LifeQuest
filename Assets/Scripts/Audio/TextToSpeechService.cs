using UnityEngine;
using System.Collections;
using System.Text;
using UnityEngine.Networking;
using System;

public interface ITextToSpeech
{
    // 재생 완료까지 대기
    IEnumerator Speak(string text, bool interrupt = true);
    void Stop();
    bool IsSpeaking { get; }
    AudioClip GenerateClip(string text);  // 문자열 -> 클립 변환 전용
}

public class TextToSpeechService : MonoBehaviour, ITextToSpeech
{
    [Header("Output")]
    [SerializeField] private AudioSource voiceSource;   // NPC 전용 VoiceSource

    [Header("Mode")]
    public bool useMock = true;                        // true=모의, false=실제 TTS

    [Header("Mock Synthesis")]
    public int mockSampleRate = 24000;
    public float mockFreq = 650f;
    [Range(0f, 0.5f)] public float mockAmp = 0.08f;
    public float durationPerChar = 0.05f;
    public Vector2 mockDurationClamp = new Vector2(0.3f, 2.5f);

    [Header("OpenAI TTS")]
    [Tooltip("모델명 (예: gpt-4o-mini-tts)")]
    public string model = "gpt-4o-mini-tts";
    [Tooltip("보이스 이름 (예: alloy)")]
    public string voice = "alloy";
    [Tooltip("OpenAI 기본 엔드포인트 (/v1 포함 권장)")]
    public string baseUrl = "https://api.openai.com/v1";

    private Coroutine speakingRoutine;
    public bool IsSpeaking => voiceSource && voiceSource.isPlaying;

    /// <summary>문자열을 AudioClip으로 변환 (모의 또는 실제 TTS 결과)</summary>
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
            // 실제 TTS는 비동기 호출로 Clip을 받아오므로 여기선 null 반환
            // 재생은 Speak()에서 FetchClipFromAPI로 처리
            return null;
        }
    }

    /// <summary>문자열을 받아 재생(완료까지 대기). interrupt=false면 현재 재생이 끝날 때까지 대기 후 재생.</summary>
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
            // 실제 TTS: API 호출 -> WAV 바이트 -> AudioClip -> 재생
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

    /// <summary>런타임에 VoiceSource 바꾸고 싶을 때 사용(선택)</summary>
    public void SetVoiceSource(AudioSource src) => voiceSource = src;

    private IEnumerator PlayRoutine(AudioClip clip)
    {
        if (!voiceSource || !clip) yield break;
        voiceSource.clip = clip;
        voiceSource.Play();
        // 보다 견고한 대기: length + 약간의 여유
        yield return new WaitForSeconds(clip.length + 0.02f);
        speakingRoutine = null;
    }

    // === OpenAI TTS 호출부 (UnityWebRequest) ===
    private IEnumerator FetchClipFromAPI(string text, bool autoPlay)
    {
        // 0) 키 확인 (환경변수)
        string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("[TTS] OPENAI_API_KEY 환경변수가 설정되지 않았습니다.");
            yield break;
        }

        // 1) URL 조립 (/v1 보정)
        string baseFixed = (baseUrl ?? "https://api.openai.com/v1").TrimEnd('/');
        if (!baseFixed.EndsWith("/v1")) baseFixed += "/v1";
        string url = baseFixed + "/audio/speech";

        // 2) 요청 JSON (반드시 WAV로 받도록 "format":"wav")
        string bodyJson = "{\"model\":\"" + model + "\",\"voice\":\"" + voice +
                          "\",\"format\":\"wav\",\"input\":\"" + EscapeJson(text) + "\"}";

        using (var req = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJson);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + apiKey);
            // 타임아웃 필요시: req.timeout = 15;

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

            // 3) WAV 바이트 -> AudioClip
            byte[] audioData = req.downloadHandler.data;
            AudioClip clip = WavUtility.ToAudioClip(audioData, "tts_result");  // <- 변환 지점
            if (clip == null)
            {
                Debug.LogError("[TTS] WAV -> AudioClip 변환 실패");
                yield break;
            }

            if (autoPlay)
                speakingRoutine = StartCoroutine(PlayRoutine(clip));
        }
    }

    // JSON 안전하게 만들기 (쌍따옴표/역슬래시/개행 이스케이프)
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
