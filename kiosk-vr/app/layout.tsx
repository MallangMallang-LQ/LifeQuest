import './globals.css';
import './tailwind.css';
import type { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'VR Kiosk Web',
  description: 'PCVR 연습용 키오스크',
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="ko"><body>{children}</body></html>
  );
}
