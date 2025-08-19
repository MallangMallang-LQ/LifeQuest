using System;
using System.Collections.Generic;
using LifeQuest.NPCFlow.Data;

namespace LifeQuest.NPCFlow.NPC
{
    /// <summary>
    /// 대면 경로 FSM: 단계 전이만 담당 (대사 생성/ASR/애니는 외부에서)
    /// - 슬롯 가드: DialogueStepDefs.GetMissingSlots 사용
    /// - 행동 가드(확답/승인/전달/작별): 내부 플래그로 제어
    /// </summary>
    public class NPCDialogueStateMachine
    {
        public DialogueStepDefs.Step CurrentStep { get; private set; } = DialogueStepDefs.Step.Greeting;
        public OrderState State { get; private set; }

        // ---- 에피hemeral(행동) 플래그 ----
        public bool Confirmed { get; private set; }        // “네, 맞아요”
        public bool PaymentApproved { get; private set; }  // 결제 승인 완료
        public bool ReceiptHanded { get; private set; }    // 영수증/번호표 실제 전달 완료
        public bool FarewellSaid { get; private set; }     // 마지막 멘트 완료

        // 이벤트: 단계 변경/완료/실패
        public event Action<DialogueStepDefs.Step> OnStepEntered;
        public event Action OnQuestSucceeded;
        public event Action<string> OnQuestFailed; // 실패 사유 전달

        /// <summary>FSM 시작(상태 주입 및 초기화)</summary>
        public void Begin(OrderState orderState)
        {
            State = orderState ?? throw new ArgumentNullException(nameof(orderState));
            CurrentStep = DialogueStepDefs.Step.Greeting;
            ResetEphemeral();
            OnStepEntered?.Invoke(CurrentStep);
        }

        /// <summary>
        /// 슬롯 갱신 시마다 호출: 현재 단계가 충족되면 자동 전이
        /// (필수 슬롯/조건부 슬롯 가드는 DialogueStepDefs가 계산)
        /// </summary>
        public void TryAdvance()
        {
            if (State == null) return;

            // 1) 현재 단계 필수(+상황부) 슬롯 체크
            List<string> missing = DialogueStepDefs.GetMissingSlots(CurrentStep, State);
            if (missing != null && missing.Count > 0)
                return; // 아직 이 단계에서 더 물어야 함

            // 2) 다음 단계 계산(행동 가드 포함)
            var next = GetNextStep(CurrentStep, State);

            // 3) Success 진입 전 퀘스트 조건 최종 검사(예: 아이스 아메리카노 1잔)
            if (next == DialogueStepDefs.Step.Success)
            {
                if (!IsQuestMenuCorrect(State))
                {
                    OnQuestFailed?.Invoke("Quest 조건 불일치: 아이스 아메리카노 1잔 주문이 아닙니다.");
                    // UX: 확인 단계로 되돌려 재주문 유도
                    CurrentStep = DialogueStepDefs.Step.ConfirmOrder;
                    OnStepEntered?.Invoke(CurrentStep);
                    return;
                }
            }

            // 4) 전이 적용 및 이벤트
            CurrentStep = next;
            OnStepEntered?.Invoke(CurrentStep);

            if (CurrentStep == DialogueStepDefs.Step.Success)
                OnQuestSucceeded?.Invoke();
        }

        /// <summary>디버그/예외 복구용 강제 단계 이동</summary>
        public void ForceStep(DialogueStepDefs.Step step)
        {
            CurrentStep = step;
            OnStepEntered?.Invoke(CurrentStep);
        }

        // ---- 외부(Orchestrator 등)에서 행동 완료 시 호출 ----
        public void MarkConfirmed() => Confirmed = true;
        public void MarkPaymentApproved() => PaymentApproved = true;
        public void MarkReceiptHanded() => ReceiptHanded = true;
        public void MarkFarewellSaid() => FarewellSaid = true;

        // ---- 내부: 다음 단계 결정(행동 가드 포함) ----
        private DialogueStepDefs.Step GetNextStep(DialogueStepDefs.Step current, OrderState s)
        {
            switch (current)
            {
                case DialogueStepDefs.Step.Greeting:
                    return DialogueStepDefs.Step.TakeOrder;

                case DialogueStepDefs.Step.TakeOrder:
                    // 슬롯 가드는 TryAdvance 상단에서 이미 통과됨(메뉴+온도)
                    return DialogueStepDefs.Step.ConfirmOrder;

                case DialogueStepDefs.Step.ConfirmOrder:
                    // 사용자 확답 필요
                    return Confirmed ? DialogueStepDefs.Step.DineOption
                                     : DialogueStepDefs.Step.ConfirmOrder;

                case DialogueStepDefs.Step.DineOption:
                    return DialogueStepDefs.Step.Points;

                case DialogueStepDefs.Step.Points:
                    return DialogueStepDefs.Step.Coupon;

                case DialogueStepDefs.Step.Coupon:
                    return DialogueStepDefs.Step.PaymentMethod;

                case DialogueStepDefs.Step.PaymentMethod:
                    // 현금이면 CashReceipt, 아니면 결제 진행
                    return s.paymentMethod == OrderModels.PaymentMethod.Cash
                        ? DialogueStepDefs.Step.CashReceipt
                        : DialogueStepDefs.Step.PaymentProcessing;

                case DialogueStepDefs.Step.CashReceipt:
                    // 번호 필요 여부 등 조건부 슬롯은 TryAdvance에서 이미 검증됨
                    return DialogueStepDefs.Step.PaymentProcessing;

                case DialogueStepDefs.Step.PaymentProcessing:
                    // 결제 승인 완료 신호 필요
                    return PaymentApproved ? DialogueStepDefs.Step.ReceiptHandOff
                                           : DialogueStepDefs.Step.PaymentProcessing;

                case DialogueStepDefs.Step.ReceiptHandOff:
                    // StepDefs가 receiptNeeded 슬롯은 가드함 + 실제 전달 완료 필요
                    return ReceiptHanded ? DialogueStepDefs.Step.Farewell
                                         : DialogueStepDefs.Step.ReceiptHandOff;

                case DialogueStepDefs.Step.Farewell:
                    // 마지막 멘트 TTS 완료 등
                    return FarewellSaid ? DialogueStepDefs.Step.Success
                                        : DialogueStepDefs.Step.Farewell;

                case DialogueStepDefs.Step.Success:
                default:
                    return DialogueStepDefs.Step.Success;
            }
        }

        /// <summary>
        /// MVP 퀘스트 조건: "아이스 아메리카노 1잔"인지 확인
        /// - 수량 슬롯이 없다면, 메뉴/온도만 체크(수량=1 가정)
        /// </summary>
        private bool IsQuestMenuCorrect(OrderState s)
        {
            return s.menuItem == OrderModels.MenuItem.Americano
                   && s.tempOption == OrderModels.TempOption.Ice;
        }

        private void ResetEphemeral()
        {
            Confirmed = false;
            PaymentApproved = false;
            ReceiptHanded = false;
            FarewellSaid = false;
        }
    }
}
