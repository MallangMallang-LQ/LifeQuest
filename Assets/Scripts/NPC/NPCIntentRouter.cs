using System.Text.RegularExpressions;
using LifeQuest.NPCFlow.Data;
using LifeQuest.NPCFlow.NPC;

public class NPCIntentRouter
{
    // ����(��ȭ ��) ����: 10~11�ڸ��� ������
    private static readonly Regex PhoneRx = new(@"\d{10,11}", RegexOptions.Compiled);

    public void Apply(string text, OrderState s, NPCDialogueStateMachine fsm, DialogueStepDefs.Step step)
    {
        var t = Normalize(text);

        // ���� �ǻ�ǥ��
        if (IsYes(t)) fsm.MarkConfirmed(); // Confirm �ܰ迡���� �ǹ� ������, �����ص� ����
        // ���ƴϿ䡱�� Confirm �帧���� �ٽ� �ֹ� ������ ����(���⼭�� �н�)

        // --- �ܰ躰 �� (�ʿ��� �͸� ä��) ---
        switch (step)
        {
            case DialogueStepDefs.Step.TakeOrder:
                // �޴�
                if (t.Contains("�Ƹ޸�ī��") || t.Contains("�ƾ�") || t.Contains("�߾�"))
                    s.menuItem = OrderModels.MenuItem.Americano;

                // �µ�
                if (t.Contains("���̽�") || t.Contains("����") || t.Contains("�ÿ�"))
                    s.tempOption = OrderModels.TempOption.Ice;
                else if (t.Contains("�߰�") || t.Contains("��") || t.Contains("����"))
                    s.tempOption = OrderModels.TempOption.Hot;
                break;

            case DialogueStepDefs.Step.DineOption:
                if (t.Contains("����") || t.Contains("�԰�"))
                    s.dineOption = OrderModels.DineOption.DineIn;
                else if (t.Contains("����") || t.Contains("����ũ�ƿ�") || t.Contains("������"))
                    s.dineOption = OrderModels.DineOption.TakeOut;
                break;

            case DialogueStepDefs.Step.Points:
                if (t.Contains("����") || t.Contains("����Ʈ"))
                {
                    if (IsNo(t)) s.pointsOption = OrderModels.PointsOption.No;
                    else if (IsYes(t)) s.pointsOption = OrderModels.PointsOption.Yes;

                    var m = PhoneRx.Match(t);
                    if (m.Success) s.pointsPhoneNumber = m.Value;
                }
                break;

            case DialogueStepDefs.Step.Coupon:
                if (t.Contains("����") || t.Contains("���ڵ�"))
                {
                    if (IsNo(t)) s.couponOption = OrderModels.CouponOption.No;
                    else if (IsYes(t)) s.couponOption = OrderModels.CouponOption.Yes;

                    // �ǹ� ����
                    if (t.Contains("����") || t.Contains("����")) s.couponPresented = true;

                    // �ڵ� ����(���� �� ��)
                    var m2 = PhoneRx.Match(t);
                    if (m2.Success) s.couponCode = m2.Value;
                }
                break;

            case DialogueStepDefs.Step.PaymentMethod:
                if (t.Contains("����")) s.paymentMethod = OrderModels.PaymentMethod.Cash;
                else if (t.Contains("ī��")) s.paymentMethod = OrderModels.PaymentMethod.Card;
                else if (t.Contains("����") || t.Contains("�������") || t.Contains("�Ｚ����") || t.Contains("���̹�����") || t.Contains("īī������"))
                    s.paymentMethod = OrderModels.PaymentMethod.Card;
                break;

            case DialogueStepDefs.Step.CashReceipt:
                if (t.Contains("���ݿ�����") || t.Contains("������"))
                {
                    if (IsNo(t)) s.cashReceiptOption = OrderModels.CashReceiptOption.No;
                    else if (IsYes(t)) s.cashReceiptOption = OrderModels.CashReceiptOption.Yes;

                    var m3 = PhoneRx.Match(t);
                    if (m3.Success) s.cashReceiptPhoneNumber = m3.Value;
                }
                break;

            case DialogueStepDefs.Step.ReceiptHandOff:
                // ������ �ʿ� ����
                if (t.Contains("������"))
                {
                    if (IsNo(t)) s.receiptNeeded = false;
                    else if (IsYes(t)) s.receiptNeeded = true;
                }
                break;
        }
    }

    private static string Normalize(string src)
    {
        if (string.IsNullOrEmpty(src)) return string.Empty;
        var t = src.Trim();
        t = t.Replace(" ", "");
        t = t.Replace("-", "");
        return t;
    }

    private static bool IsYes(string t)
        => t.Contains("��") || t.Contains("��") || t.Contains("��") || t.Contains("��") || t.Contains("�׷�");

    private static bool IsNo(string t)
        => t.Contains("�ƴ�") || t.Contains("��") || t.Contains("����") || t.Contains("�ʿ��");
}