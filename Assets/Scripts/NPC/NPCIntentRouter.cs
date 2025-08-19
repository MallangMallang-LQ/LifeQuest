using System.Text.RegularExpressions;
using LifeQuest.NPCFlow.Data;
using LifeQuest.NPCFlow.NPC;

public class NPCIntentRouter
{
    // 숫자(전화 등) 추출: 10~11자리만 간단히
    private static readonly Regex PhoneRx = new(@"\d{10,11}", RegexOptions.Compiled);

    public void Apply(string text, OrderState s, NPCDialogueStateMachine fsm, DialogueStepDefs.Step step)
    {
        var t = Normalize(text);

        // 공통 의사표현
        if (IsYes(t)) fsm.MarkConfirmed(); // Confirm 단계에서만 의미 있지만, 누적해도 무해
        // “아니요”는 Confirm 흐름에선 다시 주문 수정을 유도(여기서는 패스)

        // --- 단계별 룰 (필요한 것만 채움) ---
        switch (step)
        {
            case DialogueStepDefs.Step.TakeOrder:
                // 메뉴
                if (t.Contains("아메리카노") || t.Contains("아아") || t.Contains("뜨아"))
                    s.menuItem = OrderModels.MenuItem.Americano;

                // 온도
                if (t.Contains("아이스") || t.Contains("차갑") || t.Contains("시원"))
                    s.tempOption = OrderModels.TempOption.Ice;
                else if (t.Contains("뜨거") || t.Contains("핫") || t.Contains("따뜻"))
                    s.tempOption = OrderModels.TempOption.Hot;
                break;

            case DialogueStepDefs.Step.DineOption:
                if (t.Contains("매장") || t.Contains("먹고가"))
                    s.dineOption = OrderModels.DineOption.DineIn;
                else if (t.Contains("포장") || t.Contains("테이크아웃") || t.Contains("가져갈"))
                    s.dineOption = OrderModels.DineOption.TakeOut;
                break;

            case DialogueStepDefs.Step.Points:
                if (t.Contains("적립") || t.Contains("포인트"))
                {
                    if (IsNo(t)) s.pointsOption = OrderModels.PointsOption.No;
                    else if (IsYes(t)) s.pointsOption = OrderModels.PointsOption.Yes;

                    var m = PhoneRx.Match(t);
                    if (m.Success) s.pointsPhoneNumber = m.Value;
                }
                break;

            case DialogueStepDefs.Step.Coupon:
                if (t.Contains("쿠폰") || t.Contains("바코드"))
                {
                    if (IsNo(t)) s.couponOption = OrderModels.CouponOption.No;
                    else if (IsYes(t)) s.couponOption = OrderModels.CouponOption.Yes;

                    // 실물 제시
                    if (t.Contains("보여") || t.Contains("제시")) s.couponPresented = true;

                    // 코드 추출(숫자 긴 것)
                    var m2 = PhoneRx.Match(t);
                    if (m2.Success) s.couponCode = m2.Value;
                }
                break;

            case DialogueStepDefs.Step.PaymentMethod:
                if (t.Contains("현금")) s.paymentMethod = OrderModels.PaymentMethod.Cash;
                else if (t.Contains("카드")) s.paymentMethod = OrderModels.PaymentMethod.Card;
                else if (t.Contains("페이") || t.Contains("간편결제") || t.Contains("삼성페이") || t.Contains("네이버페이") || t.Contains("카카오페이"))
                    s.paymentMethod = OrderModels.PaymentMethod.Card;
                break;

            case DialogueStepDefs.Step.CashReceipt:
                if (t.Contains("현금영수증") || t.Contains("영수증"))
                {
                    if (IsNo(t)) s.cashReceiptOption = OrderModels.CashReceiptOption.No;
                    else if (IsYes(t)) s.cashReceiptOption = OrderModels.CashReceiptOption.Yes;

                    var m3 = PhoneRx.Match(t);
                    if (m3.Success) s.cashReceiptPhoneNumber = m3.Value;
                }
                break;

            case DialogueStepDefs.Step.ReceiptHandOff:
                // 영수증 필요 여부
                if (t.Contains("영수증"))
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
        => t.Contains("네") || t.Contains("예") || t.Contains("맞") || t.Contains("응") || t.Contains("그래");

    private static bool IsNo(string t)
        => t.Contains("아니") || t.Contains("노") || t.Contains("안해") || t.Contains("필요없");
}