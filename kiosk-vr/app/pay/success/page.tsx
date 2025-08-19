'use client';
import { useSearchParams } from 'next/navigation';

const paymentLabels = {
  kakaopay: '카카오페이',
  tosspay: '토스페이',
  naverpay: '네이버페이',
  card: '신용/체크카드',
  samsungpay: '삼성페이',
  applepay: '애플페이',
} as const;

type PaymentMethod = keyof typeof paymentLabels;

const label = (m?: string) =>
  paymentLabels[m as PaymentMethod] ?? '결제';

export default function PaySuccess() {
  const p = useSearchParams();
  const paymentKey = p.get('paymentKey');
  const orderId = p.get('orderId');
  const amount = Number(p.get('amount') ?? 0);
  const method = p.get('method') ?? undefined;

  return (
    <main className="min-h-screen flex items-center justify-center bg-neutral-100 p-6">
      <div className="w-full max-w-[640px] bg-white rounded-2xl shadow-lg p-8 text-center">
        <div className="text-2xl font-extrabold mb-2">결제 완료</div>
        <div className="text-neutral-600 mb-6">{label(method)}로 결제가 완료되었습니다.</div>
        <div className="text-3xl font-black mb-2">{amount.toLocaleString()}원</div>
        <div className="text-xs text-neutral-500 mb-8">
          주문번호: {orderId} <br/> 결제키: {paymentKey}
        </div>
        <a href="/kiosk" className="px-5 py-3 rounded-xl bg-neutral-900 text-white font-extrabold">
          키오스크로 돌아가기
        </a>
      </div>
    </main>
  );
}
