using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using LifeQuest.Integrations; // OpenAISettings

namespace LifeQuest.AI
{
    /// <summary>
    /// OpenAI Chat Completions (텍스트) 최소 클라이언트.
    /// - 코루틴: Chat(system, user, maxTokens, onOk, onError)
    /// - 실패/키없음/타임아웃 시 onError 호출
    /// </summary>
    public class OpenAIClient
    {
        private readonly OpenAISettings _s;
        public OpenAIClient(OpenAISettings settings) { _s = settings ?? throw new ArgumentNullException(nameof(settings)); }

        // --- Request/Response DTO ---
        [Serializable] class Msg { public string role; public string content; }
        [Serializable]
        class Req
        {
            public string model;
            public Msg[] messages;
            public int max_tokens;
            public float temperature;
        }
        [Serializable] class ChoiceMsg { public string role; public string content; }
        [Serializable] class Choice { public ChoiceMsg message; }
        [Serializable] class Resp { public Choice[] choices; }

        /// <summary>
        /// Chat Completions 요청. 성공 시 onOk(text) 호출.
        /// </summary>
        public IEnumerator Chat(
            string system, string user, int maxTokens,
            Action<string> onOk, Action<string> onError)
        {
            // 키 확인 (에셋값 → 환경변수 폴백)
            var key = _s.ResolveKey();
            if (string.IsNullOrEmpty(key))
            {
                onError?.Invoke("OpenAI API key is missing (asset & OPENAI_API_KEY).");
                yield break;
            }

            // URL 조합
            string baseUrl = _s.baseUrl.TrimEnd('/');
            if (!baseUrl.EndsWith("/v1"))
                baseUrl += "/v1";
            string url = baseUrl + "/chat/completions";

            // 요청 본문
            var reqObj = new Req
            {
                model = _s.model,
                messages = new[]
                {
                    new Msg{ role="system", content = system ?? string.Empty },
                    new Msg{ role="user",   content = user   ?? string.Empty }
                },
                max_tokens = Mathf.Clamp(maxTokens, 16, _s.maxTokens),
                temperature = Mathf.Clamp(_s.temperature, 0f, 2f)
            };
            string json = JsonUtility.ToJson(reqObj);

            if (_s.logRequests) Debug.Log($"[OpenAI] POST {url}\n{json}");

            using (var www = new UnityWebRequest(url, "POST"))
            {
                www.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("Authorization", "Bearer " + key);
                www.timeout = Mathf.Clamp(_s.timeoutSec, 5, 60);

                yield return www.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
                bool failed = www.result != UnityWebRequest.Result.Success;
#else
                bool failed = www.isHttpError || www.isNetworkError;
#endif
                if (failed)
                {
                    onError?.Invoke($"HTTP {(int)www.responseCode}: {www.error}\n{www.downloadHandler?.text}");
                    yield break;
                }

                string body = www.downloadHandler.text;
                string text = ParseFirstMessage(body);
                if (string.IsNullOrWhiteSpace(text))
                {
                    onError?.Invoke("Empty or unparsable response.");
                }
                else
                {
                    onOk?.Invoke(text.Trim());
                }
            }
        }

        // choices[0].message.content 파싱
        private static string ParseFirstMessage(string json)
        {
            try
            {
                var resp = JsonUtility.FromJson<Resp>(json);
                if (resp != null && resp.choices != null && resp.choices.Length > 0)
                    return resp.choices[0]?.message?.content;
            }
            catch { /* JsonUtility 파싱 실패 시 null */ }
            return null;
        }
    }
}
