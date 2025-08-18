using System;
using System.Collections;
using UnityEngine;

namespace LifeQuest.AI
{
    /// <summary>
    /// LLM 출력 안전/비용 가드 + 재시도 래퍼.
    /// - 템플릿 폴백, 금칙어 필터, 토큰/비용 캡, 지수 백오프 재시도
    /// </summary>
    public static class SafetyGuard
    {
        // ===== 0) 출력 필터 =====
        public static bool ContainsBanned(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            var t = text.ToLowerInvariant();
            // TODO: 실제 금칙어 목록 확장
            return t.Contains("욕설1") || t.Contains("금칙2");
        }
        public static string FilterOrFallback(string text, string fallback) =>
            string.IsNullOrWhiteSpace(text) || ContainsBanned(text) ? fallback : text;

#if LIFEQUEST_AI
        // ===== 1) 간이 토큰 추정 & 세션 비용 추적 =====
        static class TokenEstimator
        {
            // 매우 러프: 한글 기준 문자수 * 0.6 ≈ 토큰
            public static int Estimate(string s)
            {
                if (string.IsNullOrEmpty(s)) return 0;
                return Mathf.Clamp(Mathf.CeilToInt(s.Length * 0.6f), 0, 128_000);
            }
        }

        static class UsageTracker
        {
            public static float SessionCostUSD { get; private set; }
            public static void Add(float usd) => SessionCostUSD += Mathf.Max(0f, usd);
            public static void Reset() => SessionCostUSD = 0f;
        }

        // ===== 2) 프리플라이트(토큰/비용 캡 확인) =====
        public static bool Preflight(
            string system, string user,
            int modelMaxOutTokens,
            out string reason,
            // 캡/버짓(없으면 기본값)
            int   maxInputTokPerTurn    = 1000,
            int   maxOutputTokPerTurn   = 180,
            float priceInPer1k          = 0.005f,
            float priceOutPer1k         = 0.015f,
            float maxCostPerTurnUSD     = 0.02f,
            float maxCostPerSessionUSD  = 0.50f
        )
        {
            int inTok  = TokenEstimator.Estimate(system) + TokenEstimator.Estimate(user);
            int outCap = Mathf.Min(Mathf.Max(1, modelMaxOutTokens), Mathf.Max(1, maxOutputTokPerTurn));

            if (inTok > maxInputTokPerTurn)
            {
                reason = $"Input tokens over cap ({inTok}>{maxInputTokPerTurn})";
                return false;
            }

            float turnCost = (inTok / 1000f) * priceInPer1k + (outCap / 1000f) * priceOutPer1k;
            if (turnCost > maxCostPerTurnUSD)
            {
                reason = $"Turn cost over cap (${turnCost:0.000}>${maxCostPerTurnUSD:0.000})";
                return false;
            }
            if (UsageTracker.SessionCostUSD + turnCost > maxCostPerSessionUSD)
            {
                reason = $"Session cost over cap (${UsageTracker.SessionCostUSD + turnCost:0.000}>{maxCostPerSessionUSD:0.000})";
                return false;
            }

            reason = null;
            return true;
        }

        // ===== 3) 정책 적용 호출(재시도+폴백) =====
        /// <summary>
        /// LLM 호출을 안전하게 감싸서 실행. 실패/금칙/캡 초과/타임아웃 시 fallback으로 폴백.
        /// </summary>
        public static IEnumerator GenerateWithPolicy(
            OpenAIClient client,
            string system, string user, string fallback,
            Action<string> onText,
            // 모델/설정 파라미터
            int   modelMaxOutTokens     = 120,
            int   timeoutSec            = 15,   // (참고값: 실제 타임아웃은 OpenAISettings에서 적용)
            // 캡/버짓(없으면 기본값)
            int   maxInputTokPerTurn    = 1000,
            int   maxOutputTokPerTurn   = 180,
            float priceInPer1k          = 0.005f,
            float priceOutPer1k         = 0.015f,
            float maxCostPerTurnUSD     = 0.02f,
            float maxCostPerSessionUSD  = 0.50f,
            // 재시도
            int   maxRetries            = 1,
            float backoffBaseS          = 0.5f
        )
        {
            // 입력 금칙어/과도 길이/비용 사전 점검
            if (!Preflight(system, user, modelMaxOutTokens, out var reason,
                           maxInputTokPerTurn, maxOutputTokPerTurn,
                           priceInPer1k, priceOutPer1k,
                           maxCostPerTurnUSD, maxCostPerSessionUSD))
            {
                Debug.LogWarning($"[AI] Preflight block: {reason}");
                onText?.Invoke(fallback);
                yield break;
            }

            int    tries   = 0;
            string lastErr = null;

            while (tries <= maxRetries)
            {
                bool   finished = false;
                string result   = null;
                string err      = null;

                // OpenAIClient 최신 시그니처(Chat) 사용
                yield return client.Chat(
                    system, user, modelMaxOutTokens,
                    onOk:   t => { finished = true; result = t; },
                    onError:e => { finished = true; err    = e; }
                );

                if (err == null && !string.IsNullOrWhiteSpace(result))
                {
                    // 비용 누적(추정)
                    int inTok  = TokenEstimator.Estimate(system) + TokenEstimator.Estimate(user);
                    int outTok = TokenEstimator.Estimate(result);
                    float cost = (inTok / 1000f) * priceInPer1k + (outTok / 1000f) * priceOutPer1k;
                    UsageTracker.Add(cost);

                    // 출력 필터 후 전달
                    onText?.Invoke(FilterOrFallback(result.Trim(), fallback));
                    yield break;
                }

                lastErr = err ?? "unknown error";
                tries++;
                if (tries <= maxRetries)
                    yield return new WaitForSeconds(backoffBaseS * tries); // 0.5s, 1.0s, ...
            }

            Debug.LogWarning($"[AI] Retry exhausted: {lastErr}");
            onText?.Invoke(fallback);
        }
#endif
    }
}
