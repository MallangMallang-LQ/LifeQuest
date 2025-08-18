using UnityEngine;
using System;
using System.Collections;
using System.Text;
using UnityEngine.Networking;
using LifeQuest.Integrations; // OpenAISettings(ScriptableObject)

public interface ISpeechToText
{
    // 최근 windowSec 만큼 마이크 버퍼를 사용해 변환(모의/실제). 완료 시 onResult 호출.
    IEnumerator TranscribeRecent(float windowSec, Action<string> onResult);

    // 임의 샘플을 직접 넘겨 변환(모의/실제).
    IEnumerator TranscribeSamples(float[] samples, int sampleRate, Action<string> onResult);

    bool IsBusy { get; }
}

[DisallowMultipleComponent]
public class SpeechToTextService : MonoBehaviour, ISpeechToText
{
    [Header("Deps")]
    [SerializeField] private MicrophoneCapture mic;          // Inspector에서 드래그
    [SerializeField] private OpenAISettings settings;        // baseUrl/key/model/timeouts 등

    [Header("Mode")]
    public bool useMock = true;

    [Header("Timing (sec)")]
    [Tooltip("모의 인퍼런스 지연 시간")]
    public float mockLatency = 0.35f;
    [Tooltip("연속 호출 쿨다운(디바운스)")]
    public float cooldownSec = 0.30f;

    [Header("ASR Options")]
    [Tooltip("OpenAI STT 모델명 (예: whisper-1)")]
    public string sttModel = "whisper-1";
    [Tooltip("언어 힌트 (예: ko, en 등)")]
    public string language = "ko";

    public event Action<string> OnTranscriptReady;

    public bool IsBusy { get; private set; }
    private float _lastAt;

    // 메인 진입점: 최근 windowSec 오디오로 STT 실행
    public IEnumerator TranscribeRecent(float windowSec, Action<string> onResult)
    {
        Debug.Log("[STT] TranscribeRecent 호출됨");

        // 디바운스 체크
        if (IsBusy || Time.unscaledTime - _lastAt < cooldownSec)
        {
            Debug.Log("[STT] 디바운스로 인해 스킵됨");
            yield break;
        }

        Debug.Log("[STT] 처리 시작");
        IsBusy = true;

        float ws = Mathf.Clamp(windowSec, 0.5f, mic != null ? mic.clipLengthSec : 5f);
        float[] samples = null;
        int sr = 16000;

        // 마이크 상태 확인
        if (mic == null)
        {
            Debug.LogError("[STT] MicrophoneCapture가 할당되지 않음");
            IsBusy = false;
            yield break;
        }

        Debug.Log($"[STT] 마이크 녹음 상태: {mic.isRecording}");

        if (mic && mic.isRecording)
        {
            samples = mic.GetRecentSamples(ws);
            sr = mic.sampleRate;
            Debug.Log($"[STT] 샘플 수집됨: {samples?.Length ?? 0} 개, 샘플레이트: {sr}");
        }
        else
        {
            Debug.LogWarning("[STT] 마이크가 녹음 중이 아니거나 null임");
        }

        Debug.Log("[STT] TranscribeSamples 호출 시작");
        yield return StartCoroutine(TranscribeSamples(samples, sr, onResult));

        _lastAt = Time.unscaledTime;
        IsBusy = false;
        Debug.Log("[STT] TranscribeRecent 완료");
    }

    // 샘플 배열을 직접 받아 변환(모의/실제 공용 경로)
    public IEnumerator TranscribeSamples(float[] samples, int sampleRate, Action<string> onResult)
    {
        if (useMock)
        {
            // 모의 처리: 약간 지연 후 샘플 문구 반환
            yield return new WaitForSeconds(mockLatency);

            string text = "아이스 아메리카노 한 잔이요";
            onResult?.Invoke(text);
            OnTranscriptReady?.Invoke(text);
            yield break;
        }

        // === 실제 ASR 연동 ===
        // 0) 키 확인
        string key = settings != null ? settings.ResolveKey() : Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogError("[STT] OpenAI API key is missing (asset & OPENAI_API_KEY).");
            yield break;
        }

        // 1) 입력 샘플 확보
        if (samples == null || samples.Length == 0)
        {
            Debug.LogWarning("[STT] 입력 샘플이 비어있습니다.");
            yield break;
        }
        if (sampleRate <= 0) sampleRate = 16000;

        // 2) float[] → WAV(PCM16) 메모리 인코딩
        byte[] wavBytes = FloatToWav(samples, sampleRate, channels: 1);

        // 3) URL 조립 (/v1 보정)
        string baseUrl = (settings != null ? settings.baseUrl : "https://api.openai.com/v1")?.TrimEnd('/');
        if (!baseUrl.EndsWith("/v1")) baseUrl += "/v1";
        string url = baseUrl + "/audio/transcriptions";

        // 4) multipart/form-data 구성 (response_format=text로 간단 반환)
        WWWForm form = new WWWForm();
        form.AddField("model", sttModel);
        form.AddField("response_format", "text");  // => 본문이 순수 텍스트로 옴
        if (!string.IsNullOrEmpty(language)) form.AddField("language", language);
        form.AddBinaryData("file", wavBytes, "speech.wav", "audio/wav");

        using (UnityWebRequest req = UnityWebRequest.Post(url, form))
        {
            req.SetRequestHeader("Authorization", "Bearer " + key);
            // 타임아웃
            int tmo = settings != null ? Mathf.Clamp(settings.timeoutSec, 5, 60) : 15;
            req.timeout = tmo;

            // 디버그 로깅
            if (settings != null && settings.logRequests)
                Debug.Log($"[STT] POST {url} (samples={samples.Length}, sr={sampleRate})");

            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool failed = req.result != UnityWebRequest.Result.Success;
#else
            bool failed = req.isHttpError || req.isNetworkError;
#endif
            if (failed)
            {
                Debug.LogError($"[STT] HTTP {(int)req.responseCode}: {req.error}\n{req.downloadHandler.text}");
                yield break;
            }

            // 5) 응답 파싱 (response_format=text 이므로 바로 본문이 결과)
            string text = req.downloadHandler.text;
            if (string.IsNullOrWhiteSpace(text))
            {
                Debug.LogWarning("[STT] 빈 응답을 수신했습니다.");
                yield break;
            }

            onResult?.Invoke(text);
            OnTranscriptReady?.Invoke(text);
        }
    }

    // 런타임에 마이크 주입하고 싶을 때(선택)
    public void SetMic(MicrophoneCapture m) => mic = m;

    // 이벤트만 쓰고 싶을 때: 콜백 없이 트리거하는 유틸(선택)
    public Coroutine TranscribeRecentFireAndForget(float windowSec)
    {
        return StartCoroutine(TranscribeRecent(windowSec, null));
    }

    // ===== Helpers =====

    // float PCM(-1..1) → WAV(PCM16, little-endian) 바이트 배열
    private static byte[] FloatToWav(float[] samples, int sampleRate, int channels)
    {
        if (channels <= 0) channels = 1;

        // PCM16으로 변환
        short[] pcm16 = new short[samples.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            float v = Mathf.Clamp(samples[i], -1f, 1f);
            pcm16[i] = (short)Mathf.RoundToInt(v * short.MaxValue);
        }

        int byteRate = sampleRate * channels * 2;            // 16bit = 2bytes
        int subchunk2Size = pcm16.Length * 2;
        int chunkSize = 36 + subchunk2Size;

        using (var ms = new System.IO.MemoryStream(44 + subchunk2Size))
        using (var bw = new System.IO.BinaryWriter(ms))
        {
            // RIFF 헤더
            bw.Write(Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(chunkSize);
            bw.Write(Encoding.ASCII.GetBytes("WAVE"));

            // fmt chunk
            bw.Write(Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);                 // PCM fmt chunk size
            bw.Write((short)1);           // audio format = PCM
            bw.Write((short)channels);    // channels
            bw.Write(sampleRate);         // sample rate
            bw.Write(byteRate);           // byte rate
            bw.Write((short)(channels * 2)); // block align
            bw.Write((short)16);          // bits per sample

            // data chunk
            bw.Write(Encoding.ASCII.GetBytes("data"));
            bw.Write(subchunk2Size);

            // PCM 데이터
            for (int i = 0; i < pcm16.Length; i++)
                bw.Write(pcm16[i]);

            return ms.ToArray();
        }
    }
}
