#define LIFEQUEST_AI // OpenAI 연동 후 활성화시키기

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
/// 대화 루프 총괄:
/// - (템플릿/LLM) 프롬프트 생성 및 출력
/// - STT 결과 -> IntentRouter 적용 -> FSM.TryAdvance()
/// - 일부 단계 자동 진행(결제/전달/작별) + 무응답 리프롬프트
/// </summary>
public class NPCDialogueOrchestrator : MonoBehaviour
{
    // ==== Refs (Installer/Inspector에서 1회 주입) ====
    public NpcViewDriver view;
    public SpeechToTextService stt;
    public TextToSpeechService tts;

#if LIFEQUEST_AI
    [Header("AI (옵션)")]
    public bool useLLMStyle = false;
    public OpenAISettings openAISettings;   // SO 에셋
    private OpenAIClient _ai;
#endif

    // ==== FSM/상태/라우터 ====
    public NPCDialogueStateMachine fsm = new NPCDialogueStateMachine();
    public OrderState order = new OrderState();
    private NPCIntentRouter router = new NPCIntentRouter();

    // ==== 옵션 ====
    [Header("Start")]
    public bool autoStart = false; // GameFlow가 시작 주도

    [Header("Mock Auto Progress (sec)")]
    public float paymentMockDelay = 0.8f;
    public float handoffMockDelay = 0.6f;
    public float farewellMockDelay = 0.5f;

    [Header("UI Options")]
    public bool mirrorTranscriptToHUD = true;       // STT 결과 HUD 미러링
    public bool clearPlayerHUDOnStepChange = false; // 단계 전이 시 HUD 비우기

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
        // 부족 레퍼런스 보조(있으면 유지)
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
        Say(DialogueStepDefs.FirstLine);   // 고정 인사
        ShowPromptForCurrentStep();        // 첫 질문
    }

#if UNITY_EDITOR
    // 테스트용
    public void FeedTranscriptForTesting(string text) => HandleTranscript(text);
#endif

    // ==== Core Loop ====
    private void HandleTranscript(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        // 1) 플레이어 HUD 미러링
        if (mirrorTranscriptToHUD && text != _lastPlayerHUD)
        {
            view?.SetPlayerText(text);
            _lastPlayerHUD = text;
        }

        // 2) 라우팅
        router.Apply(text, order, fsm, fsm.CurrentStep);

        // 3) 전이 시도
        var prev = fsm.CurrentStep;
        fsm.TryAdvance();

        // 4) 전이 처리
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
            // 같은 단계->  재질문
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
                view?.SetNpcText("퀘스트를 성공하셨습니다. 축하드립니다!");
                // GameFlow에 완료 신호(있으면)
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
        // 템플릿 출력
        Say(fallback);
        StartReprompt();
    }

    private void Say(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        view?.SetNpcText(text);
        if (tts) StartCoroutine(tts.Speak(text)); // 모의/실제 TTS
        // (tts가 없으면 텍스트만 표시)
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
        ShowPromptForCurrentStep(); // 동일 단계 재질문
    }

    // ==== Utils ====
    private IEnumerator After(float sec, System.Action act)
    {
        yield return new WaitForSeconds(sec);
        act?.Invoke();
    }
    private void LogStep() => Debug.Log($"[FSM] → {fsm.CurrentStep}");
}