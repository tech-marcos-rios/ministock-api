import { AlertTriangle, RefreshCw } from "lucide-react";
import { getErrorMessage } from "@/lib/errors";

interface QueryErrorProps {
  error: unknown;
  onRetry: () => void;
}

/**
 * Estado de error para queries de lectura (GET). Sin esto, un `GET` que falla (red caída,
 * 500) deja `data` en `undefined` con `isLoading: false` — la UI mostraba "No se
 * encontraron productos", indistinguible de una lista realmente vacía. Este componente le
 * da al error su propio estado visual, con un botón que llama `refetch()` de React Query.
 */
export function QueryError({ error, onRetry }: QueryErrorProps) {
  return (
    <div className="flex flex-col items-center justify-center gap-3 py-16 text-center px-4">
      <div className="p-3 bg-red-50 rounded-full text-red-600">
        <AlertTriangle size={24} />
      </div>
      <div>
        <p className="text-sm font-medium text-gray-800">No se pudo cargar la información</p>
        <p className="text-xs text-gray-500 mt-1">{getErrorMessage(error)}</p>
      </div>
      <button
        onClick={onRetry}
        className="flex items-center gap-2 px-4 py-2 text-sm font-medium text-blue-600 bg-blue-50 rounded-lg hover:bg-blue-100 transition-colors"
      >
        <RefreshCw size={14} />
        Reintentar
      </button>
    </div>
  );
}
