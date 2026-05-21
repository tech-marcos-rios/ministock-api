import { NextRequest, NextResponse } from "next/server";

/**
 * Proxy Route Handler — reenvía todas las peticiones de /api/v1/* al backend .NET.
 *
 * POR QUÉ EXISTE ESTE PROXY:
 * El frontend está en Vercel (HTTPS) y el backend en Hetzner (HTTP).
 * Los browsers modernos bloquean llamadas "mixed-content": una página HTTPS
 * no puede hacer fetch a un endpoint HTTP. El proxy resuelve esto porque
 * la llamada HTTP la hace el servidor de Vercel (Node.js), no el browser.
 * El browser solo ve HTTPS (frontend → Vercel), sin saber que internamente
 * Vercel llama a HTTP (Vercel → Hetzner).
 *
 * FLUJO:
 *   Browser → HTTPS → Vercel /api/v1/[...path] → HTTP → Hetzner :5010
 *
 * HEADERS EXCLUIDOS AL REENVIAR:
 * - host: el backend rechazaría peticiones con el host de Vercel.
 * - connection / transfer-encoding: headers hop-by-hop que no deben reenviarse.
 * - content-length: se recalcula automáticamente según el body.
 * - expect: Kestrel (.NET) no implementa "100 Continue" y rechaza la conexión.
 *   Este fue el bug que causaba "TypeError: fetch failed" en todos los POSTs.
 */

const BACKEND = process.env.API_BASE_URL ?? "http://localhost:5197/api/v1";

const HOP_BY_HOP_HEADERS = ["host", "connection", "transfer-encoding", "content-length", "expect"];

async function proxy(req: NextRequest, path: string[]) {
  const url = `${BACKEND}/${path.join("/")}${req.nextUrl.search}`;

  const headers = new Headers();
  req.headers.forEach((value, key) => {
    if (!HOP_BY_HOP_HEADERS.includes(key.toLowerCase())) {
      headers.set(key, value);
    }
  });

  let body: BodyInit | null = null;
  if (!["GET", "HEAD"].includes(req.method)) {
    body = await req.arrayBuffer();
  }

  let res: Response;
  try {
    res = await fetch(url, { method: req.method, headers, body });
  } catch (err) {
    console.error("[proxy] fetch error to", url, err);
    return NextResponse.json({ error: String(err) }, { status: 502 });
  }

  const resHeaders = new Headers();
  res.headers.forEach((value, key) => {
    if (!["transfer-encoding", "connection"].includes(key.toLowerCase())) {
      resHeaders.set(key, value);
    }
  });

  return new NextResponse(await res.arrayBuffer(), {
    status: res.status,
    headers: resHeaders,
  });
}

export async function GET(req: NextRequest, { params }: { params: { path: string[] } }) {
  return proxy(req, params.path);
}
export async function POST(req: NextRequest, { params }: { params: { path: string[] } }) {
  return proxy(req, params.path);
}
export async function PUT(req: NextRequest, { params }: { params: { path: string[] } }) {
  return proxy(req, params.path);
}
export async function DELETE(req: NextRequest, { params }: { params: { path: string[] } }) {
  return proxy(req, params.path);
}
export async function PATCH(req: NextRequest, { params }: { params: { path: string[] } }) {
  return proxy(req, params.path);
}
