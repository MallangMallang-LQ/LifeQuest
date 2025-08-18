using System;
using UnityEngine;

namespace LifeQuest.Integrations
{
    /// <summary>
    /// OpenAI 호출 설정(ScriptableObject). 에셋 하나 만들어서 씬/프로젝트 전역으로 사용.
    /// </summary>
    [CreateAssetMenu(menuName = "LifeQuest/OpenAI Settings")]
    public class OpenAISettings : ScriptableObject
    {
        [Header("Auth & Endpoint")]
        [Tooltip("빈 경우 환경변수 OPENAI_API_KEY를 사용합니다.")]
        public string apiKey;

        // OpenAI 기본 엔드포인트 (v1까지 포함)
        [Tooltip("예: https://api.openai.com/v1")]
        public string baseUrl = "https://api.openai.com/v1";

        [Header("Inference")]
        [Tooltip("예: gpt-4o-mini")]
        public string model = "gpt-4o-mini";

        [Range(0f, 2f)]
        public float temperature = 0.3f;

        [Tooltip("응답 토큰 상한")]
        [Range(16, 256)]
        public int maxTokens = 120;

        [Header("Timeout")]
        [Tooltip("HTTP 타임아웃(초)")]
        [Range(5, 60)]
        public int timeoutSec = 15;

        [Header("Debug")]
        [Tooltip("요청 JSON을 콘솔에 출력")]
        public bool logRequests = false;

        /// <summary>에셋에 키가 없으면 OS 환경변수(OPENAI_API_KEY)로 대체.</summary>
        public string ResolveKey()
        {
            if (!string.IsNullOrEmpty(apiKey)) return apiKey;
            try { return Environment.GetEnvironmentVariable("OPENAI_API_KEY"); }
            catch { return null; }
        }
    }
}
