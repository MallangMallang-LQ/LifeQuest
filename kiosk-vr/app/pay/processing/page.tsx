'use client';

import { useEffect } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';

const paymentMethods = {
  kakaopay: '카카오페이',
  tosspay: '토스페이',
  naverpay: '네이버페이',
  card: '신용/체크카드',
  samsungpay: '삼성페이',
  applepay: '애플페이',
} as const;

type PaymentMethod = keyof typeof paymentMethods;

const methodLabel = (m?: string) =>
  paymentMethods[m as PaymentMethod] ?? '결제';

export default function Processing() {
  const sp = useSearchParams();
  const router = useRouter();

  const amount = Number(sp.get('amount') ?? 0);
  const method = sp.get('method') ?? 'card';
  const orderId = sp.get('orderId') ?? 'order-' + Date.now();

  useEffect(() => {
    const t = setTimeout(() => {
      const paymentKey = `mock_${crypto.randomUUID()}`;
      const q = new URLSearchParams({
        paymentKey, orderId, amount: String(amount), method,
      });
      router.replace(`/pay/success?${q.toString()}`);
    }, 1500 + Math.random()*1000); // 1.5~2.5초 대기
    return () => clearTimeout(t);
  }, [amount, method, orderId, router]);

  return (
    <main className="min-h-screen flex items-center justify-center bg-neutral-100 p-6">
      <div className="w-full max-w-[640px] bg-white rounded-2xl shadow-lg p-8 text-center">
        <div className="text-2xl font-extrabold mb-3">{methodLabel(method)} 진행 중</div>
        <div className="text-neutral-600 mb-8">승인 요청을 처리하고 있어요…</div>
        <div className="w-14 h-14 rounded-full border-4 border-neutral-200 border-t-neutral-800 animate-spin mx-auto" />
        <div className="mt-8 text-lg font-bold">{amount.toLocaleString()}원</div>
        <div className="mt-2 text-xs text-neutral-500">주문번호: {orderId}</div>
      </div>
    </main>
  );
}
