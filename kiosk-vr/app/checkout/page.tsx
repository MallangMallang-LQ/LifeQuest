import { Suspense } from 'react';
import CheckoutClient from './CheckoutClient';

export default function Page() {
  return (
    <Suspense fallback={<div className="min-h-screen flex items-center justify-center">불러오는 중…</div>}>
      <CheckoutClient />
    </Suspense>
  );
}
