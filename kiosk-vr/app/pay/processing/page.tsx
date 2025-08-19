import { Suspense } from 'react';
import ProcessingClient from './processingClient';

export default function Page() {
  return (
    <Suspense fallback={<div className="min-h-screen flex items-center justify-center">처리 중…</div>}>
      <ProcessingClient />
    </Suspense>
  );
}
