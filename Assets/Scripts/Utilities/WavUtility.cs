using UnityEngine;
using System;
using System.IO;

/// <summary>
/// byte[] WAV → Unity AudioClip 변환 유틸리티 (간단 버전)
/// </summary>
public static class WavUtility
{
    public static AudioClip ToAudioClip(byte[] wavData, string clipName = "tts_clip")
    {
        if (wavData == null || wavData.Length == 0)
        {
            Debug.LogError("[WavUtility] 입력 데이터가 비어있음.");
            return null;
        }

        try
        {
            using (var ms = new MemoryStream(wavData))
            using (var reader = new BinaryReader(ms))
            {
                // WAV 헤더 검사
                string riff = new string(reader.ReadChars(4));
                if (riff != "RIFF")
                {
                    Debug.LogError("[WavUtility] WAV 헤더(RIFF) 아님");
                    return null;
                }

                reader.ReadInt32(); // 파일 전체 크기
                string wave = new string(reader.ReadChars(4));
                if (wave != "WAVE")
                {
                    Debug.LogError("[WavUtility] WAV 포맷(WAVE) 아님");
                    return null;
                }

                // fmt chunk
                string fmtID = new string(reader.ReadChars(4));
                int fmtSize = reader.ReadInt32();
                int audioFormat = reader.ReadInt16();
                int channels = reader.ReadInt16();
                int sampleRate = reader.ReadInt32();
                reader.ReadInt32(); // byteRate
                reader.ReadInt16(); // blockAlign
                int bitsPerSample = reader.ReadInt16();

                // fmt chunk 남은 부분 skip
                if (fmtSize > 16)
                    reader.ReadBytes(fmtSize - 16);

                // data chunk 찾기
                string dataID = new string(reader.ReadChars(4));
                while (dataID != "data")
                {
                    int size = reader.ReadInt32();
                    reader.ReadBytes(size);
                    dataID = new string(reader.ReadChars(4));
                }

                int dataSize = reader.ReadInt32();
                byte[] pcm = reader.ReadBytes(dataSize);

                // PCM → float[]
                int bytesPerSample = bitsPerSample / 8;
                int sampleCount = dataSize / bytesPerSample;
                float[] samples = new float[sampleCount];

                for (int i = 0; i < sampleCount; i++)
                {
                    short val = BitConverter.ToInt16(pcm, i * bytesPerSample);
                    samples[i] = val / 32768f;
                }

                var clip = AudioClip.Create(clipName, sampleCount / channels, channels, sampleRate, false);
                clip.SetData(samples, 0);
                return clip;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[WavUtility] 변환 실패: {ex.Message}");
            return null;
        }
    }
}
