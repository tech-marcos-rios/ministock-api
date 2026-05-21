import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api";

/**
 * Tipado del producto tal como lo devuelve la API.
 * Espeja el DTO `ProductResponse` del backend para garantizar consistencia.
 */
export interface Product {
  id: string;
  name: string;
  description: string | null;
  sku: string;
  price: number;
  stock: number;
  minStock: number;
  isLowStock: boolean; // Calculado en el backend: stock <= minStock
  isActive: boolean;
  categoryId: string;
  categoryName: string;
  createdAt: string;
  updatedAt: string | null;
}

/** Tipado genérico de respuesta paginada. Espeja `PagedResult<T>` del backend. */
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

/** Payload para crear un producto. SKU e initialStock solo se envían en creación. */
export interface CreateProductPayload {
  name: string;
  sku: string;
  price: number;
  initialStock: number;
  minStock: number;
  categoryId: string;
  description?: string;
}

/** Payload para actualizar. Sin SKU (inmutable) ni initialStock (se gestiona con movimientos). */
export interface UpdateProductPayload {
  name: string;
  description?: string;
  price: number;
  minStock: number;
  categoryId: string;
}

/**
 * Obtiene productos paginados con búsqueda y filtro por categoría.
 *
 * La queryKey incluye todos los parámetros: Tanstack Query reejecutará
 * la query automáticamente cada vez que cambie alguno (página, búsqueda, etc.),
 * sin necesidad de gestionar ese efecto manualmente.
 */
export function useProducts(page = 1, pageSize = 20, search?: string, categoryId?: string) {
  return useQuery({
    queryKey: ["products", page, pageSize, search, categoryId],
    queryFn: async () => {
      const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
      if (search)     params.set("search",     search);
      if (categoryId) params.set("categoryId", categoryId);
      const { data } = await api.get<PagedResult<Product>>(`/products?${params}`);
      return data;
    },
  });
}

/**
 * Mutación para crear un producto.
 * `invalidateQueries(["products"])` fuerza a Tanstack Query a recargar
 * la lista desde el servidor, garantizando que el nuevo producto aparezca
 * sin necesidad de gestionar el estado local manualmente.
 */
export function useCreateProduct() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateProductPayload) =>
      api.post<Product>("/products", payload).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["products"] }),
  });
}

/**
 * Mutación para actualizar un producto.
 * Invalida también el dashboard porque los KPIs (valor total, bajo stock)
 * pueden cambiar al editar precio o stock mínimo.
 */
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

/** Mutación para dar de baja un producto (soft delete en el backend). */
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
