import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api";

export interface Product {
  id: string;
  name: string;
  description: string | null;
  sku: string;
  price: number;
  stock: number;
  minStock: number;
  isLowStock: boolean;
  isActive: boolean;
  categoryId: string;
  categoryName: string;
  createdAt: string;
  updatedAt: string | null;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface CreateProductPayload {
  name: string;
  sku: string;
  price: number;
  initialStock: number;
  minStock: number;
  categoryId: string;
  description?: string;
}

export interface UpdateProductPayload {
  name: string;
  description?: string;
  price: number;
  minStock: number;
  categoryId: string;
}

export function useProducts(page = 1, pageSize = 20, search?: string, categoryId?: string) {
  return useQuery({
    queryKey: ["products", page, pageSize, search, categoryId],
    queryFn: async () => {
      const params = new URLSearchParams({
        page: String(page),
        pageSize: String(pageSize),
      });
      if (search) params.set("search", search);
      if (categoryId) params.set("categoryId", categoryId);
      const { data } = await api.get<PagedResult<Product>>(`/products?${params}`);
      return data;
    },
  });
}

export function useCreateProduct() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateProductPayload) =>
      api.post<Product>("/products", payload).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["products"] }),
  });
}

export function useUpdateProduct() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateProductPayload }) =>
      api.put<Product>(`/products/${id}`, payload).then((r) => r.data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["products"] });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
    },
  });
}

export function useDeleteProduct() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.delete(`/products/${id}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["products"] });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
    },
  });
}
