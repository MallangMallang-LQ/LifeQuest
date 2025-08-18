using System.Collections;
using TMPro;
using UnityEngine;

namespace LifeQuest.NPCFlow.UI
{
    /// <summary>짧은 안내 토스트(겹치면 마지막 문구로 덮고 재시작).</summary>
    public class Toast : MonoBehaviour
    {
        public CanvasGroup root;
        public TextMeshProUGUI text;
        public float fade = 0.15f;

        Coroutine _co;

        public void Show(string msg, float seconds = 1.2f)
        {
            if (!root || !text) return;
            text.text = msg ?? "";
            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(Run(seconds));
        }

        IEnumerator Run(float seconds)
        {
            yield return FadeTo(1f);
            yield return new WaitForSeconds(seconds);
            yield return FadeTo(0f);
        }

        IEnumerator FadeTo(float target)
        {
            float t = 0, start = root.alpha;
            while (t < fade) { t += Time.unscaledDeltaTime; root.alpha = Mathf.Lerp(start, target, t / fade); yield return null; }
            root.alpha = target;
        }
    }
}
