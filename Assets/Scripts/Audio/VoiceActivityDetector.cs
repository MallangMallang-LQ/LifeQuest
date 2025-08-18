using UnityEngine;
using System;

public interface IVoiceActivityDetector
{
    event Action OnSpeechStart;
    event Action OnSpeechEnd;
    event Action<float[]> OnSpeechFrame; // 말하는 동안 프레임 콜백(선택)
    bool IsSpeech { get; }
}

[DisallowMultipleComponent]
public class VoiceActivityDetector : MonoBehaviour, IVoiceActivityDetector
{
    [Header("Dependencies")]
    [SerializeField] private MicrophoneCapture mic;

    [Header("Frame")]
    [Tooltip("프레임 크기(ms). 10~30ms 권장")]
    [Range(5, 60)] public int frameMs = 20;

    [Header("Levels (dBFS)")]
    [Tooltip("스피치 인입 임계치(노이즈 플로어 대비). 예:+10dB")]
    public float enterOffsetDb = 10f;
    [Tooltip("스피치 이탈 임계치(히스테리시스). 예:+6dB")]
    public float exitOffsetDb = 6f;

    [Header("Adaptive Noise Floor")]
    [Tooltip("노이즈 플로어 EMA 계수(0~1, 클수록 느리게 갱신)")]
    [Range(0f, 1f)] public float noiseEma = 0.9f;
    [Tooltip("초기 노이즈 플로어(dBFS). 환경에 맞게 조정(-60~-45 권장)")]
    public float initialNoiseDb = -55f;
    [Tooltip("최소 허용 노이즈(dBFS) 상한(너무 높게 치솟지 않도록)")]
    public float maxNoiseDb = -35f;

    [Header("Timing (ms)")]
    [Tooltip("스피치 최소 지속 시간")]
    [Range(0, 1000)] public int minSpeechMs = 120;
    [Tooltip("스피치 종료 후 유지(행오버) 시간")]
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

        // 최근 프레임 길이만큼 샘플 가져오기
        var frame = mic.GetRecentSamples(frameMs / 1000f);
        if (frame == null || frame.Length == 0) return;

        float rms = CalcRms(frame);
        float db = RmsToDb(rms);

        // 노이즈 플로어 갱신(비발화 추정 구간에서 주로 하향/상향)
        AdaptNoise(db);

        bool aboveEnter = db >= _enterDb;
        bool belowExit = db <= _exitDb;

        if (!IsSpeech)
        {
            // 아직 미발화 → 진입 조건 체크(최소 지속시간 보장)
            if (aboveEnter)
            {
                if (_speechStartedAt <= 0f)
                    _speechStartedAt = Time.time;
                // 충분히 높게 유지됐으면 SpeechStart
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
            // 말하는 중
            if (!belowExit)
            {
                // 여전히 말하는 중
                _lastVoiceTime = Time.time;
                OnSpeechFrame?.Invoke(frame);
            }
            else
            {
                // 임계치 아래로 내려갔음 → 행오버 체크
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
        // 스피치가 아니거나(진입 전) 충분히 낮은 값이면 노이즈로 추정
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
        // dBFS: full-scale 1.0 기준
        const float eps = 1e-7f;
        return 20f * Mathf.Log10(Mathf.Max(rms, eps));
    }

    // 인스펙터에서 프레임 크기 바뀌면 샘플 수 재계산
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
