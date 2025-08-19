import type { Metadata } from 'next';
import './tailwind.css';
import './globals.css';

export const metadata: Metadata = {
  title: 'VR Kiosk Trainer',
  description: '카페 키오스크/결제 연습용 시뮬레이터',
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="ko">
      <head>
        {/* 크롬/모바일에서 자동 다크 변환을 막고 라이트로 고정 */}
        <meta name="color-scheme" content="light" />
      </head>
      {/* ✅ 기본 텍스트는 진한 회색으로 고정 */}
      <body className="bg-neutral-100 text-neutral-900 antialiased">
        {children}
      </body>
    </html>
  );
}
