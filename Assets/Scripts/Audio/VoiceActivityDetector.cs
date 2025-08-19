using UnityEngine;
using System;

public interface IVoiceActivityDetector
{
    event Action OnSpeechStart;
    event Action OnSpeechEnd;
    event Action<float[]> OnSpeechFrame; // ���ϴ� ���� ������ �ݹ�(����)
    bool IsSpeech { get; }
}

[DisallowMultipleComponent]
public class VoiceActivityDetector : MonoBehaviour, IVoiceActivityDetector
{
    [Header("Dependencies")]
    [SerializeField] private MicrophoneCapture mic;

    [Header("Frame")]
    [Tooltip("������ ũ��(ms). 10~30ms ����")]
    [Range(5, 60)] public int frameMs = 20;

    [Header("Levels (dBFS)")]
    [Tooltip("����ġ ���� �Ӱ�ġ(������ �÷ξ� ���). ��:+10dB")]
    public float enterOffsetDb = 10f;
    [Tooltip("����ġ ��Ż �Ӱ�ġ(�����׸��ý�). ��:+6dB")]
    public float exitOffsetDb = 6f;

    [Header("Adaptive Noise Floor")]
    [Tooltip("������ �÷ξ� EMA ���(0~1, Ŭ���� ������ ����)")]
    [Range(0f, 1f)] public float noiseEma = 0.9f;
    [Tooltip("�ʱ� ������ �÷ξ�(dBFS). ȯ�濡 �°� ����(-60~-45 ����)")]
    public float initialNoiseDb = -55f;
    [Tooltip("�ּ� ��� ������(dBFS) ����(�ʹ� ���� ġ���� �ʵ���)")]
    public float maxNoiseDb = -35f;

    [Header("Timing (ms)")]
    [Tooltip("����ġ �ּ� ���� �ð�")]
    [Range(0, 1000)] public int minSpeechMs = 120;
    [Tooltip("����ġ ���� �� ����(�����) �ð�")]
    [Range(0, 1000)] public int hangoverMs = 200;

    [Header("Debug")]
    public bool debugLog = false;

    // Runtime
    public bool IsSpeech { get; private set; }
    public event Action OnSpeechStart;
    public event Action OnSpeechEnd;
    public event Action<float[]> OnSpeechFrame;

    private float _noiseDb;
    private float _enterDb; // noise + enterOffset
    private float _exitDb;  // noise + exitOffset
    private int _sampleRate;
    private int _frameSamples;
    private float _speechStartedAt;   // Time.time
    private float _lastVoiceTime;     // Time.time

    void Start()
    {
        if (!mic)
        {
            Debug.LogWarning("[VAD] MicrophoneCapture not assigned.");
            enabled = false;
            return;
        }
        _sampleRate = Mathf.Max(8000, mic.sampleRate);
        _frameSamples = Mathf.Max(1, Mathf.RoundToInt(_sampleRate * (frameMs / 1000f)));
        _noiseDb = initialNoiseDb;
        RecomputeThresholds();
    }

    void Update()
    {
        if (!mic || !mic.isRecording) return;

        // �ֱ� ������ ���̸�ŭ ���� ��������
        var frame = mic.GetRecentSamples(frameMs / 1000f);
        if (frame == null || frame.Length == 0) return;

        float rms = CalcRms(frame);
        float db = RmsToDb(rms);

        // ������ �÷ξ� ����(���ȭ ���� �������� �ַ� ����/����)
        AdaptNoise(db);

        bool aboveEnter = db >= _enterDb;
        bool belowExit = db <= _exitDb;

        if (!IsSpeech)
        {
            // ���� �̹�ȭ �� ���� ���� üũ(�ּ� ���ӽð� ����)
            if (aboveEnter)
            {
                if (_speechStartedAt <= 0f)
                    _speechStartedAt = Time.time;
                // ����� ���� ���������� SpeechStart
                if ((Time.time - _speechStartedAt) * 1000f >= minSpeechMs)
                {
                    IsSpeech = true;
                    _lastVoiceTime = Time.time;
                    OnSpeechStart?.Invoke();
                    if (debugLog) Debug.Log("[VAD] SpeechStart");
                }
            }
            else
            {
                _speechStartedAt = 0f;
            }
        }
        else
        {
            // ���ϴ� ��
            if (!belowExit)
            {
                // ������ ���ϴ� ��
                _lastVoiceTime = Time.time;
                OnSpeechFrame?.Invoke(frame);
            }
            else
            {
                // �Ӱ�ġ �Ʒ��� �������� �� ����� üũ
                if (((Time.time - _lastVoiceTime) * 1000f) >= hangoverMs)
                {
                    IsSpeech = false;
                    _speechStartedAt = 0f;
                    OnSpeechEnd?.Invoke();
                    if (debugLog) Debug.Log("[VAD] SpeechEnd");
                }
            }
        }
    }

    private void AdaptNoise(float db)
    {
        // ����ġ�� �ƴϰų�(���� ��) ����� ���� ���̸� ������� ����
        bool updateNoise = !IsSpeech || db < _noiseDb + 2f;
        if (updateNoise)
        {
            _noiseDb = Mathf.Min(
                maxNoiseDb,
                noiseEma * _noiseDb + (1f - noiseEma) * db
            );
            RecomputeThresholds();
        }
    }

    private void RecomputeThresholds()
    {
        _enterDb = _noiseDb + enterOffsetDb;
        _exitDb = _noiseDb + exitOffsetDb;
    }

    private static float CalcRms(float[] frame)
    {
        double sum = 0;
        for (int i = 0; i < frame.Length; i++)
        {
            float s = frame[i];
            sum += s * s;
        }
        return (float)System.Math.Sqrt(sum / frame.Length);
    }

    private static float RmsToDb(float rms)
    {
        // dBFS: full-scale 1.0 ����
        const float eps = 1e-7f;
        return 20f * Mathf.Log10(Mathf.Max(rms, eps));
    }

    // �ν����Ϳ��� ������ ũ�� �ٲ�� ���� �� ����
#if UNITY_EDITOR
    void OnValidate()
    {
        if (mic != null)
        {
            _sampleRate = Mathf.Max(8000, mic.sampleRate);
            _frameSamples = Mathf.Max(1, Mathf.RoundToInt(_sampleRate * (frameMs / 1000f)));
        }
        RecomputeThresholds();
    }
#endif
}
