/** @type {import('next').NextConfig} */

// Si NEXT_PUBLIC_API_URL es una URL absoluta (otro origen), connect-src
// necesita permitirlo explícitamente además de 'self' — si no, el CSP
// bloquea el fetch aunque CORS del backend esté bien. Si es relativa
// (como "/api/v1", el caso normal: todo pasa por el proxy same-origin),
// 'self' ya alcanza y no hay origen extra que agregar.
let apiOrigin = "";
try {
  apiOrigin = new URL(process.env.NEXT_PUBLIC_API_URL ?? "").origin;
} catch {
  // URL relativa o no seteada — no hace falta origen extra en connect-src.
}

const securityHeaders = [
  { key: "X-Frame-Options",        value: "DENY" },
  { key: "X-Content-Type-Options", value: "nosniff" },
  { key: "Referrer-Policy",        value: "strict-origin-when-cross-origin" },
  { key: "Permissions-Policy",     value: "camera=(), microphone=(), geolocation=()" },
  {
    key: "Content-Security-Policy",
    value: [
      "default-src 'self'",
      "script-src 'self' 'unsafe-eval' 'unsafe-inline'",
      "style-src 'self' 'unsafe-inline'",
      "img-src 'self' data: blob:",
      "font-src 'self'",
      `connect-src 'self' ${apiOrigin}`,
      "frame-ancestors 'none'",
    ].join("; "),
  },
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
