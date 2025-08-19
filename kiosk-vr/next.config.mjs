// next.config.mjs
const isGhPages = process.env.GITHUB_PAGES === 'true';
const repo = 'LifeQuest/kiosk-vr'; 

/** @type {import('next').NextConfig} */
const nextConfig = {
  output: 'export',  
  images: { unoptimized: true },
  trailingSlash: true,   
  basePath: isGhPages ? `/${repo}` : undefined,
  assetPrefix: isGhPages ? `/${repo}/` : undefined,
};
export default nextConfig;
