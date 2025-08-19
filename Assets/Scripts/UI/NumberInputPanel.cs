using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LifeQuest.NPCFlow.UI
{
    /// <summary>
    /// 숫자 전용 입력 패널(전화/현금영수증 번호). Show/Hide로 제어.
    /// </summary>
    public class NumberInputPanel : MonoBehaviour
    {
        [Header("Refs")]
        public CanvasGroup root;               // 패널 루트(페이드용)
        public TextMeshProUGUI titleText;      // 상단 타이틀
        public TMP_InputField input;           // 숫자 전용
        public Button okButton;
        public Button cancelButton;

        [Header("Options")]
        public int maxDigits = 11;
        public bool maskDigits = false;        // * 마스킹 표시

        public event Action<string> OnSubmitted;
        public event Action OnCanceled;

        bool _visible;
        string _raw = "";

        void Awake()
        {
            if (input)
            {
                input.contentType = TMP_InputField.ContentType.Standard; // 직접 필터링
                input.onValueChanged.AddListener(OnChanged);
            }
            if (okButton) okButton.onClick.AddListener(Submit);
            if (cancelButton) cancelButton.onClick.AddListener(Cancel);

            SetVisible(false, instant: true);
        }

        public void Show(string title, int maxLen = 11, bool mask = true, string preset = "")
        {
            titleText?.SetText(string.IsNullOrEmpty(title) ? "번호 입력" : title);
            maxDigits = Mathf.Max(1, maxLen);
            maskDigits = mask;
            _raw = DigitsOnly(preset);
            input.text = Display(_raw);
            SetVisible(true);
        }

        public void Hide() => SetVisible(false);

        void OnChanged(string _)
        {
            // 숫자만 유지 + 길이 제한
            _raw = DigitsOnly(input.text);
            if (_raw.Length > maxDigits) _raw = _raw.Substring(0, maxDigits);
            input.SetTextWithoutNotify(Display(_raw));
            okButton.interactable = _raw.Length >= 8; // 대충 8자리 이상일 때 활성화
        }

        void Submit()
        {
            if (string.IsNullOrEmpty(_raw)) return;
            OnSubmitted?.Invoke(_raw);
            Hide();
        }

        void Cancel()
        {
            OnCanceled?.Invoke();
            Hide();
        }

        string DigitsOnly(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            System.Text.StringBuilder sb = new();
            foreach (char c in s) if (char.IsDigit(c)) sb.Append(c);
            return sb.ToString();
        }

        string Display(string digits)
        {
            if (!maskDigits) return digits;
            return new string('*', digits.Length);
        }

        void SetVisible(bool on, bool instant = false)
        {
            _visible = on;
            if (!root) { gameObject.SetActive(on); return; }
            StopAllCoroutines();
            if (instant) { root.alpha = on ? 1f : 0f; root.blocksRaycasts = on; root.interactable = on; gameObject.SetActive(on); return; }
            if (on) gameObject.SetActive(true);
            StartCoroutine(Fade(on ? 0f : 1f, on ? 1f : 0f, 0.12f, () =>
            {
                root.blocksRaycasts = on; root.interactable = on;
                if (!on) gameObject.SetActive(false);
            }));
        }

        System.Collections.IEnumerator Fade(float a, float b, float t, Action done)
        {
            float e = 0;
            while (e < t) { e += Time.unscaledDeltaTime; root.alpha = Mathf.Lerp(a, b, e / t); yield return null; }
            root.alpha = b; done?.Invoke();
        }
    }
}
