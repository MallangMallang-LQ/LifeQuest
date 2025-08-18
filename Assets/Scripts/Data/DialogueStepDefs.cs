using System.Collections.Generic;
using System.Text;
// UnityEngine �� �ʿ� �ø� ��� (��: Debug.Log)
// using UnityEngine;

namespace LifeQuest.NPCFlow.Data
{
    /// <summary>
    /// ��� ��ο� ���� ��Ʈ/�ܰ� ����/������Ʈ ���ø� ����
    /// </summary>
    public static class DialogueStepDefs
    {
        // ���� ��Ʈ
        public const string FirstLine = "�������. �ֹ� ���͵帮�ڽ��ϴ�.";
        public const string LastLine = "�ֹ� �Ϸ�Ǿ����ϴ�. ���ʿ��� ������ֽø� �޴� �غ��ؼ� ��ȣ �ҷ��帮�ڽ��ϴ�.";

        // �ܰ� enum (FSM�� ����)
        public enum Step
        {
            Greeting = 0,
            TakeOrder,         // �޴� + Hot/Ice
            ConfirmOrder,      // �ֹ� ��Ȯ��
            DineOption,        // ����/����ũ�ƿ�
            Points,            // ����Ʈ ����
            Coupon,            // ���� ���
            PaymentMethod,     // ī��/����
            CashReceipt,       // ���� �ÿ���
            PaymentProcessing, // ���� ���� �ȳ�
            ReceiptHandOff,    // ������/��ȣǥ ����
            Farewell,          // ������ ��Ʈ
            Success            // ����Ʈ �Ϸ�
        }

        // �� �ܰ迡�� �ʼ��� ä���� �������� �Ѿ ���Ե�
        // (�ʿ�� FSM���� ��Ȳ�� �������� �� �߰� üũ)
        private static readonly Dictionary<Step, string[]> RequiredSlots = new()
        {
            { Step.TakeOrder,         new[] { nameof(OrderState.menuItem), nameof(OrderState.tempOption) } },
            { Step.ConfirmOrder,      new[] { nameof(OrderState.menuItem), nameof(OrderState.tempOption) } },
            { Step.DineOption,        new[] { nameof(OrderState.dineOption) } },

            // ����Ʈ/���� �и�
            { Step.Points,            new[] { nameof(OrderState.pointsOption) } },  // Yes�� ��ȣ �߰� üũ
            { Step.Coupon,            new[] { nameof(OrderState.couponOption) } },  // Yes�� ����/�ڵ� �߰� üũ

            { Step.PaymentMethod,     new[] { nameof(OrderState.paymentMethod) } },
            { Step.CashReceipt,       new[] { nameof(OrderState.cashReceiptOption) } }, // ������ ���� �ܰ� ����
            { Step.PaymentProcessing, new[] { nameof(OrderState.paymentMethod) } },
            { Step.ReceiptHandOff,    new[] { nameof(OrderState.receiptNeeded) } },
            { Step.Farewell,          new string[0] },
            { Step.Success,           new string[0] }
        };

        /// <summary>
        /// LLM���� ������ "����/��Ģ" ��� ���� ������Ʈ (PromptBuilder���� system���� ��� ����)
        /// </summary>
        public static string BuildSystemPreamble()
        {
            return
@"�ʴ� ī�� �����̴�. �Ʒ� ������ �ݵ�� ��Ű�� �� ���� �� ������ ª�� �Ƿ��ϰ� ���Ѵ�.
����: �ֹ� �ޱ�(Hot/Ice ����) �� �ֹ� ��Ȯ�� �� ����/����ũ�ƿ� �� �հ� �ݾ� �ȳ� �� ����Ʈ �� ���� �� �������� �� (���� ��) ���ݿ����� �� ������ �ʿ� ���� �� ���� �Ϸ� ��Ʈ.
������ ������� �� �׸� ������ �������Ѵ�. ����ڰ� �߰� ������ ��û�ϸ� �ش� ������ �����ϰ� �ٽ� ª�� ��Ȯ���Ѵ�.
ù ��Ʈ�� ������ ��Ʈ�� ���� ������ ����Ѵ�.";
        }

        /// <summary>
        /// �ܰ躰 "����ڿ��� ���� ���� ���ø�" (LLM user/content ��Ʈ�� ���)
        /// </summary>
        public static string GetUserPromptTemplate(Step step, OrderState state)
        {
            switch (step)
            {
                case Step.Greeting:
                    return FirstLine;

                case Step.TakeOrder:
                    return "�ֹ� ���͵帮�ڽ��ϴ�. � ����� �غ��ص帱���? (��: �Ƹ޸�ī��/��) �߰ſ� �Ͱ� ���̽� �߿� �������� �Ͻ����� �˷��ּ���.";

                case Step.ConfirmOrder:
                    return BuildConfirmText(state);

                case Step.DineOption:
                    return "���忡�� ��ó���, ����ũ�ƿ����� �������ó���?";

                case Step.Points:
                    if (state.pointsOption == OrderModels.PointsOption.None)
                        return "����Ʈ �����Ͻðھ��?";
                    if (state.pointsOption == OrderModels.PointsOption.Yes && string.IsNullOrEmpty(state.pointsPhoneNumber))
                        return "����Ʈ ���� ��ȣ�� �˷��ּ���.";
                    return "����Ʈ ���� Ȯ���߽��ϴ�.";

                case Step.Coupon:
                    if (state.couponOption == OrderModels.CouponOption.None)
                        return "���� ����Ͻðھ��?";
                    if (state.couponOption == OrderModels.CouponOption.Yes && !state.couponPresented && string.IsNullOrEmpty(state.couponCode))
                        return "������ �����ּ���. (�ǹ� �Ǵ� �޴��� ���ڵ�)";
                    return "���� Ȯ���߽��ϴ�.";

                case Step.PaymentMethod:
                    return "������ ī��� �Ͻðھ��, �������� �Ͻðھ��?";

                case Step.CashReceipt:
                    // PaymentMethod�� Cash�� ���� �ܰ� ����
                    if (state.cashReceiptOption == OrderModels.CashReceiptOption.None)
                        return "���ݿ����� �Ͻðھ��?";
                    if (state.cashReceiptOption == OrderModels.CashReceiptOption.Yes &&
                        string.IsNullOrEmpty(state.cashReceiptPhoneNumber))
                        return "���ݿ����� ��ȣ�� �˷��ּ���.";
                    return "���ݿ����� ó�� �Ϸ�Ǿ����ϴ�.";

                case Step.PaymentProcessing:
                    return "���� �����ϰڽ��ϴ�. ī�� �ܸ��⿡ ī�带 �Ⱦ��ּ���. (������ ��� �ݾ��� �غ����ּ���.)";

                case Step.ReceiptHandOff:
                    if (state.receiptNeeded == null)
                        return "������ �ʿ��ϽŰ���?";
                    return state.receiptNeeded == true
                        ? "�������� �ֹ���ȣǥ �帱�Կ�."
                        : "�ֹ���ȣǥ�� �帮�ڽ��ϴ�.";

                case Step.Farewell:
                    return LastLine;

                case Step.Success:
                    return "����Ʈ �Ϸ� ó���մϴ�.";

                default:
                    return "";
            }
        }

        /// <summary>
        /// Confirm �ܰ迡�� �ֹ� ��Ȯ�� ���� ����
        /// </summary>
        private static string BuildConfirmText(OrderState s)
        {
            var menu = s.menuItem.ToString();
            var temp = s.tempOption == OrderModels.TempOption.Hot ? "�߰ſ�" :
                       s.tempOption == OrderModels.TempOption.Ice ? "���̽�" : "";
            var sb = new StringBuilder();

            // �޴�/�µ��� Required �̹Ƿ� �� �� �ִٰ� ����
            sb.Append($"�ֹ� Ȯ���ϰڽ��ϴ�. {temp} {menu}");

            // ����/���� ������ ������ �̸� ���
            if (s.dineOption == OrderModels.DineOption.DineIn) sb.Append(", ���� ���");
            else if (s.dineOption == OrderModels.DineOption.TakeOut) sb.Append(", ����ũ�ƿ�");

            sb.Append(" �����Ǳ��?");
            return sb.ToString();
        }

        /// <summary>
        /// Ư�� �ܰ迡�� �ʼ� ���� ��� ��ȯ
        /// </summary>
        public static IReadOnlyList<string> GetRequiredSlots(Step step) =>
            RequiredSlots.TryGetValue(step, out var arr) ? arr : System.Array.Empty<string>();

        /// <summary>
        /// ���� ���¿��� �ش� �ܰ��� �ʼ� ���� �� ������ �׸� ����Ʈ (��Ȳ�� ����)
        /// </summary>
        public static List<string> GetMissingSlots(Step step, OrderState state)
        {
            var missing = new List<string>();
            foreach (var slot in GetRequiredSlots(step))
            {
                if (!state.IsFilled(slot)) missing.Add(slot);
            }

            // === ��Ȳ�� ����(Conditional Slots) ===

            // ����Ʈ: Yes�� ��ȣ �ʿ�
            if (step == Step.Points && state.pointsOption == OrderModels.PointsOption.Yes)
            {
                if (string.IsNullOrEmpty(state.pointsPhoneNumber))
                    missing.Add(nameof(OrderState.pointsPhoneNumber));
            }

            // ����: Yes�� '����' �Ǵ� '�ڵ�' �� �ϳ��� �ʿ�
            if (step == Step.Coupon && state.couponOption == OrderModels.CouponOption.Yes)
            {
                if (!state.couponPresented && string.IsNullOrEmpty(state.couponCode))
                {
                    // �� �� �ϳ��� �����ص� �Ѿ �� �ְ� �⺻�� '����'�� �䱸
                    missing.Add(nameof(OrderState.couponPresented));
                    // �ʿ� �� ���� �ٷ� '�ڵ�'�� �䱸 ����:
                    // missing.Add(nameof(OrderState.couponCode));
                }
            }

            // ���ݿ�����: Yes�� ��ȣ �ʿ�
            if (step == Step.CashReceipt && state.paymentMethod == OrderModels.PaymentMethod.Cash)
            {
                if (state.cashReceiptOption == OrderModels.CashReceiptOption.Yes &&
                    string.IsNullOrEmpty(state.cashReceiptPhoneNumber))
                {
                    missing.Add(nameof(OrderState.cashReceiptPhoneNumber));
                }
            }

            // ������ �ʿ� ����
            if (step == Step.ReceiptHandOff && state.receiptNeeded == null)
            {
                missing.Add(nameof(OrderState.receiptNeeded));
            }

            return missing;
        }
    }
}
