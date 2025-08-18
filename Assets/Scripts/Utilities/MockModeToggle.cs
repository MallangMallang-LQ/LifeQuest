using UnityEngine;
using UnityEngine.UI;

public class MockModeToggle : MonoBehaviour
{
    public Toggle toggle;
    public SpeechToTextService stt;
    public TextToSpeechService tts;

    const string KEY = "LifeQuest.MockMode";

    void Awake()
    {
        bool on = PlayerPrefs.GetInt(KEY, 1) == 1; // ±âº» true
        if (toggle) { toggle.isOn = on; toggle.onValueChanged.AddListener(SetMock); }
        Apply(on);
    }

    void Apply(bool on)
    {
        if (stt) stt.useMock = on;
        if (tts) tts.useMock = on;
        PlayerPrefs.SetInt(KEY, on ? 1 : 0);
    }

    public void SetMock(bool on) => Apply(on);
}
