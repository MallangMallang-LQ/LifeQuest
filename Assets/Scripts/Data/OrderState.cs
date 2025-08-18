using System;
using UnityEngine;

namespace LifeQuest.NPCFlow.Data
{
    [Serializable]
    public class OrderState
    {
        public OrderModels.MenuItem menuItem = OrderModels.MenuItem.None;
        public OrderModels.TempOption tempOption = OrderModels.TempOption.None;
        public OrderModels.DineOption dineOption = OrderModels.DineOption.None;

        // ����Ʈ
        public OrderModels.PointsOption pointsOption = OrderModels.PointsOption.None;
        public string pointsPhoneNumber = string.Empty; // ����Ʈ ���� ��ȣ

        // ����
        public OrderModels.CouponOption couponOption = OrderModels.CouponOption.None;
        public bool couponPresented = false;        // �ǹ�/���ڵ� ���� ����
        public string couponCode = string.Empty;    // (����) ���ڵ�/���� �ڵ� �ؽ�Ʈ

        public bool hasCoupon => couponPresented || !string.IsNullOrEmpty(couponCode);  // ȣȯ��

        // ����
        public OrderModels.PaymentMethod paymentMethod = OrderModels.PaymentMethod.None;
        public OrderModels.CashReceiptOption cashReceiptOption = OrderModels.CashReceiptOption.None;
        public string cashReceiptPhoneNumber = string.Empty; // ���ݿ����� ��ȣ

        // ������
        public bool? receiptNeeded = null; // null=����, true/false=������

        /// <summary>
        /// ������ ���Ե��� ��� ä�������� Ȯ��
        /// </summary>
        public bool IsFilled(params string[] slots)
        {
            foreach (var slot in slots)
            {
                switch (slot)
                {
                    case nameof(menuItem):
                        if (menuItem == OrderModels.MenuItem.None) return false;
                        break;
                    case nameof(tempOption):
                        if (tempOption == OrderModels.TempOption.None) return false;
                        break;
                    case nameof(dineOption):
                        if (dineOption == OrderModels.DineOption.None) return false;
                        break;
                    case nameof(pointsOption):
                        if (pointsOption == OrderModels.PointsOption.None) return false;
                        break;
                    case nameof(pointsPhoneNumber):
                        if (string.IsNullOrEmpty(pointsPhoneNumber)) return false;
                        break;
                    case nameof(couponOption):
                        if (couponOption == OrderModels.CouponOption.None) return false;
                        break;
                    case nameof(couponPresented):
                        if (!couponPresented) return false;
                        break;
                    case nameof(couponCode):
                        if (string.IsNullOrEmpty(couponCode)) return false;
                        break;
                    case nameof(paymentMethod):
                        if (paymentMethod == OrderModels.PaymentMethod.None) return false;
                        break;
                    case nameof(cashReceiptOption):
                        if (cashReceiptOption == OrderModels.CashReceiptOption.None) return false;
                        break;
                    case nameof(cashReceiptPhoneNumber):
                        if (string.IsNullOrEmpty(cashReceiptPhoneNumber)) return false;
                        break;
                    case nameof(receiptNeeded):
                        if (receiptNeeded == null) return false;
                        break;
                }
            }
            return true;
        }

        /// <summary>
        /// ��� ���� �ʱ�ȭ
        /// </summary>
        public void Reset()
        {
            menuItem = OrderModels.MenuItem.None;
            tempOption = OrderModels.TempOption.None;
            dineOption = OrderModels.DineOption.None;

            pointsOption = OrderModels.PointsOption.None;
            pointsPhoneNumber = string.Empty;
            
            couponOption = OrderModels.CouponOption.None;
            couponPresented = false;
            couponCode = string.Empty;

            paymentMethod = OrderModels.PaymentMethod.None;
            cashReceiptOption = OrderModels.CashReceiptOption.None;
            cashReceiptPhoneNumber = string.Empty;

            receiptNeeded = null;
        }
    }
}
