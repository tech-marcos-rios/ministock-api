import { NextRequest, NextResponse } from "next/server";

/**
 * Genera un nonce por request y arma el Content-Security-Policy con él.
 *
 * Vive acá (no en next.config.mjs) porque el nonce tiene que ser distinto
 * en cada response — next.config.mjs solo puede devolver headers estáticos.
 *
 * Next.js detecta el nonce leyendo el header Content-Security-Policy del
 * REQUEST que llega a su renderer (no alcanza con setearlo en la response) y
 * lo aplica automáticamente a los scripts/estilos que él mismo inyecta
 * (hidratación, RSC payload, styled-jsx). Si el día de mañana se agrega un
 * <script> inline propio, hay que pasarle nonce={headers().get("x-nonce")}
 * a mano.
 *
 * OJO — esto solo funciona en páginas con render dinámico. Una página
 * prerenderizada como estática (sin cookies()/headers()/fetch sin cache en
 * ningún Server Component del árbol) se genera una sola vez en build time,
 * así que nunca puede llevar un nonce distinto por request: los scripts
 * quedan sin nonce y el browser los bloquea en silencio (sin error de build
 * ni de runtime del lado server — solo se ve como "Content-Security-Policy
 * directive... blocked" en la consola del browser). Por eso
 * src/app/layout.tsx llama a headers() — fuerza dinámico a toda la app.
 *
 * Igual que en next.config.mjs: si NEXT_PUBLIC_API_URL es una URL absoluta
 * (otro origen), connect-src necesita permitirlo explícitamente. Si es
 * relativa (el caso normal — todo pasa por el proxy same-origin de la API,
 * sin relación con este archivo), 'self' ya alcanza.
 */
function apiOrigin(): string {
  try {
    return new URL(process.env.NEXT_PUBLIC_API_URL ?? "").origin;
  } catch {
    return "";
  }
}

export function proxy(request: NextRequest) {
  const nonce = Buffer.from(crypto.randomUUID()).toString("base64");

  const csp = [
    "default-src 'self'",
    `script-src 'self' 'nonce-${nonce}' 'strict-dynamic'`,
    `style-src 'self' 'nonce-${nonce}'`,
    "img-src 'self' data: blob:",
    "font-src 'self'",
    `connect-src 'self' ${apiOrigin()}`,
    "frame-ancestors 'none'",
    "base-uri 'self'",
    "object-src 'none'",
  ].join("; ");

  // Next.js arma el nonce de los scripts/estilos que él mismo inyecta leyendo
  // el header Content-Security-Policy del REQUEST que llega a su renderer
  // (no el de x-nonce) — hay que setearlo ahí, no solo en la response.
  const requestHeaders = new Headers(request.headers);
  requestHeaders.set("x-nonce", nonce);
  requestHeaders.set("Content-Security-Policy", csp);

  const response = NextResponse.next({ request: { headers: requestHeaders } });
  response.headers.set("Content-Security-Policy", csp);
  return response;
}

export const config = {
  matcher: [
    // Corre en todo menos assets estáticos de Next y el ícono — ahí no hay
    // HTML que proteger con CSP y el nonce sería trabajo de más.
    "/((?!_next/static|_next/image|favicon.ico).*)",
  ],
};
