using System.Collections.Generic;
using System.Text;
// UnityEngine 은 필요 시만 사용 (예: Debug.Log)
// using UnityEngine;

namespace LifeQuest.NPCFlow.Data
{
    /// <summary>
    /// 대면 경로용 고정 멘트/단계 정의/프롬프트 템플릿 모음
    /// </summary>
    public static class DialogueStepDefs
    {
        // 고정 멘트
        public const string FirstLine = "어서오세요. 주문 도와드리겠습니다.";
        public const string LastLine = "주문 완료되었습니다. 뒷쪽에서 대기해주시면 메뉴 준비해서 번호 불러드리겠습니다.";

        // 단계 enum (FSM과 공유)
        public enum Step
        {
            Greeting = 0,
            TakeOrder,         // 메뉴 + Hot/Ice
            ConfirmOrder,      // 주문 재확인
            DineOption,        // 매장/테이크아웃
            Points,            // 포인트 적립
            Coupon,            // 쿠폰 사용
            PaymentMethod,     // 카드/현금
            CashReceipt,       // 현금 시에만
            PaymentProcessing, // 결제 진행 안내
            ReceiptHandOff,    // 영수증/번호표 전달
            Farewell,          // 마지막 멘트
            Success            // 퀘스트 완료
        }

        // 각 단계에서 필수로 채워야 다음으로 넘어갈 슬롯들
        // (필요시 FSM에서 상황부 조건으로 더 추가 체크)
        private static readonly Dictionary<Step, string[]> RequiredSlots = new()
        {
            { Step.TakeOrder,         new[] { nameof(OrderState.menuItem), nameof(OrderState.tempOption) } },
            { Step.ConfirmOrder,      new[] { nameof(OrderState.menuItem), nameof(OrderState.tempOption) } },
            { Step.DineOption,        new[] { nameof(OrderState.dineOption) } },

            // 포인트/쿠폰 분리
            { Step.Points,            new[] { nameof(OrderState.pointsOption) } },  // Yes면 번호 추가 체크
            { Step.Coupon,            new[] { nameof(OrderState.couponOption) } },  // Yes면 제시/코드 추가 체크

            { Step.PaymentMethod,     new[] { nameof(OrderState.paymentMethod) } },
            { Step.CashReceipt,       new[] { nameof(OrderState.cashReceiptOption) } }, // 현금일 때만 단계 진입
            { Step.PaymentProcessing, new[] { nameof(OrderState.paymentMethod) } },
            { Step.ReceiptHandOff,    new[] { nameof(OrderState.receiptNeeded) } },
            { Step.Farewell,          new string[0] },
            { Step.Success,           new string[0] }
        };

        /// <summary>
        /// LLM에게 전달할 "역할/규칙" 상단 고정 프롬프트 (PromptBuilder에서 system으로 사용 권장)
        /// </summary>
        public static string BuildSystemPreamble()
        {
            return
@"너는 카페 직원이다. 아래 순서를 반드시 지키며 한 번에 한 가지씩 짧고 또렷하게 말한다.
순서: 주문 받기(Hot/Ice 포함) → 주문 재확인 → 매장/테이크아웃 → 합계 금액 안내 → 포인트 → 쿠폰 → 결제수단 → (현금 시) 현금영수증 → 영수증 필요 여부 → 결제 완료 멘트.
슬롯이 비었으면 그 항목만 정중히 재질문한다. 사용자가 중간 변경을 요청하면 해당 슬롯을 갱신하고 다시 짧게 재확인한다.
첫 멘트와 마지막 멘트는 고정 문구를 사용한다.";
        }

        /// <summary>
        /// 단계별 "사용자에게 물을 질문 템플릿" (LLM user/content 힌트로 사용)
        /// </summary>
        public static string GetUserPromptTemplate(Step step, OrderState state)
        {
            switch (step)
            {
                case Step.Greeting:
                    return FirstLine;

                case Step.TakeOrder:
                    return "주문 도와드리겠습니다. 어떤 음료로 준비해드릴까요? (예: 아메리카노/라떼) 뜨거운 것과 아이스 중에 무엇으로 하실지도 알려주세요.";

                case Step.ConfirmOrder:
                    return BuildConfirmText(state);

                case Step.DineOption:
                    return "매장에서 드시나요, 테이크아웃으로 가져가시나요?";

                case Step.Points:
                    if (state.pointsOption == OrderModels.PointsOption.None)
                        return "포인트 적립하시겠어요?";
                    if (state.pointsOption == OrderModels.PointsOption.Yes && string.IsNullOrEmpty(state.pointsPhoneNumber))
                        return "포인트 적립 번호를 알려주세요.";
                    return "포인트 적립 확인했습니다.";

                case Step.Coupon:
                    if (state.couponOption == OrderModels.CouponOption.None)
                        return "쿠폰 사용하시겠어요?";
                    if (state.couponOption == OrderModels.CouponOption.Yes && !state.couponPresented && string.IsNullOrEmpty(state.couponCode))
                        return "쿠폰을 보여주세요. (실물 또는 휴대폰 바코드)";
                    return "쿠폰 확인했습니다.";

                case Step.PaymentMethod:
                    return "결제는 카드로 하시겠어요, 현금으로 하시겠어요?";

                case Step.CashReceipt:
                    // PaymentMethod가 Cash일 때만 단계 진입
                    if (state.cashReceiptOption == OrderModels.CashReceiptOption.None)
                        return "현금영수증 하시겠어요?";
                    if (state.cashReceiptOption == OrderModels.CashReceiptOption.Yes &&
                        string.IsNullOrEmpty(state.cashReceiptPhoneNumber))
                        return "현금영수증 번호를 알려주세요.";
                    return "현금영수증 처리 완료되었습니다.";

                case Step.PaymentProcessing:
                    return "결제 진행하겠습니다. 카드 단말기에 카드를 꽂아주세요. (현금일 경우 금액을 준비해주세요.)";

                case Step.ReceiptHandOff:
                    if (state.receiptNeeded == null)
                        return "영수증 필요하신가요?";
                    return state.receiptNeeded == true
                        ? "영수증과 주문번호표 드릴게요."
                        : "주문번호표만 드리겠습니다.";

                case Step.Farewell:
                    return LastLine;

                case Step.Success:
                    return "퀘스트 완료 처리합니다.";

                default:
                    return "";
            }
        }

        /// <summary>
        /// Confirm 단계에서 주문 재확인 문구 생성
        /// </summary>
        private static string BuildConfirmText(OrderState s)
        {
            var menu = s.menuItem.ToString();
            var temp = s.tempOption == OrderModels.TempOption.Hot ? "뜨거운" :
                       s.tempOption == OrderModels.TempOption.Ice ? "아이스" : "";
            var sb = new StringBuilder();

            // 메뉴/온도는 Required 이므로 둘 다 있다고 가정
            sb.Append($"주문 확인하겠습니다. {temp} {menu}");

            // 매장/포장 정보가 있으면 미리 언급
            if (s.dineOption == OrderModels.DineOption.DineIn) sb.Append(", 매장 취식");
            else if (s.dineOption == OrderModels.DineOption.TakeOut) sb.Append(", 테이크아웃");

            sb.Append(" 맞으실까요?");
            return sb.ToString();
        }

        /// <summary>
        /// 특정 단계에서 필수 슬롯 목록 반환
        /// </summary>
        public static IReadOnlyList<string> GetRequiredSlots(Step step) =>
            RequiredSlots.TryGetValue(step, out var arr) ? arr : System.Array.Empty<string>();

        /// <summary>
        /// 현재 상태에서 해당 단계의 필수 슬롯 중 미충족 항목 리스트 (상황부 포함)
        /// </summary>
        public static List<string> GetMissingSlots(Step step, OrderState state)
        {
            var missing = new List<string>();
            foreach (var slot in GetRequiredSlots(step))
            {
                if (!state.IsFilled(slot)) missing.Add(slot);
            }

            // === 상황부 조건(Conditional Slots) ===

            // 포인트: Yes면 번호 필요
            if (step == Step.Points && state.pointsOption == OrderModels.PointsOption.Yes)
            {
                if (string.IsNullOrEmpty(state.pointsPhoneNumber))
                    missing.Add(nameof(OrderState.pointsPhoneNumber));
            }

            // 쿠폰: Yes면 '제시' 또는 '코드' 중 하나는 필요
            if (step == Step.Coupon && state.couponOption == OrderModels.CouponOption.Yes)
            {
                if (!state.couponPresented && string.IsNullOrEmpty(state.couponCode))
                {
                    // 둘 중 하나만 충족해도 넘어갈 수 있게 기본은 '제시'를 요구
                    missing.Add(nameof(OrderState.couponPresented));
                    // 필요 시 다음 줄로 '코드'도 요구 가능:
                    // missing.Add(nameof(OrderState.couponCode));
                }
            }

            // 현금영수증: Yes면 번호 필요
            if (step == Step.CashReceipt && state.paymentMethod == OrderModels.PaymentMethod.Cash)
            {
                if (state.cashReceiptOption == OrderModels.CashReceiptOption.Yes &&
                    string.IsNullOrEmpty(state.cashReceiptPhoneNumber))
                {
                    missing.Add(nameof(OrderState.cashReceiptPhoneNumber));
                }
            }

            // 영수증 필요 여부
            if (step == Step.ReceiptHandOff && state.receiptNeeded == null)
            {
                missing.Add(nameof(OrderState.receiptNeeded));
            }

            return missing;
        }
    }
}
