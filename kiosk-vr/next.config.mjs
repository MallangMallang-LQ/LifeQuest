// kiosk-vr/next.config.mjs
const isGh = process.env.GITHUB_PAGES === 'true';
const repo = 'LifeQuest'; // ← 정확히 레포 이름

/** @type {import('next').NextConfig} */
export default {
  output: 'export',            // 정적 내보내기
  images: { unoptimized: true },
  trailingSlash: true,
  basePath: isGh ? `/${repo}` : undefined,
  assetPrefix: isGh ? `/${repo}/` : undefined,
};
