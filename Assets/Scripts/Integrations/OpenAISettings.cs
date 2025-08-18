using System;
using UnityEngine;

namespace LifeQuest.Integrations
{
    /// <summary>
    /// OpenAI ȣ�� ����(ScriptableObject). ���� �ϳ� ���� ��/������Ʈ �������� ���.
    /// </summary>
    [CreateAssetMenu(menuName = "LifeQuest/OpenAI Settings")]
    public class OpenAISettings : ScriptableObject
    {
        [Header("Auth & Endpoint")]
        [Tooltip("�� ��� ȯ�溯�� OPENAI_API_KEY�� ����մϴ�.")]
        public string apiKey;

        // OpenAI �⺻ ��������Ʈ (v1���� ����)
        [Tooltip("��: https://api.openai.com/v1")]
        public string baseUrl = "https://api.openai.com/v1";

        [Header("Inference")]
        [Tooltip("��: gpt-4o-mini")]
        public string model = "gpt-4o-mini";

        [Range(0f, 2f)]
        public float temperature = 0.3f;

        [Tooltip("���� ��ū ����")]
        [Range(16, 256)]
        public int maxTokens = 120;

        [Header("Timeout")]
        [Tooltip("HTTP Ÿ�Ӿƿ�(��)")]
        [Range(5, 60)]
        public int timeoutSec = 15;

        [Header("Debug")]
        [Tooltip("��û JSON�� �ֿܼ� ���")]
        public bool logRequests = false;

        /// <summary>���¿� Ű�� ������ OS ȯ�溯��(OPENAI_API_KEY)�� ��ü.</summary>
        public string ResolveKey()
        {
            if (!string.IsNullOrEmpty(apiKey)) return apiKey;
            try { return Environment.GetEnvironmentVariable("OPENAI_API_KEY"); }
            catch { return null; }
        }
    }
}
