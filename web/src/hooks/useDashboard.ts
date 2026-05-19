import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api";

export interface DashboardSummary {
  totalProducts: number;
  totalCategories: number;
  lowStockProducts: number;
  totalInventoryValue: number;
}

export interface StockByCategory {
  categoryId: string;
  categoryName: string;
  totalStock: number;
}

export interface StockMovement {
  id: string;
  productId: string;
  productName: string;
  productSKU: string;
  quantity: number;
  type: number; // 1: Entry, 2: Exit, 3: Adjustment
  notes: string;
  createdById: string;
  createdByName: string;
  createdAt: string;
}

export function useDashboardSummary() {
  return useQuery({
    queryKey: ["dashboard", "summary"],
    queryFn: async () => {
      const { data } = await api.get<DashboardSummary>("/dashboard/summary");
      return data;
    },
  });
}

export function useStockByCategory() {
  return useQuery({
    queryKey: ["dashboard", "stock-by-category"],
    queryFn: async () => {
      const { data } = await api.get<StockByCategory[]>("/dashboard/stock-by-category");
      return data;
    },
  });
}

export function useRecentMovements(count = 10) {
  return useQuery({
    queryKey: ["dashboard", "recent-movements", count],
    queryFn: async () => {
      const { data } = await api.get<StockMovement[]>(`/dashboard/recent-movements?count=${count}`);
      return data;
    },
  });
}
