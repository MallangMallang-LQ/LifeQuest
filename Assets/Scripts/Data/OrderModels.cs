using UnityEngine;

namespace LifeQuest.NPCFlow.Data
{
    public class OrderModels
    {
        // �޴� ����
        public enum MenuItem
        {
            None = 0,
            Americano,
            Latte,
            Cappuccino,
            Mocha
        }

        // �µ� �ɼ�
        public enum TempOption
        {
            None = 0,
            Hot,
            Ice
        }

        // ����/����ũ�ƿ�
        public enum DineOption
        {
            None = 0,
            DineIn,
            TakeOut
        }

        // ���� ����
        public enum PaymentMethod
        {
            None = 0,
            Card,
            Cash
        }

        // ���ݿ����� ����
        public enum CashReceiptOption
        {
            None = 0,
            Yes,
            No
        }

        // ����Ʈ ���� ����
        public enum PointsOption
        {
            None = 0,
            Yes,
            No
        }

        // ���� ��� ����
        public enum CouponOption
        {
            None = 0,
            Yes,
            No
        }
    }
}