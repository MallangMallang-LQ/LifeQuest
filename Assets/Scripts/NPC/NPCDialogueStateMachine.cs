using System;
using System.Collections.Generic;
using LifeQuest.NPCFlow.Data;

namespace LifeQuest.NPCFlow.NPC
{
    /// <summary>
    /// ��� ��� FSM: �ܰ� ���̸� ��� (��� ����/ASR/�ִϴ� �ܺο���)
    /// - ���� ����: DialogueStepDefs.GetMissingSlots ���
    /// - �ൿ ����(Ȯ��/����/����/�ۺ�): ���� �÷��׷� ����
    /// </summary>
    public class NPCDialogueStateMachine
    {
        public DialogueStepDefs.Step CurrentStep { get; private set; } = DialogueStepDefs.Step.Greeting;
        public OrderState State { get; private set; }

        // ---- ����hemeral(�ൿ) �÷��� ----
        public bool Confirmed { get; private set; }        // ����, �¾ƿ䡱
        public bool PaymentApproved { get; private set; }  // ���� ���� �Ϸ�
        public bool ReceiptHanded { get; private set; }    // ������/��ȣǥ ���� ���� �Ϸ�
        public bool FarewellSaid { get; private set; }     // ������ ��Ʈ �Ϸ�

        // �̺�Ʈ: �ܰ� ����/�Ϸ�/����
        public event Action<DialogueStepDefs.Step> OnStepEntered;
        public event Action OnQuestSucceeded;
        public event Action<string> OnQuestFailed; // ���� ���� ����

        /// <summary>FSM ����(���� ���� �� �ʱ�ȭ)</summary>
        public void Begin(OrderState orderState)
        {
            State = orderState ?? throw new ArgumentNullException(nameof(orderState));
            CurrentStep = DialogueStepDefs.Step.Greeting;
            ResetEphemeral();
            OnStepEntered?.Invoke(CurrentStep);
        }

        /// <summary>
        /// ���� ���� �ø��� ȣ��: ���� �ܰ谡 �����Ǹ� �ڵ� ����
        /// (�ʼ� ����/���Ǻ� ���� ����� DialogueStepDefs�� ���)
        /// </summary>
        public void TryAdvance()
        {
            if (State == null) return;

            // 1) ���� �ܰ� �ʼ�(+��Ȳ��) ���� üũ
            List<string> missing = DialogueStepDefs.GetMissingSlots(CurrentStep, State);
            if (missing != null && missing.Count > 0)
                return; // ���� �� �ܰ迡�� �� ����� ��

            // 2) ���� �ܰ� ���(�ൿ ���� ����)
            var next = GetNextStep(CurrentStep, State);

            // 3) Success ���� �� ����Ʈ ���� ���� �˻�(��: ���̽� �Ƹ޸�ī�� 1��)
            if (next == DialogueStepDefs.Step.Success)
            {
                if (!IsQuestMenuCorrect(State))
                {
                    OnQuestFailed?.Invoke("Quest ���� ����ġ: ���̽� �Ƹ޸�ī�� 1�� �ֹ��� �ƴմϴ�.");
                    // UX: Ȯ�� �ܰ�� �ǵ��� ���ֹ� ����
                    CurrentStep = DialogueStepDefs.Step.ConfirmOrder;
                    OnStepEntered?.Invoke(CurrentStep);
                    return;
                }
            }

            // 4) ���� ���� �� �̺�Ʈ
            CurrentStep = next;
            OnStepEntered?.Invoke(CurrentStep);

            if (CurrentStep == DialogueStepDefs.Step.Success)
                OnQuestSucceeded?.Invoke();
        }

        /// <summary>�����/���� ������ ���� �ܰ� �̵�</summary>
        public void ForceStep(DialogueStepDefs.Step step)
        {
            CurrentStep = step;
            OnStepEntered?.Invoke(CurrentStep);
        }

        // ---- �ܺ�(Orchestrator ��)���� �ൿ �Ϸ� �� ȣ�� ----
        public void MarkConfirmed() => Confirmed = true;
        public void MarkPaymentApproved() => PaymentApproved = true;
        public void MarkReceiptHanded() => ReceiptHanded = true;
        public void MarkFarewellSaid() => FarewellSaid = true;

        // ---- ����: ���� �ܰ� ����(�ൿ ���� ����) ----
        private DialogueStepDefs.Step GetNextStep(DialogueStepDefs.Step current, OrderState s)
        {
            switch (current)
            {
                case DialogueStepDefs.Step.Greeting:
                    return DialogueStepDefs.Step.TakeOrder;

                case DialogueStepDefs.Step.TakeOrder:
                    // ���� ����� TryAdvance ��ܿ��� �̹� �����(�޴�+�µ�)
                    return DialogueStepDefs.Step.ConfirmOrder;

                case DialogueStepDefs.Step.ConfirmOrder:
                    // ����� Ȯ�� �ʿ�
                    return Confirmed ? DialogueStepDefs.Step.DineOption
                                     : DialogueStepDefs.Step.ConfirmOrder;

                case DialogueStepDefs.Step.DineOption:
                    return DialogueStepDefs.Step.Points;

                case DialogueStepDefs.Step.Points:
                    return DialogueStepDefs.Step.Coupon;

                case DialogueStepDefs.Step.Coupon:
                    return DialogueStepDefs.Step.PaymentMethod;

                case DialogueStepDefs.Step.PaymentMethod:
                    // �����̸� CashReceipt, �ƴϸ� ���� ����
                    return s.paymentMethod == OrderModels.PaymentMethod.Cash
                        ? DialogueStepDefs.Step.CashReceipt
                        : DialogueStepDefs.Step.PaymentProcessing;

                case DialogueStepDefs.Step.CashReceipt:
                    // ��ȣ �ʿ� ���� �� ���Ǻ� ������ TryAdvance���� �̹� ������
                    return DialogueStepDefs.Step.PaymentProcessing;

                case DialogueStepDefs.Step.PaymentProcessing:
                    // ���� ���� �Ϸ� ��ȣ �ʿ�
                    return PaymentApproved ? DialogueStepDefs.Step.ReceiptHandOff
                                           : DialogueStepDefs.Step.PaymentProcessing;

                case DialogueStepDefs.Step.ReceiptHandOff:
                    // StepDefs�� receiptNeeded ������ ������ + ���� ���� �Ϸ� �ʿ�
                    return ReceiptHanded ? DialogueStepDefs.Step.Farewell
                                         : DialogueStepDefs.Step.ReceiptHandOff;

                case DialogueStepDefs.Step.Farewell:
                    // ������ ��Ʈ TTS �Ϸ� ��
                    return FarewellSaid ? DialogueStepDefs.Step.Success
                                        : DialogueStepDefs.Step.Farewell;

                case DialogueStepDefs.Step.Success:
                default:
                    return DialogueStepDefs.Step.Success;
            }
        }

        /// <summary>
        /// MVP ����Ʈ ����: "���̽� �Ƹ޸�ī�� 1��"���� Ȯ��
        /// - ���� ������ ���ٸ�, �޴�/�µ��� üũ(����=1 ����)
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
