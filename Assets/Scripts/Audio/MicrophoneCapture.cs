using System;
using System.Linq;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

/// <summary>
/// ����ũ �Է��� ��ȯ ����(AudioClip)�� Ȯ���ϰ�,
/// �ֱ� N�� PCM�� �߶󳻾� STT�� �ѱ� �� �ְ� ���ִ� ��ƿ.
/// </summary>
public class MicrophoneCapture : MonoBehaviour
{
    [Header("Mic Settings")]
    [Tooltip("���� ù ��° ��ġ ���")]
    public string deviceName = "";
    [Tooltip("ASR ģȭ ���÷���Ʈ(16k~24k ����)")]
    public int sampleRate = 16000;
    [Range(1, 30)]
    [Tooltip("��ȯ ���� ����(��)")]
    public int clipLengthSec = 10;

    [Header("Monitor (����)")]
    [Tooltip("����͸�(���� ����)")]
    public bool loopbackMonitor = false;
    public AudioSource monitorSource; // ���� ����

    [Header("Runtime (�б�����)")]
    [ReadOnly] public bool isRecording;
    [ReadOnly] public AudioClip micClip;
    [ReadOnly] public string resolvedDevice;
    [ReadOnly] public int lastMicPosition;

    // ���� ǥ�ÿ� �̺�Ʈ(����)
    public event Action<float> OnLevel; // 0~1 RMS

    void OnEnable()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
            // ���� �ݹ� ���� ���� ������ �õ�: �Ʒ� StartCapture���� ��ġ ������ �Ǵ�
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

        // �Ϻ� ����̽��� 16k ������ �� ���� ������ �ڵ� ����
        int minFreq, maxFreq;
        Microphone.GetDeviceCaps(resolvedDevice, out minFreq, out maxFreq);
        if (minFreq == 0 && maxFreq == 0)
        {
            // ĸ�� ���� �� �� �� ��û�� �״�� �õ�
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

        // ����͸�(����)
        if (loopbackMonitor && monitorSource != null)
        {
            monitorSource.clip = micClip;
            monitorSource.loop = true;
            // ����ũ ���� ������ 0 �̺��� �� �����ϰ� �÷��� ����
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

        // ���� RMS ���� ����(�ֱ� 0.1��)
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
    /// �ֱ� seconds ��ŭ�� ������ �����ؼ� ��ȯ. (wrap-around ó��)
    /// </summary>
    public float[] GetRecentSamples(float seconds)
    {
        if (!isRecording || micClip == null) return null;
        seconds = Mathf.Clamp(seconds, 0.01f, clipLengthSec);

        int need = Mathf.CeilToInt(sampleRate * seconds);
        int micPos = Microphone.GetPosition(resolvedDevice);
        if (micPos < 0) return null;

        float[] buffer = new float[need];

        // �б� ���� �ε���(��ȯ)
        int start = micPos - need;
        if (start < 0) start += micClip.samples;

        // �� �������� ���� �б�(�� ó��)
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
    /// �ֱ� seconds�� 16-bit PCM little-endian ����Ʈ�� ��ȯ(HTTP ���ε� � ���).
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