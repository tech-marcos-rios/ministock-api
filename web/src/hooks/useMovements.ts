import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { PagedResult } from "@/hooks/useProducts";

/**
 * Tipo de movimiento. Los valores enteros coinciden con el enum `MovementType`
 * del backend para que la serialización JSON sea directa sin conversión.
 * 1 = Ingreso (Entry), 2 = Salida (Exit), 3 = Ajuste (Adjustment).
 */
export type MovementType = 1 | 2 | 3;

/** Espeja el DTO `StockMovementResponse` del backend. */
export interface StockMovement {
  id: string;
  productId: string;
  productName: string;
  productSKU: string;
  /** Delta aplicado al stock. Negativo para salidas. */
  quantity: number;
  type: MovementType;
  notes: string | null;
  createdById: string;
  createdByName: string;
  createdAt: string;
}

export interface RegisterMovementPayload {
  productId: string;
  /** Siempre positivo. El backend calcula el delta real según el tipo. */
  quantity: number;
  type: MovementType;
  notes?: string;
}

/** Obtiene el historial de movimientos paginado, opcionalmente filtrado por producto. */
export function useMovements(page = 1, pageSize = 20, productId?: string) {
  return useQuery({
    queryKey: ["movements", page, pageSize, productId],
    queryFn: async () => {
      const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
      if (productId) params.set("productId", productId);
      const { data } = await api.get<PagedResult<StockMovement>>(`/stock-movements?${params}`);
      return data;
    },
  });
}

/**
 * Mutación para registrar un movimiento de stock.
 *
 * Invalida tres cachés simultáneamente porque un movimiento afecta:
 * - La lista de movimientos (nuevo registro visible).
 * - Los productos (el stock del producto afectado cambia).
 * - El dashboard (KPIs de bajo stock y valor total pueden cambiar).
 */
export function useRegisterMovement() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: RegisterMovementPayload) =>
      api.post<StockMovement>("/stock-movements", payload).then((r) => r.data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["movements"] });
      qc.invalidateQueries({ queryKey: ["products"] });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
    },
  });
}
