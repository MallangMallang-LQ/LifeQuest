'use client';

import { useMemo, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import Link from 'next/link';
import clsx from 'clsx';

type MethodId =
  | 'kakaopay' | 'tosspay' | 'naverpay'
  | 'card' | 'samsungpay' | 'applepay';

const QUICK: { id: MethodId; label: string }[] = [
  { id: 'kakaopay', label: '카카오페이' },
  { id: 'tosspay',  label: '토스페이' },
  { id: 'naverpay', label: '네이버페이' },
];

const CARD: { id: MethodId; label: string }[] = [
  { id: 'card',      label: '신용/체크카드' },
  { id: 'samsungpay',label: '삼성페이' },
  { id: 'applepay',  label: '애플페이' },
];

const KR = (n: number) => n.toLocaleString() + '원';

export default function CheckoutClient() {
  const sp = useSearchParams();
  const router = useRouter();
  const baseAmount = Number(sp.get('amount') ?? 0);

  // 쿠폰
  const [coupon, setCoupon] = useState('');
  const [couponMsg, setCouponMsg] = useState<string>('');
  const [discount, setDiscount] = useState<number>(0);

  const finalAmount = useMemo(() => Math.max(0, baseAmount - discount), [baseAmount, discount]);

  const applyCoupon = () => {
    const code = coupon.trim().toUpperCase();
    if (!code) { setCouponMsg('쿠폰 코드를 입력하세요'); return; }
    if (code === 'FREE') { setDiscount(baseAmount); setCouponMsg('쿠폰 적용: 전액 할인'); return; }
    if (code === 'FIX1000') { setDiscount(Math.min(1000, baseAmount)); setCouponMsg('쿠폰 적용: 1,000원 할인'); return; }
    if (code === 'PCT10') { const d = Math.floor(baseAmount*0.1); setDiscount(Math.min(d, baseAmount)); setCouponMsg(`쿠폰 적용: ${KR(d)} 할인`); return; }
    setCouponMsg('유효하지 않은 쿠폰입니다');
  };

  const goPay = (method: MethodId) => {
    const q = new URLSearchParams({
      method,
      amount: String(finalAmount),
      orderId: crypto.randomUUID(),
    });
    router.push(`/pay/processing?${q.toString()}`);
  };

  return (
    <main className="min-h-screen flex items-center justify-center bg-neutral-100 p-6">
      <div className="w-full max-w-[980px] bg-white rounded-2xl shadow-lg p-6">
        <h2 className="text-2xl font-extrabold mb-2">결제</h2>
        <div className="text-neutral-600 mb-6">결제 금액</div>

        <div className="flex items-end justify-between gap-4 mb-6">
          <div className="text-3xl font-black">{KR(finalAmount)}</div>
          <div className="text-sm text-neutral-500">원금 {KR(baseAmount)} {discount>0 && <>/ 할인 -{KR(discount)}</>}</div>
        </div>

        {/* 쿠폰 */}
        <section className="mb-8">
          <div className="text-sm font-semibold mb-2">쿠폰</div>
          <div className="flex gap-2">
            <input
              value={coupon}
              onChange={e=>setCoupon(e.target.value)}
              placeholder="쿠폰 코드 입력 (FREE, FIX1000, PCT10)"
              className="flex-1 px-4 py-3 rounded-xl bg-neutral-100 outline-none"
            />
            <button onClick={applyCoupon} className="px-5 py-3 rounded-xl bg-neutral-900 text-white font-bold">
              적용
            </button>
          </div>
          {couponMsg && <div className="mt-2 text-sm text-teal-700">{couponMsg}</div>}
        </section>

        {/* 간편결제 */}
        <section className="mb-6">
          <div className="text-sm font-semibold mb-3">간편 결제</div>
          <div className="grid grid-cols-3 gap-3">
            {QUICK.map(m => (
              <button
                key={m.id}
                onClick={()=>goPay(m.id)}
                className={clsx('h-16 rounded-xl bg-neutral-50 hover:bg-neutral-100 transition','text-lg font-bold')}
              >
                {m.label}
              </button>
            ))}
          </div>
        </section>

        {/* 카드 결제 */}
        <section className="mb-2">
          <div className="text-sm font-semibold mb-3">카드 결제</div>
          <div className="grid grid-cols-3 gap-3">
            {CARD.map(m => (
              <button
                key={m.id}
                onClick={()=>goPay(m.id)}
                className="h-16 rounded-xl bg-neutral-50 hover:bg-neutral-100 transition text-lg font-bold"
              >
                {m.label}
              </button>
            ))}
          </div>
        </section>

        <div className="mt-6 text-sm text-neutral-500">
          ※ 훈련용 모의결제입니다. 선택 후 <b>결제 중</b> 화면으로 이동하고, 몇 초 뒤 자동으로 완료됩니다.
        </div>

        <div className="mt-6">
          <Link href="/" className="text-sm text-neutral-500 hover:underline">← 홈으로 돌아가기</Link>
        </div>
      </div>
    </main>
  );
}
