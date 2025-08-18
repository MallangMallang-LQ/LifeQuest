using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LifeQuest.NPCFlow.UI
{
    /// <summary>
    /// ���� ���� �Է� �г�(��ȭ/���ݿ����� ��ȣ). Show/Hide�� ����.
    /// </summary>
    public class NumberInputPanel : MonoBehaviour
    {
        [Header("Refs")]
        public CanvasGroup root;               // �г� ��Ʈ(���̵��)
        public TextMeshProUGUI titleText;      // ��� Ÿ��Ʋ
        public TMP_InputField input;           // ���� ����
        public Button okButton;
        public Button cancelButton;

        [Header("Options")]
        public int maxDigits = 11;
        public bool maskDigits = false;        // * ����ŷ ǥ��

        public event Action<string> OnSubmitted;
        public event Action OnCanceled;

        bool _visible;
        string _raw = "";

        void Awake()
        {
            if (input)
            {
                input.contentType = TMP_InputField.ContentType.Standard; // ���� ���͸�
                input.onValueChanged.AddListener(OnChanged);
            }
            if (okButton) okButton.onClick.AddListener(Submit);
            if (cancelButton) cancelButton.onClick.AddListener(Cancel);

            SetVisible(false, instant: true);
        }

        public void Show(string title, int maxLen = 11, bool mask = true, string preset = "")
        {
            titleText?.SetText(string.IsNullOrEmpty(title) ? "��ȣ �Է�" : title);
            maxDigits = Mathf.Max(1, maxLen);
            maskDigits = mask;
            _raw = DigitsOnly(preset);
            input.text = Display(_raw);
            SetVisible(true);
        }

        public void Hide() => SetVisible(false);

        void OnChanged(string _)
        {
            // ���ڸ� ���� + ���� ����
            _raw = DigitsOnly(input.text);
            if (_raw.Length > maxDigits) _raw = _raw.Substring(0, maxDigits);
            input.SetTextWithoutNotify(Display(_raw));
            okButton.interactable = _raw.Length >= 8; // ���� 8�ڸ� �̻��� �� Ȱ��ȭ
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
