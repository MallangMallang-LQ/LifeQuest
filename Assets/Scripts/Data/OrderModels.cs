using UnityEngine;

namespace LifeQuest.NPCFlow.Data
{
    public class OrderModels
    {
        // 메뉴 종류
        public enum MenuItem
        {
            None = 0,
            Americano,
            Latte,
            Cappuccino,
            Mocha
        }

        // 온도 옵션
        public enum TempOption
        {
            None = 0,
            Hot,
            Ice
        }

        // 매장/테이크아웃
        public enum DineOption
        {
            None = 0,
            DineIn,
            TakeOut
        }

        // 결제 수단
        public enum PaymentMethod
        {
            None = 0,
            Card,
            Cash
        }

        // 현금영수증 여부
        public enum CashReceiptOption
        {
            None = 0,
            Yes,
            No
        }

        // 포인트 적립 여부
        public enum PointsOption
        {
            None = 0,
            Yes,
            No
        }

        // 쿠폰 사용 여부
        public enum CouponOption
        {
            None = 0,
            Yes,
            No
        }
    }
}