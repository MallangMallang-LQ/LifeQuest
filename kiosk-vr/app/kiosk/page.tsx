'use client';

import { useMemo, useState } from 'react';
import Link from 'next/link';
import clsx from 'clsx';

/** ---------- 단일(통합) 메뉴 데이터 ----------
 *  시뮬용 기본가이며 지점/프로모션에 따라 다를 수 있습니다.
 *  “대표 메뉴” 중심으로 정리했고, 가능 온도(HOT/ICE)를 표시합니다.
 */
type Cat = '커피' | '라떼' | '에이드' | '스무디' | '프라페' | '티';

type Item = {
  id: string;
  name: string;
  price: number;      // Regular, 기본 기준
  cat: Cat;
  hot?: boolean;
  ice?: boolean;
};

const ITEMS: Item[] = [
  // 커피
  { id: 'ame_hot',      name: '아메리카노(HOT)',  price: 1800, cat: '커피', hot: true,  ice: false },
  { id: 'ame_ice',      name: '아메리카노(ICE)',  price: 1800, cat: '커피', hot: false, ice: true  },
  { id: 'coldbrew',     name: '콜드브루',         price: 2800, cat: '커피', hot: false, ice: true  },
  // 라떼 / 스페셜티
  { id: 'latte',        name: '카페라떼',         price: 2800, cat: '라떼', hot: true,  ice: true  },
  { id: 'vanilla_latte',name: '바닐라라떼',       price: 3300, cat: '라떼', hot: true,  ice: true  },
  { id: 'caramel_mac',  name: '카라멜마끼아또',   price: 3500, cat: '라떼', hot: true,  ice: true  },
  { id: 'cafe_mocha',   name: '카페모카',         price: 3300, cat: '라떼', hot: true,  ice: true  },
  { id: 'matcha_latte', name: '말차라떼',         price: 3300, cat: '라떼', hot: true,  ice: true  },
  // 에이드
  { id: 'ade_lemon',    name: '레몬에이드',       price: 3200, cat: '에이드', hot: false, ice: true },
  { id: 'ade_grapefruit', name:'자몽에이드',     price: 3200, cat: '에이드', hot: false, ice: true },
  { id: 'ade_green',    name: '청포도에이드',     price: 3300, cat: '에이드', hot: false, ice: true },
  // 스무디 / 요거트
  { id: 'smoothie_straw', name: '딸기스무디',     price: 3800, cat: '스무디', hot: false, ice: true },
  { id: 'smoothie_mango',  name: '망고스무디',    price: 3800, cat: '스무디', hot: false, ice: true },
  { id: 'yogurt_plain',    name: '플레인요거트',  price: 3500, cat: '스무디', hot: false, ice: true },
  { id: 'yogurt_straw',    name: '딸기요거트',    price: 3800, cat: '스무디', hot: false, ice: true },
  // 프라페 / 블렌디드
  { id: 'frappe_mocha',    name: '모카프라페',    price: 3900, cat: '프라페', hot: false, ice: true },
  { id: 'frappe_choco',    name: '초코프라페',    price: 3900, cat: '프라페', hot: false, ice: true },
  { id: 'frappe_greentea', name: '그린티프라페',  price: 3900, cat: '프라페', hot: false, ice: true },
  // 티 / 기타
  { id: 'tea_black',       name: '블랙티',        price: 2500, cat: '티', hot: true,  ice: true  },
  { id: 'tea_earlgrey',    name: '얼그레이',      price: 2700, cat: '티', hot: true,  ice: true  },
  { id: 'choco_latte',     name: '초코라떼',      price: 3000, cat: '티', hot: true,  ice: true  },
];

/** ---------- 옵션/장바구니 모델 ---------- */
type TempOpt = 'HOT' | 'ICE';
type SizeOpt = 'REG' | 'LARGE';

type CartLine = {
  key: string; // 동일옵션 병합 키
  itemId: string;
  name: string;
  base: number;
  qty: number;
  temp?: TempOpt;
  size?: SizeOpt;
  addShot?: number;
  syrup?: '바닐라' | '헤이즐넛' | null;
  extra: number; // 옵션가 합(1잔)
};

const SHOT_PRICE = 600;         // 샷 추가 +600
const SYRUP_PRICE = 500;        // 시럽 +500
const LARGE_EXTRA = 500;        // Large +500 (시뮬)

/** ---------- 유틸 ---------- */
const KR = (n: number) => n.toLocaleString() + '원';
const CATS: Cat[] = ['커피', '라떼', '에이드', '스무디', '프라페', '티'];

function makeKey(
  id: string, temp?: TempOpt, size?: SizeOpt, shot?: number, syrup?: CartLine['syrup']
) {
  return [id, temp ?? '-', size ?? '-', shot ?? 0, syrup ?? '-'].join('|');
}

/** ---------- 메인 ---------- */
export default function KioskLandscapeUnified() {
  const [cat, setCat] = useState<Cat>('커피');
  const catalog = useMemo(() => ITEMS.filter(i => i.cat === cat), [cat]);

  // cart
  const [cart, setCart] = useState<CartLine[]>([]);
  const total = useMemo(() => cart.reduce((s, l) => s + (l.base + l.extra) * l.qty, 0), [cart]);

  // modal state
  const [open, setOpen] = useState(false);
  const [selItem, setSelItem] = useState<Item | null>(null);
  const [temp, setTemp] = useState<TempOpt | undefined>(undefined);
  const [size, setSize] = useState<SizeOpt>('REG');
  const [shot, setShot] = useState(0);
  const [syrup, setSyrup] = useState<CartLine['syrup']>(null);

  const optionExtra = useMemo(() => {
    const shotExtra = shot * SHOT_PRICE;
    const syrupExtra = syrup ? SYRUP_PRICE : 0;
    const sizeExtra = size === 'LARGE' ? LARGE_EXTRA : 0;
    return shotExtra + syrupExtra + sizeExtra;
  }, [shot, syrup, size]);

  const openModal = (it: Item) => {
    setSelItem(it);
    setTemp(it.hot ? 'HOT' : it.ice ? 'ICE' : undefined);
    setSize('REG');
    setShot(0);
    setSyrup(null);
    setOpen(true);
  };

  const addToCart = () => {
    if (!selItem) return;
    const key = makeKey(selItem.id, temp, size, shot, syrup);
    setCart(prev => {
      const idx = prev.findIndex(l => l.key === key);
      if (idx >= 0) {
        const cp = [...prev];
        cp[idx] = { ...cp[idx], qty: cp[idx].qty + 1 };
        return cp;
      }
      return [
        ...prev,
        {
          key,
          itemId: selItem.id,
          name: selItem.name,
          base: selItem.price,
          qty: 1,
          temp,
          size,
          addShot: shot,
          syrup,
          extra: optionExtra,
        },
      ];
    });
    setOpen(false);
  };

  const changeQty = (key: string, delta: number) => {
    setCart(prev =>
      prev.flatMap(l => {
        if (l.key !== key) return [l];
        const q = l.qty + delta;
        if (q <= 0) return [];
        return [{ ...l, qty: q }];
      }),
    );
  };

  return (
    <main className="min-h-screen bg-neutral-100 flex flex-col items-center pb-[320px] px-6">
      <div className="w-full max-w-[1280px] pt-6 space-y-4">
        {/* 카테고리 탭 (보더 제거, 활성은 진한 배경) */}
        <div className="flex justify-center gap-3">
          {CATS.map(c => (
            <button
              key={c}
              onClick={() => setCat(c)}
              className={clsx(
                'px-5 py-3 rounded-xl text-lg transition',
                c === cat ? 'bg-neutral-900 text-white' : 'bg-white shadow-sm hover:shadow'
              )}
            >
              {c}
            </button>
          ))}
        </div>

        {/* 메뉴 그리드 (카드 보더 제거) */}
        <div className="grid grid-cols-4 gap-4">
          {catalog.map(i => (
            <button
              key={i.id}
              onClick={() => openModal(i)}
              className="h-40 text-left p-4 rounded-xl bg-white shadow-md hover:shadow-lg transition"
            >
              <div className="font-extrabold text-lg">{i.name}</div>
              <div className="text-neutral-600">{KR(i.price)}</div>
              <div className="text-xs text-neutral-500 mt-3">탭해서 옵션 선택</div>
            </button>
          ))}
          {catalog.length === 0 && (
            <div className="col-span-4 text-center text-neutral-500 py-12">
              해당 카테고리에 메뉴가 없습니다.
            </div>
          )}
        </div>
      </div>

      {/* 바닥 장바구니 (센터 고정, 보더 제거) */}
      <div className="fixed left-1/2 -translate-x-1/2 bottom-0 w-full max-w-[1280px] bg-white shadow-[0_-8px_24px_rgba(0,0,0,0.08)] px-5 py-4">
        <div className="grid grid-cols-[1fr_auto_auto] items-center gap-4">
          <div className="font-extrabold text-lg">장바구니</div>
          <div className="text-2xl font-black">{KR(total)}</div>
          <Link
            href={`/checkout?amount=${total}`}
            className={clsx(
              'px-5 py-3 rounded-xl font-extrabold text-white',
              total > 0 ? 'bg-teal-700' : 'bg-neutral-400 pointer-events-none'
            )}
          >
            결제하기
          </Link>
        </div>

        <div className="mt-3 max-h-48 overflow-y-auto pr-1 divide-y divide-neutral-200">
          {cart.length === 0 && (
            <div className="text-neutral-500 py-2">담긴 항목이 없습니다.</div>
          )}
          {cart.map(l => (
            <div key={l.key} className="grid grid-cols-[1fr_auto_auto] items-center gap-3 py-2">
              <div>
                <div className="font-semibold">
                  {l.name}
                  {l.temp ? ` · ${l.temp}` : ''}{l.size === 'LARGE' ? ' · Large' : ''}
                </div>
                <div className="text-xs text-neutral-500">
                  {l.addShot ? `샷 ${l.addShot} · ` : ''}
                  {l.syrup ? `${l.syrup}시럽 · ` : ''}
                  옵션가 {KR(l.extra)}
                </div>
              </div>
              <div className="flex items-center gap-2">
                <button onClick={() => changeQty(l.key, -1)} className="px-3 py-1 rounded bg-neutral-100 shadow-inner">-</button>
                <div className="w-8 text-center">{l.qty}</div>
                <button onClick={() => changeQty(l.key, +1)} className="px-3 py-1 rounded bg-neutral-100 shadow-inner">+</button>
              </div>
              <div className="font-bold">
                {KR((l.base + l.extra) * l.qty)}
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* 옵션 모달 (보더 제거, 선택 시 진한 배경) */}
      {open && selItem && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center p-6">
          <div className="w-full max-w-lg bg-white rounded-2xl p-5 shadow-xl">
            <div className="flex items-center justify-between mb-2">
              <div className="text-xl font-extrabold">{selItem.name}</div>
              <button onClick={() => setOpen(false)} className="px-3 py-1 rounded-lg bg-neutral-100">닫기</button>
            </div>

            {/* 온도 */}
            <div className="mt-3">
              <div className="text-sm font-semibold mb-2">온도</div>
              <div className="flex gap-2">
                <button
                  disabled={!selItem.hot}
                  onClick={() => setTemp('HOT')}
                  className={clsx(
                    'px-3 py-2 rounded-lg transition',
                    !selItem.hot && 'opacity-40 cursor-not-allowed',
                    temp === 'HOT' ? 'bg-neutral-900 text-white' : 'bg-white shadow-sm hover:shadow'
                  )}
                >
                  HOT
                </button>
                <button
                  disabled={!selItem.ice}
                  onClick={() => setTemp('ICE')}
                  className={clsx(
                    'px-3 py-2 rounded-lg transition',
                    !selItem.ice && 'opacity-40 cursor-not-allowed',
                    temp === 'ICE' ? 'bg-neutral-900 text-white' : 'bg-white shadow-sm hover:shadow'
                  )}
                >
                  ICE
                </button>
              </div>
            </div>

            {/* 사이즈 */}
            <div className="mt-4">
              <div className="text-sm font-semibold mb-2">사이즈</div>
              <div className="flex gap-2">
                {(['REG','LARGE'] as SizeOpt[]).map(s => (
                  <button
                    key={s}
                    onClick={() => setSize(s)}
                    className={clsx(
                      'px-3 py-2 rounded-lg transition',
                      size === s ? 'bg-neutral-900 text-white' : 'bg-white shadow-sm hover:shadow'
                    )}
                  >
                    {s === 'REG' ? 'Regular' : `Large(+${LARGE_EXTRA})`}
                  </button>
                ))}
              </div>
            </div>

            {/* 샷/시럽 */}
            <div className="mt-4 grid grid-cols-2 gap-4">
              <div>
                <div className="text-sm font-semibold mb-2">샷 추가(+{SHOT_PRICE}원/샷)</div>
                <div className="flex items-center gap-2">
                  <button onClick={() => setShot(Math.max(0, shot - 1))} className="px-3 py-1 rounded bg-neutral-100 shadow-inner">-</button>
                  <div className="w-10 text-center">{shot}</div>
                  <button onClick={() => setShot(shot + 1)} className="px-3 py-1 rounded bg-neutral-100 shadow-inner">+</button>
                </div>
              </div>
              <div>
                <div className="text-sm font-semibold mb-2">시럽(+{SYRUP_PRICE}원)</div>
                <div className="flex gap-2">
                  {(['바닐라','헤이즐넛'] as const).map(s => (
                    <button
                      key={s}
                      onClick={() => setSyrup(syrup === s ? null : s)}
                      className={clsx(
                        'px-3 py-2 rounded-lg transition',
                        syrup === s ? 'bg-neutral-900 text-white' : 'bg-white shadow-sm hover:shadow'
                      )}
                    >
                      {s}
                    </button>
                  ))}
                </div>
              </div>
            </div>

            {/* 가격 & 담기 */}
            <div className="mt-5 flex items-center justify-between">
              <div className="text-lg">
                1잔 금액: <b>{KR((selItem.price + optionExtra))}</b>
              </div>
              <button
                onClick={addToCart}
                className="px-5 py-3 rounded-xl bg-teal-700 text-white font-extrabold"
              >
                담기
              </button>
            </div>
          </div>
        </div>
      )}
    </main>
  );
}
