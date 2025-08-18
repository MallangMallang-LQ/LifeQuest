using UnityEngine;
using System;
using System.Collections;
using System.Text;
using UnityEngine.Networking;
using LifeQuest.Integrations; // OpenAISettings(ScriptableObject)

public interface ISpeechToText
{
    // �ֱ� windowSec ��ŭ ����ũ ���۸� ����� ��ȯ(����/����). �Ϸ� �� onResult ȣ��.
    IEnumerator TranscribeRecent(float windowSec, Action<string> onResult);

    // ���� ������ ���� �Ѱ� ��ȯ(����/����).
    IEnumerator TranscribeSamples(float[] samples, int sampleRate, Action<string> onResult);

    bool IsBusy { get; }
}

[DisallowMultipleComponent]
public class SpeechToTextService : MonoBehaviour, ISpeechToText
{
    [Header("Deps")]
    [SerializeField] private MicrophoneCapture mic;          // Inspector���� �巡��
    [SerializeField] private OpenAISettings settings;        // baseUrl/key/model/timeouts ��

    [Header("Mode")]
    public bool useMock = true;

    [Header("Timing (sec)")]
    [Tooltip("���� ���۷��� ���� �ð�")]
    public float mockLatency = 0.35f;
    [Tooltip("���� ȣ�� ��ٿ�(��ٿ)")]
    public float cooldownSec = 0.30f;

    [Header("ASR Options")]
    [Tooltip("OpenAI STT �𵨸� (��: whisper-1)")]
    public string sttModel = "whisper-1";
    [Tooltip("��� ��Ʈ (��: ko, en ��)")]
    public string language = "ko";

    public event Action<string> OnTranscriptReady;

    public bool IsBusy { get; private set; }
    private float _lastAt;

    // ���� ������: �ֱ� windowSec ������� STT ����
    public IEnumerator TranscribeRecent(float windowSec, Action<string> onResult)
    {
        Debug.Log("[STT] TranscribeRecent ȣ���");

        // ��ٿ üũ
        if (IsBusy || Time.unscaledTime - _lastAt < cooldownSec)
        {
            Debug.Log("[STT] ��ٿ�� ���� ��ŵ��");
            yield break;
        }

        Debug.Log("[STT] ó�� ����");
        IsBusy = true;

        float ws = Mathf.Clamp(windowSec, 0.5f, mic != null ? mic.clipLengthSec : 5f);
        float[] samples = null;
        int sr = 16000;

        // ����ũ ���� Ȯ��
        if (mic == null)
        {
            Debug.LogError("[STT] MicrophoneCapture�� �Ҵ���� ����");
            IsBusy = false;
            yield break;
        }

        Debug.Log($"[STT] ����ũ ���� ����: {mic.isRecording}");

        if (mic && mic.isRecording)
        {
            samples = mic.GetRecentSamples(ws);
            sr = mic.sampleRate;
            Debug.Log($"[STT] ���� ������: {samples?.Length ?? 0} ��, ���÷���Ʈ: {sr}");
        }
        else
        {
            Debug.LogWarning("[STT] ����ũ�� ���� ���� �ƴϰų� null��");
        }

        Debug.Log("[STT] TranscribeSamples ȣ�� ����");
        yield return StartCoroutine(TranscribeSamples(samples, sr, onResult));

        _lastAt = Time.unscaledTime;
        IsBusy = false;
        Debug.Log("[STT] TranscribeRecent �Ϸ�");
    }

    // ���� �迭�� ���� �޾� ��ȯ(����/���� ���� ���)
    public IEnumerator TranscribeSamples(float[] samples, int sampleRate, Action<string> onResult)
    {
        if (useMock)
        {
            // ���� ó��: �ణ ���� �� ���� ���� ��ȯ
            yield return new WaitForSeconds(mockLatency);

            string text = "���̽� �Ƹ޸�ī�� �� ���̿�";
            onResult?.Invoke(text);
            OnTranscriptReady?.Invoke(text);
            yield break;
        }

        // === ���� ASR ���� ===
        // 0) Ű Ȯ��
        string key = settings != null ? settings.ResolveKey() : Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogError("[STT] OpenAI API key is missing (asset & OPENAI_API_KEY).");
            yield break;
        }

        // 1) �Է� ���� Ȯ��
        if (samples == null || samples.Length == 0)
        {
            Debug.LogWarning("[STT] �Է� ������ ����ֽ��ϴ�.");
            yield break;
        }
        if (sampleRate <= 0) sampleRate = 16000;

        // 2) float[] �� WAV(PCM16) �޸� ���ڵ�
        byte[] wavBytes = FloatToWav(samples, sampleRate, channels: 1);

        // 3) URL ���� (/v1 ����)
        string baseUrl = (settings != null ? settings.baseUrl : "https://api.openai.com/v1")?.TrimEnd('/');
        if (!baseUrl.EndsWith("/v1")) baseUrl += "/v1";
        string url = baseUrl + "/audio/transcriptions";

        // 4) multipart/form-data ���� (response_format=text�� ���� ��ȯ)
        WWWForm form = new WWWForm();
        form.AddField("model", sttModel);
        form.AddField("response_format", "text");  // => ������ ���� �ؽ�Ʈ�� ��
        if (!string.IsNullOrEmpty(language)) form.AddField("language", language);
        form.AddBinaryData("file", wavBytes, "speech.wav", "audio/wav");

        using (UnityWebRequest req = UnityWebRequest.Post(url, form))
        {
            req.SetRequestHeader("Authorization", "Bearer " + key);
            // Ÿ�Ӿƿ�
            int tmo = settings != null ? Mathf.Clamp(settings.timeoutSec, 5, 60) : 15;
            req.timeout = tmo;

            // ����� �α�
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

            // 5) ���� �Ľ� (response_format=text �̹Ƿ� �ٷ� ������ ���)
            string text = req.downloadHandler.text;
            if (string.IsNullOrWhiteSpace(text))
            {
                Debug.LogWarning("[STT] �� ������ �����߽��ϴ�.");
                yield break;
            }

            onResult?.Invoke(text);
            OnTranscriptReady?.Invoke(text);
        }
    }

    // ��Ÿ�ӿ� ����ũ �����ϰ� ���� ��(����)
    public void SetMic(MicrophoneCapture m) => mic = m;

    // �̺�Ʈ�� ���� ���� ��: �ݹ� ���� Ʈ�����ϴ� ��ƿ(����)
    public Coroutine TranscribeRecentFireAndForget(float windowSec)
    {
        return StartCoroutine(TranscribeRecent(windowSec, null));
    }

    // ===== Helpers =====

    // float PCM(-1..1) �� WAV(PCM16, little-endian) ����Ʈ �迭
    private static byte[] FloatToWav(float[] samples, int sampleRate, int channels)
    {
        if (channels <= 0) channels = 1;

        // PCM16���� ��ȯ
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
            // RIFF ���
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

            // PCM ������
            for (int i = 0; i < pcm16.Length; i++)
                bw.Write(pcm16[i]);

            return ms.ToArray();
        }
    }
}
