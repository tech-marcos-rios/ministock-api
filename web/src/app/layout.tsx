import type { Metadata } from "next";
import localFont from "next/font/local";
import { headers } from "next/headers";
import "./globals.css";
import { QueryProvider } from "@/providers/query-provider";

const geistSans = localFont({
  src: "./fonts/GeistVF.woff",
  variable: "--font-geist-sans",
  weight: "100 900",
});
const geistMono = localFont({
  src: "./fonts/GeistMonoVF.woff",
  variable: "--font-geist-mono",
  weight: "100 900",
});

export const metadata: Metadata = {
  title: "MiniStock",
  description: "Sistema de gestión de inventario",
};

export default async function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  // Leer headers() fuerza render dinámico (opta afuera del prerender estático)
  // — sin esto, la página se genera una sola vez en build time y nunca puede
  // llevar el nonce por-request que arma src/middleware.ts.
  await headers();

  return (
    <html lang="es">
      <body
        className={`${geistSans.variable} ${geistMono.variable} antialiased bg-gray-50 text-gray-900`}
      >
        <QueryProvider>{children}</QueryProvider>
      </body>
    </html>
  );
}
