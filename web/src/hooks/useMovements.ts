import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { PagedResult } from "@/hooks/useProducts";

export type MovementType = 1 | 2 | 3; // 1: Ingreso, 2: Salida, 3: Ajuste

export interface StockMovement {
  id: string;
  productId: string;
  productName: string;
  productSKU: string;
  quantity: number;
  type: MovementType;
  notes: string | null;
  createdById: string;
  createdByName: string;
  createdAt: string;
}

export interface RegisterMovementPayload {
  productId: string;
  quantity: number;
  type: MovementType;
  notes?: string;
}

export function useMovements(page = 1, pageSize = 20, productId?: string) {
  return useQuery({
    queryKey: ["movements", page, pageSize, productId],
    queryFn: async () => {
      const params = new URLSearchParams({
        page: String(page),
        pageSize: String(pageSize),
      });
      if (productId) params.set("productId", productId);
      const { data } = await api.get<PagedResult<StockMovement>>(`/stock-movements?${params}`);
      return data;
    },
  });
}

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
