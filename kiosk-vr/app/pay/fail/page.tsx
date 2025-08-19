import { Suspense } from 'react';
import FailClient from './failClient';

export default function Page() {
  return (
    <Suspense fallback={<div className="min-h-screen flex items-center justify-center">불러오는 중…</div>}>
      <FailClient />
    </Suspense>
  );
}
