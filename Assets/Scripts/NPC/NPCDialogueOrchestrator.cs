#define LIFEQUEST_AI // OpenAI ���� �� Ȱ��ȭ��Ű��

using System.Collections;
using UnityEngine;
using LifeQuest.NPCFlow.Data;     // OrderState, DialogueStepDefs
using LifeQuest.NPCFlow.NPC;      // NPCDialogueStateMachine, NPCIntentRouter
using LifeQuest.NPCFlow.UI;       // NpcViewDriver

#if LIFEQUEST_AI
using LifeQuest.Integrations;     // OpenAISettings
using LifeQuest.AI;               // OpenAIClient, PromptBuilder, SafetyGuard
#endif

/// <summary>
/// ��ȭ ���� �Ѱ�:
/// - (���ø�/LLM) ������Ʈ ���� �� ���
/// - STT ��� -> IntentRouter ���� -> FSM.TryAdvance()
/// - �Ϻ� �ܰ� �ڵ� ����(����/����/�ۺ�) + ������ ��������Ʈ
/// </summary>
public class NPCDialogueOrchestrator : MonoBehaviour
{
    // ==== Refs (Installer/Inspector���� 1ȸ ����) ====
    public NpcViewDriver view;
    public SpeechToTextService stt;
    public TextToSpeechService tts;

#if LIFEQUEST_AI
    [Header("AI (�ɼ�)")]
    public bool useLLMStyle = false;
    public OpenAISettings openAISettings;   // SO ����
    private OpenAIClient _ai;
#endif

    // ==== FSM/����/����� ====
    public NPCDialogueStateMachine fsm = new NPCDialogueStateMachine();
    public OrderState order = new OrderState();
    private NPCIntentRouter router = new NPCIntentRouter();

    // ==== �ɼ� ====
    [Header("Start")]
    public bool autoStart = false; // GameFlow�� ���� �ֵ�

    [Header("Mock Auto Progress (sec)")]
    public float paymentMockDelay = 0.8f;
    public float handoffMockDelay = 0.6f;
    public float farewellMockDelay = 0.5f;

    [Header("UI Options")]
    public bool mirrorTranscriptToHUD = true;       // STT ��� HUD �̷���
    public bool clearPlayerHUDOnStepChange = false; // �ܰ� ���� �� HUD ����

    [Header("Reprompt")]
    public bool useReprompt = true;
    [Range(1f, 15f)] public float repromptSec = 6f;
    private Coroutine _repromptCo;
    private string _lastPlayerHUD;

    // ==== Unity Hooks ====
    void OnEnable()
    {
        if (stt) stt.OnTranscriptReady += HandleTranscript;
    }
    void OnDisable()
    {
        if (stt) stt.OnTranscriptReady -= HandleTranscript;
        StopReprompt();
    }
    void Start()
    {
        // ���� ���۷��� ����(������ ����)
        view ??= FindObjectOfType<NpcViewDriver>(includeInactive: true);
        stt ??= FindObjectOfType<SpeechToTextService>(includeInactive: true);
        tts ??= FindObjectOfType<TextToSpeechService>(includeInactive: true);

#if LIFEQUEST_AI
        if (useLLMStyle && openAISettings) _ai = new OpenAIClient(openAISettings);
#endif
        if (autoStart) StartDialogue();
    }

    // ==== Public ====
    public void StartDialogue()
    {
        fsm.Begin(order);
        LogStep();
        Say(DialogueStepDefs.FirstLine);   // ���� �λ�
        ShowPromptForCurrentStep();        // ù ����
    }

#if UNITY_EDITOR
    // �׽�Ʈ��
    public void FeedTranscriptForTesting(string text) => HandleTranscript(text);
#endif

    // ==== Core Loop ====
    private void HandleTranscript(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        // 1) �÷��̾� HUD �̷���
        if (mirrorTranscriptToHUD && text != _lastPlayerHUD)
        {
            view?.SetPlayerText(text);
            _lastPlayerHUD = text;
        }

        // 2) �����
        router.Apply(text, order, fsm, fsm.CurrentStep);

        // 3) ���� �õ�
        var prev = fsm.CurrentStep;
        fsm.TryAdvance();

        // 4) ���� ó��
        if (fsm.CurrentStep != prev)
        {
            if (clearPlayerHUDOnStepChange)
            {
                _lastPlayerHUD = null;
                view?.SetPlayerText(string.Empty);
            }
            OnStepEntered(fsm.CurrentStep);
        }
        else
        {
            // ���� �ܰ�->  ������
            ShowPromptForCurrentStep();
        }
    }

    private void OnStepEntered(DialogueStepDefs.Step step)
    {
        StopReprompt();

        switch (step)
        {
            case DialogueStepDefs.Step.PaymentProcessing:
                ShowPromptForCurrentStep();
                StartCoroutine(After(paymentMockDelay, () => {
                    fsm.MarkPaymentApproved();
                    fsm.TryAdvance();
                    LogStep();
                    OnStepEntered(fsm.CurrentStep);
                }));
                break;

            case DialogueStepDefs.Step.ReceiptHandOff:
                ShowPromptForCurrentStep();
                StartCoroutine(After(handoffMockDelay, () => {
                    fsm.MarkReceiptHanded();
                    fsm.TryAdvance();
                    LogStep();
                    OnStepEntered(fsm.CurrentStep);
                }));
                break;

            case DialogueStepDefs.Step.Farewell:
                Say(DialogueStepDefs.LastLine);
                StartCoroutine(After(farewellMockDelay, () => {
                    fsm.MarkFarewellSaid();
                    fsm.TryAdvance();
                    LogStep();
                    OnStepEntered(fsm.CurrentStep);
                }));
                break;

            case DialogueStepDefs.Step.Success:
                view?.SetNpcText("����Ʈ�� �����ϼ̽��ϴ�. ���ϵ帳�ϴ�!");
                // GameFlow�� �Ϸ� ��ȣ(������)
                var flow = FindObjectOfType<LifeQuest.NPCFlow.Core.GameFlowController>();
                flow?.CompleteQuest();
                break;

            default:
                ShowPromptForCurrentStep();
                break;
        }
    }

    // ==== Prompt/Output ====
    private void ShowPromptForCurrentStep()
    {
        var step = fsm.CurrentStep;
        var fallback = DialogueStepDefs.GetUserPromptTemplate(step, order);

#if LIFEQUEST_AI
        if (useLLMStyle && _ai != null)
        {
            var (sys, usr) = PromptBuilder.Build(step, order);
            StartCoroutine(SafetyGuard.GenerateWithPolicy(
                client: _ai,
                system: sys, user: usr, fallback: fallback,
                onText: text => { Say(text); StartReprompt(); },
                modelMaxOutTokens: openAISettings ? openAISettings.maxTokens : 120,
                timeoutSec: openAISettings ? openAISettings.timeoutSec : 15
            ));
            return;
        }
#endif
        // ���ø� ���
        Say(fallback);
        StartReprompt();
    }

    private void Say(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        view?.SetNpcText(text);
        if (tts) StartCoroutine(tts.Speak(text)); // ����/���� TTS
        // (tts�� ������ �ؽ�Ʈ�� ǥ��)
    }

    // ==== Reprompt ====
    private void StartReprompt()
    {
        if (!useReprompt) return;
        StopReprompt();
        _repromptCo = StartCoroutine(RepromptAfter(repromptSec));
    }
    private void StopReprompt()
    {
        if (_repromptCo != null) { StopCoroutine(_repromptCo); _repromptCo = null; }
    }
    private IEnumerator RepromptAfter(float sec)
    {
        yield return new WaitForSeconds(sec);
        ShowPromptForCurrentStep(); // ���� �ܰ� ������
    }

    // ==== Utils ====
    private IEnumerator After(float sec, System.Action act)
    {
        yield return new WaitForSeconds(sec);
        act?.Invoke();
    }
    private void LogStep() => Debug.Log($"[FSM] �� {fsm.CurrentStep}");
}