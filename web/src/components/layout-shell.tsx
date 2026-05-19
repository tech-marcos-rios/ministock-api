"use client";

import { useEffect } from "react";
import { useRouter, usePathname } from "next/navigation";
import Link from "next/link";
import { LayoutDashboard, Package, Tags, ArrowLeftRight, LogOut } from "lucide-react";
import { isAuthenticated, clearToken } from "@/lib/auth";

const navItems = [
  { href: "/",            label: "Dashboard",    icon: LayoutDashboard },
  { href: "/productos",   label: "Productos",    icon: Package },
  { href: "/categorias",  label: "Categorías",   icon: Tags },
  { href: "/movimientos", label: "Movimientos",  icon: ArrowLeftRight },
];

const pageTitles: Record<string, string> = {
  "/":            "Dashboard",
  "/productos":   "Productos",
  "/categorias":  "Categorías",
  "/movimientos": "Movimientos",
};

export function LayoutShell({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();

  // TODO (post-MVP): mover la protección de rutas a middleware de Next.js (middleware.ts)
  // para que el redirect ocurra server-side y no haya flash de contenido protegido.
  useEffect(() => {
    if (!isAuthenticated()) {
      router.replace("/login");
    }
  }, [router]);

  function handleLogout() {
    clearToken();
    router.replace("/login");
  }

  return (
    <div className="flex h-screen overflow-hidden">
      <aside className="w-64 bg-white border-r border-gray-200 hidden md:flex flex-col">
        <div className="h-16 flex items-center px-6 border-b border-gray-200">
          <h1 className="text-xl font-bold text-blue-600">MiniStock</h1>
        </div>
        <nav className="flex-1 p-4 space-y-1">
          {navItems.map(({ href, label, icon: Icon }) => {
            const active = pathname === href;
            return (
              <Link
                key={href}
                href={href}
                className={`flex items-center gap-3 px-4 py-3 rounded-lg font-medium text-sm transition-colors ${
                  active
                    ? "bg-blue-50 text-blue-700"
                    : "text-gray-600 hover:bg-gray-50 hover:text-gray-900"
                }`}
              >
                <Icon size={18} />
                {label}
              </Link>
            );
          })}
        </nav>
        <div className="p-4 border-t border-gray-200">
          <button
            onClick={handleLogout}
            className="flex items-center gap-3 w-full px-4 py-3 text-sm font-medium text-gray-600 hover:bg-red-50 hover:text-red-600 rounded-lg transition-colors"
          >
            <LogOut size={18} />
            Cerrar sesión
          </button>
        </div>
      </aside>

      <div className="flex-1 flex flex-col min-w-0">
        <header className="h-16 bg-white border-b border-gray-200 flex items-center justify-between px-6 shrink-0">
          <h2 className="text-xl font-semibold text-gray-800">
            {pageTitles[pathname] ?? "MiniStock"}
          </h2>
          <div className="w-8 h-8 rounded-full bg-blue-100 flex items-center justify-center text-blue-700 font-bold text-sm select-none">
            A
          </div>
        </header>
        <main className="flex-1 overflow-auto p-6">
          {children}
        </main>
      </div>
    </div>
  );
}
