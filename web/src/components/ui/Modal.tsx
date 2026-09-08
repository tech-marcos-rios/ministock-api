import { X } from "lucide-react";
import type { ReactNode } from "react";

const MAX_WIDTH = {
  sm: "max-w-sm",
  md: "max-w-md",
  lg: "max-w-lg",
} as const;

interface ModalProps {
  title: string;
  onClose: () => void;
  children: ReactNode;
  maxWidth?: keyof typeof MAX_WIDTH;
}

/**
 * Overlay + panel genérico para modales de crear/editar.
 * Centraliza el layout que se repetía en cada página (productos, categorías,
 * movimientos): fondo oscuro, header con título + botón de cerrar, panel blanco.
 */
export function Modal({ title, onClose, children, maxWidth = "md" }: ModalProps) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40">
      <div className={`bg-white rounded-xl shadow-xl w-full ${MAX_WIDTH[maxWidth]}`}>
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
          <h3 className="font-semibold text-gray-800">{title}</h3>
          <button
            onClick={onClose}
            className="p-1 rounded-lg text-gray-400 hover:text-gray-600 hover:bg-gray-100"
          >
            <X size={18} />
          </button>
        </div>
        {children}
      </div>
    </div>
  );
}
