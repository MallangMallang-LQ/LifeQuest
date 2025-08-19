using System;
using System.Linq;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

/// <summary>
/// 마이크 입력을 순환 버퍼(AudioClip)로 확보하고,
/// 최근 N초 PCM을 잘라내어 STT에 넘길 수 있게 해주는 유틸.
/// </summary>
public class MicrophoneCapture : MonoBehaviour
{
    [Header("Mic Settings")]
    [Tooltip("비우면 첫 번째 장치 사용")]
    public string deviceName = "";
    [Tooltip("ASR 친화 샘플레이트(16k~24k 권장)")]
    public int sampleRate = 16000;
    [Range(1, 30)]
    [Tooltip("순환 버퍼 길이(초)")]
    public int clipLengthSec = 10;

    [Header("Monitor (선택)")]
    [Tooltip("모니터링(에코 위험)")]
    public bool loopbackMonitor = false;
    public AudioSource monitorSource; // 선택 연결

    [Header("Runtime (읽기전용)")]
    [ReadOnly] public bool isRecording;
    [ReadOnly] public AudioClip micClip;
    [ReadOnly] public string resolvedDevice;
    [ReadOnly] public int lastMicPosition;

    // 레벨 표시용 이벤트(선택)
    public event Action<float> OnLevel; // 0~1 RMS

    void OnEnable()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
            // 승인 콜백 따로 없이 재입장 시도: 아래 StartCapture에서 장치 유무로 판단
        }
#endif
        StartCapture();
    }

    void OnDisable()
    {
        StopCapture();
    }

    public void StartCapture()
    {
        if (isRecording) return;

        if (Microphone.devices == null || Microphone.devices.Length == 0)
        {
            Debug.LogWarning("[Mic] No microphone devices.");
            return;
        }

        resolvedDevice = string.IsNullOrEmpty(deviceName) ? Microphone.devices.First() : deviceName;

        // 일부 디바이스는 16k 미지원 → 지원 범위로 자동 보정
        int minFreq, maxFreq;
        Microphone.GetDeviceCaps(resolvedDevice, out minFreq, out maxFreq);
        if (minFreq == 0 && maxFreq == 0)
        {
            // 캡스 제공 안 함 → 요청값 그대로 시도
        }
        else
        {
            if (sampleRate < minFreq) sampleRate = minFreq;
            if (maxFreq != 0 && sampleRate > maxFreq) sampleRate = maxFreq;
        }

        micClip = Microphone.Start(resolvedDevice, true, clipLengthSec, sampleRate);
        if (micClip == null)
        {
            Debug.LogWarning("[Mic] Start failed.");
            return;
        }

        // 모니터링(선택)
        if (loopbackMonitor && monitorSource != null)
        {
            monitorSource.clip = micClip;
            monitorSource.loop = true;
            // 마이크 시작 포지션 0 미보장 → 안전하게 플레이 지연
            Invoke(nameof(StartMonitorPlayback), 0.1f);
        }

        isRecording = true;
        lastMicPosition = 0;
        Debug.Log($"[Mic] Start: {resolvedDevice}, {sampleRate}Hz, {clipLengthSec}s loop");
    }

    void StartMonitorPlayback()
    {
        if (monitorSource != null && micClip != null)
            monitorSource.Play();
    }

    public void StopCapture()
    {
        if (!isRecording) return;
        if (!string.IsNullOrEmpty(resolvedDevice))
            Microphone.End(resolvedDevice);
        if (monitorSource != null && monitorSource.isPlaying)
            monitorSource.Stop();

        isRecording = false;
        micClip = null;
        Debug.Log("[Mic] Stopped.");
    }

    void Update()
    {
        if (!isRecording || micClip == null) return;

        // 간단 RMS 레벨 산출(최근 0.1초)
        float[] small = GetRecentSamples(0.1f);
        if (small != null && small.Length > 0)
        {
            float sum = 0f;
            for (int i = 0; i < small.Length; i++) sum += small[i] * small[i];
            float rms = Mathf.Sqrt(sum / small.Length);
            OnLevel?.Invoke(rms);
        }
    }

    /// <summary>
    /// 최근 seconds 만큼의 샘플을 복사해서 반환. (wrap-around 처리)
    /// </summary>
    public float[] GetRecentSamples(float seconds)
    {
        if (!isRecording || micClip == null) return null;
        seconds = Mathf.Clamp(seconds, 0.01f, clipLengthSec);

        int need = Mathf.CeilToInt(sampleRate * seconds);
        int micPos = Microphone.GetPosition(resolvedDevice);
        if (micPos < 0) return null;

        float[] buffer = new float[need];

        // 읽기 시작 인덱스(순환)
        int start = micPos - need;
        if (start < 0) start += micClip.samples;

        // 두 구간으로 나눠 읽기(랩 처리)
        int first = Mathf.Min(need, micClip.samples - start);
        if (!micClip.GetData(buffer, start)) return null;

        if (need > first)
        {
            float[] tail = new float[need - first];
            if (!micClip.GetData(tail, 0)) return null;
            Array.Copy(tail, 0, buffer, first, tail.Length);
        }

        lastMicPosition = micPos;
        return buffer;
    }

    /// <summary>
    /// 최근 seconds를 16-bit PCM little-endian 바이트로 변환(HTTP 업로드 등에 사용).
    /// </summary>
    public byte[] GetRecentPcm16le(float seconds, float gain = 1f)
    {
        var samples = GetRecentSamples(seconds);
        if (samples == null) return null;

        int len = samples.Length;
        byte[] bytes = new byte[len * 2];
        int bi = 0;
        for (int i = 0; i < len; i++)
        {
            float s = Mathf.Clamp(samples[i] * gain, -1f, 1f);
            short v = (short)Mathf.RoundToInt(s * short.MaxValue);
            bytes[bi++] = (byte)(v & 0xFF);
            bytes[bi++] = (byte)((v >> 8) & 0xFF);
        }
        return bytes;
    }
}