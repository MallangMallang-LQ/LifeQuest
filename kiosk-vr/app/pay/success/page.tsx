import { Suspense } from 'react';
import SuccessClient from './successClient';

export default function Page() {
  return (
    <Suspense fallback={<div className="min-h-screen flex items-center justify-center">불러오는 중…</div>}>
      <SuccessClient />
    </Suspense>
  );
}
