// kiosk-vr/next.config.mjs
const isGh = process.env.GITHUB_PAGES === 'true';
const repo = 'LifeQuest'; // 레포 이름

/** @type {import('next').NextConfig} */
export default {
  output: 'export',
  images: { unoptimized: true },
  trailingSlash: true,
  basePath: isGh ? `/${repo}` : undefined,
  assetPrefix: isGh ? `/${repo}/` : undefined,
  // 빌드 때 ESLint 때문에 막히지 않게 하려면(선택):
  // eslint: { ignoreDuringBuilds: true },
};
