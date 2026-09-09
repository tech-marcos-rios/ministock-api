/** @type {import('next').NextConfig} */

// El Content-Security-Policy se arma en src/proxy.ts, no acá — necesita un
// nonce distinto por request (script-src/style-src), y headers() solo puede
// devolver valores estáticos. Estos son los headers que sí son iguales en
// cada response.
const securityHeaders = [
  { key: "X-Frame-Options",        value: "DENY" },
  { key: "X-Content-Type-Options", value: "nosniff" },
  { key: "Referrer-Policy",        value: "strict-origin-when-cross-origin" },
  { key: "Permissions-Policy",     value: "camera=(), microphone=(), geolocation=()" },
];

const nextConfig = {
  async headers() {
    return [
      {
        source: "/(.*)",
        headers: securityHeaders,
      },
    ];
  },
};

export default nextConfig;
