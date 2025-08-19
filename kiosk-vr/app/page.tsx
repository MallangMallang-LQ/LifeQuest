import Link from 'next/link';

export default function Home() {
  return (
    <main className="min-h-screen bg-gradient-to-b from-teal-50 to-neutral-100 flex items-center justify-center p-6">
      <div className="w-full max-w-[980px]">
        <section className="bg-white rounded-3xl shadow-xl p-8 sm:p-12 text-center">
          <h1 className="text-4xl sm:text-5xl font-black tracking-tight">주문 방식 선택</h1>
          <p className="mt-3 text-neutral-600">매장 / 포장을 먼저 선택하세요</p>

          <div className="mt-8 grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Link
              href="/kiosk/?service=dinein"
              className="h-36 sm:h-44 rounded-2xl bg-neutral-900 text-white flex items-center justify-center gap-3 shadow-lg hover:shadow-xl active:scale-[.98] transition text-2xl sm:text-3xl font-extrabold"
              aria-label="매장 이용하기"
            >
              <span className="text-4xl">🍽️</span><span>매장</span>
            </Link>
            <Link
              href="/kiosk/?service=takeout"
              className="h-36 sm:h-44 rounded-2xl bg-teal-700 text-white flex items-center justify-center gap-3 shadow-lg hover:shadow-xl active:scale-[.98] transition text-2xl sm:text-3xl font-extrabold"
              aria-label="포장하기"
            >
              <span className="text-4xl">🥡</span><span>포장</span>
            </Link>
          </div>

          <div className="mt-6 text-xs text-neutral-500">
            버튼 높이 ≥ 140px, VR 포인터로 쉽게 누를 수 있게 구성했어요.
          </div>
        </section>
      </div>
    </main>
  );
}
