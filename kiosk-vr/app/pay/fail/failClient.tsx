'use client';

import { useRouter, useSearchParams } from 'next/navigation';
import Link from 'next/link';

const METHODS = ['kakaopay','tosspay','naverpay','card','samsungpay','applepay'] as const;
type Method = typeof METHODS[number];
const LABELS: Record<Method, string> = {
  kakaopay: '카카오페이',
  tosspay: '토스페이',
  naverpay: '네이버페이',
  card: '신용/체크카드',
  samsungpay: '삼성페이',
  applepay: '애플페이',
};
function isMethod(x: string): x is Method { return (METHODS as readonly string[]).includes(x); }
function methodLabel(m?: string) { return m && isMethod(m) ? LABELS[m] : '결제'; }

function reasonText(r?: string) {
  switch (r) {
    case 'declined': return { title: '승인 거절', desc: '발급사 또는 결제사에서 승인이 거절되었습니다.' };
    case 'canceled': return { title: '사용자 취소', desc: '사용자가 결제를 취소했습니다.' };
    case 'timeout':  return { title: '시간 초과', desc: '승인 응답 지연으로 결제가 종료되었습니다.' };
    case 'error':
    default:         return { title: '결제 오류', desc: '일시적인 오류가 발생했습니다. 잠시 후 다시 시도하세요.' };
  }
}

export default function FailClient() {
  const sp = useSearchParams();
  const router = useRouter();

  const amount = Number(sp.get('amount') ?? 0);
  const method = sp.get('method') ?? undefined;
  const orderId = sp.get('orderId') ?? 'order-' + Date.now();
  const reason = sp.get('reason') ?? 'error';

  const { title, desc } = reasonText(reason);

  const retryProcessing = () => {
    const q = new URLSearchParams({
      method: String(method ?? 'card'),
      amount: String(amount),
      orderId: crypto.randomUUID(),
    });
    router.replace(`/pay/processing?${q.toString()}`);
  };

  return (
    <main className="min-h-screen flex items-center justify-center bg-neutral-100 p-6">
      <div className="w-full max-w-[640px] bg-white rounded-2xl shadow-lg p-8 text-center">
        <div className="mx-auto mb-4 w-14 h-14 rounded-full border-4 border-red-300 flex items-center justify-center">
          <span className="text-red-600 text-2xl">×</span>
        </div>

        <div className="text-2xl font-extrabold mb-1">{title}</div>
        <div className="text-neutral-600 mb-6">{desc}</div>

        <div className="text-3xl font-black mb-2">{amount.toLocaleString()}원</div>
        <div className="text-xs text-neutral-500 mb-8">
          결제수단: {methodLabel(method)} <br />
          주문번호: {orderId}
        </div>

        <div className="flex flex-wrap gap-3 justify-center">
          <button onClick={retryProcessing} className="px-5 py-3 rounded-xl bg-neutral-900 text-white font-extrabold">
            다시 시도
          </button>
          <Link href={`/checkout/?amount=${amount}`} className="px-5 py-3 rounded-xl bg-neutral-100 font-extrabold">
            결제수단 다시 선택
          </Link>
          <Link href="/" className="px-5 py-3 rounded-xl bg-neutral-100 font-extrabold">
            홈으로 돌아가기
          </Link>
        </div>
      </div>
    </main>
  );
}
