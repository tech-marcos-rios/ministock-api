import { Trash2 } from "lucide-react";
import type { ReactNode } from "react";

interface ConfirmDialogProps {
  title: string;
  subtitle: string;
  message: ReactNode;
  onConfirm: () => void;
  onCancel: () => void;
  isPending: boolean;
  confirmLabel?: string;
  pendingLabel?: string;
}

/**
 * Diálogo de confirmación genérico (usado para las bajas de productos, categorías, etc.).
 * Centraliza el layout que se repetía idéntico en cada página.
 */
export function ConfirmDialog({
  title,
  subtitle,
  message,
  onConfirm,
  onCancel,
  isPending,
  confirmLabel = "Dar de baja",
  pendingLabel = "Procesando…",
}: ConfirmDialogProps) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40">
      <div className="bg-white rounded-xl shadow-xl w-full max-w-sm p-6 space-y-4">
        <div className="flex items-center gap-3">
          <div className="p-2 bg-red-50 rounded-lg">
            <Trash2 size={20} className="text-red-600" />
          </div>
          <div>
            <h3 className="font-semibold text-gray-800">{title}</h3>
            <p className="text-sm text-gray-500">{subtitle}</p>
          </div>
        </div>
        <p className="text-sm text-gray-700">{message}</p>
        <div className="flex justify-end gap-3">
          <button
            onClick={onCancel}
            className="px-4 py-2 text-sm font-medium text-gray-600 hover:bg-gray-100 rounded-lg transition-colors"
          >
            Cancelar
          </button>
          <button
            onClick={onConfirm}
            disabled={isPending}
            className="px-4 py-2 text-sm font-medium bg-red-600 text-white rounded-lg hover:bg-red-700 disabled:opacity-60 transition-colors"
          >
            {isPending ? pendingLabel : confirmLabel}
          </button>
        </div>
      </div>
    </div>
  );
}
