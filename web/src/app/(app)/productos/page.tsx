"use client";

import { useState, FormEvent } from "react";
import { Plus, Search, Pencil, Trash2, AlertTriangle, Loader2 } from "lucide-react";
import {
  useProducts,
  useCreateProduct,
  useUpdateProduct,
  useDeleteProduct,
  type Product,
  type CreateProductPayload,
  type UpdateProductPayload,
} from "@/hooks/useProducts";
import { useCategoriesAll } from "@/hooks/useCategories";
import { useDebouncedValue } from "@/hooks/useDebouncedValue";
import { getErrorMessage } from "@/lib/errors";
import { Modal } from "@/components/ui/Modal";
import { ConfirmDialog } from "@/components/ui/ConfirmDialog";
import { Pagination } from "@/components/ui/Pagination";
import { QueryError } from "@/components/ui/QueryError";

// ── Tipos internos del formulario ──────────────────────────────────────────
interface ProductFormData {
  name: string;
  sku: string;
  description: string;
  price: string;
  initialStock: string;
  minStock: string;
  categoryId: string;
}

const emptyForm: ProductFormData = {
  name: "",
  sku: "",
  description: "",
  price: "",
  initialStock: "",
  minStock: "0",
  categoryId: "",
};

function formFromProduct(p: Product): ProductFormData {
  return {
    name: p.name,
    sku: p.sku,
    description: p.description ?? "",
    price: String(p.price),
    initialStock: String(p.stock),
    minStock: String(p.minStock),
    categoryId: p.categoryId,
  };
}

// ── Componente principal ───────────────────────────────────────────────────
export default function ProductosPage() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const debouncedSearch = useDebouncedValue(search);

  // Modal state
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<Product | null>(null);
  const [form, setForm] = useState<ProductFormData>(emptyForm);
  const [formError, setFormError] = useState<string | null>(null);

  // Delete confirm
  const [deleteTarget, setDeleteTarget] = useState<Product | null>(null);

  // Resetea la página al cambiar la búsqueda durante el render (no en un Effect):
  // evita el render extra que dispara react-hooks/set-state-in-effect.
  const [prevSearch, setPrevSearch] = useState(debouncedSearch);
  if (debouncedSearch !== prevSearch) {
    setPrevSearch(debouncedSearch);
    setPage(1);
  }

  const { data, isLoading, isError, error, refetch } = useProducts(page, 15, debouncedSearch || undefined);
  const { data: categories } = useCategoriesAll();
  const createMutation = useCreateProduct();
  const updateMutation = useUpdateProduct();
  const deleteMutation = useDeleteProduct();

  // ── Handlers ────────────────────────────────────────────────────────────
  function openCreate() {
    setEditing(null);
    setForm(emptyForm);
    setFormError(null);
    setModalOpen(true);
  }

  function openEdit(p: Product) {
    setEditing(p);
    setForm(formFromProduct(p));
    setFormError(null);
    setModalOpen(true);
  }

  function closeModal() {
    setModalOpen(false);
    setEditing(null);
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setFormError(null);

    try {
      if (editing) {
        const payload: UpdateProductPayload = {
          name: form.name,
          description: form.description || undefined,
          price: parseFloat(form.price),
          minStock: parseInt(form.minStock),
          categoryId: form.categoryId,
        };
        await updateMutation.mutateAsync({ id: editing.id, payload });
      } else {
        const payload: CreateProductPayload = {
          name: form.name,
          sku: form.sku,
          description: form.description || undefined,
          price: parseFloat(form.price),
          initialStock: parseInt(form.initialStock),
          minStock: parseInt(form.minStock),
          categoryId: form.categoryId,
        };
        await createMutation.mutateAsync(payload);
      }
      closeModal();
    } catch (err: unknown) {
      setFormError(getErrorMessage(err));
    }
  }

  async function handleDelete() {
    if (!deleteTarget) return;
    try {
      await deleteMutation.mutateAsync(deleteTarget.id);
    } finally {
      setDeleteTarget(null);
    }
  }

  const isPending = createMutation.isPending || updateMutation.isPending;

  // ── Render ───────────────────────────────────────────────────────────────
  return (
    <div className="space-y-4">
      {/* Toolbar */}
      <div className="flex items-center justify-between gap-4">
        <div className="relative flex-1 max-w-xs">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
          <input
            type="text"
            placeholder="Buscar por nombre o SKU…"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="w-full pl-9 pr-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>
        <button
          onClick={openCreate}
          className="flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors"
        >
          <Plus size={16} />
          Nuevo producto
        </button>
      </div>

      {/* Table */}
      <div className="bg-white rounded-xl border border-gray-100 shadow-sm overflow-hidden">
        {isLoading ? (
          <div className="flex justify-center items-center py-16">
            <Loader2 className="w-6 h-6 animate-spin text-blue-600" />
          </div>
        ) : isError ? (
          <QueryError error={error} onRetry={() => refetch()} />
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-100 bg-gray-50">
                <th className="text-left px-4 py-3 font-medium text-gray-500">SKU</th>
                <th className="text-left px-4 py-3 font-medium text-gray-500">Nombre</th>
                <th className="text-left px-4 py-3 font-medium text-gray-500">Categoría</th>
                <th className="text-right px-4 py-3 font-medium text-gray-500">Precio</th>
                <th className="text-right px-4 py-3 font-medium text-gray-500">Stock</th>
                <th className="text-right px-4 py-3 font-medium text-gray-500">Mín.</th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-50">
              {data?.items.length === 0 && (
                <tr>
                  <td colSpan={7} className="text-center py-10 text-gray-400">
                    No se encontraron productos.
                  </td>
                </tr>
              )}
              {data?.items.map((p) => (
                <tr key={p.id} className="hover:bg-gray-50 transition-colors">
                  <td className="px-4 py-3 font-mono text-xs text-gray-500">{p.sku}</td>
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-2">
                      <span className="font-medium text-gray-800">{p.name}</span>
                      {p.isLowStock && (
                        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-amber-50 text-amber-700 border border-amber-200">
                          <AlertTriangle size={10} />
                          Stock bajo
                        </span>
                      )}
                    </div>
                    {p.description && (
                      <p className="text-xs text-gray-400 mt-0.5 truncate max-w-[200px]">
                        {p.description}
                      </p>
                    )}
                  </td>
                  <td className="px-4 py-3 text-gray-600">{p.categoryName}</td>
                  <td className="px-4 py-3 text-right font-medium text-gray-800">
                    ${p.price.toLocaleString("es-AR")}
                  </td>
                  <td className="px-4 py-3 text-right">
                    <span
                      className={`font-bold ${p.isLowStock ? "text-amber-600" : "text-gray-800"}`}
                    >
                      {p.stock}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right text-gray-500">{p.minStock}</td>
                  <td className="px-4 py-3">
                    <div className="flex items-center justify-end gap-1">
                      <button
                        onClick={() => openEdit(p)}
                        className="p-1.5 rounded-lg text-gray-400 hover:text-blue-600 hover:bg-blue-50 transition-colors"
                        title="Editar"
                      >
                        <Pencil size={15} />
                      </button>
                      <button
                        onClick={() => setDeleteTarget(p)}
                        className="p-1.5 rounded-lg text-gray-400 hover:text-red-600 hover:bg-red-50 transition-colors"
                        title="Dar de baja"
                      >
                        <Trash2 size={15} />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}

        {data && (
          <Pagination
            page={data.page}
            totalPages={data.totalPages}
            totalCount={data.totalCount}
            hasPreviousPage={data.hasPreviousPage}
            hasNextPage={data.hasNextPage}
            onPageChange={setPage}
            itemLabel="productos"
          />
        )}
      </div>

      {/* Modal crear / editar */}
      {modalOpen && (
        <Modal title={editing ? "Editar producto" : "Nuevo producto"} onClose={closeModal} maxWidth="lg">
          <form onSubmit={handleSubmit} className="p-6 space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="col-span-2">
                <label className="block text-xs font-medium text-gray-600 mb-1">
                  Nombre <span className="text-red-500">*</span>
                </label>
                <input
                  required
                  value={form.name}
                  onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>

              {!editing && (
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">
                    SKU <span className="text-red-500">*</span>
                  </label>
                  <input
                    required
                    value={form.sku}
                    onChange={(e) => setForm((f) => ({ ...f, sku: e.target.value }))}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                    placeholder="EJ-001"
                  />
                </div>
              )}

              <div className={editing ? "col-span-1" : ""}>
                <label className="block text-xs font-medium text-gray-600 mb-1">
                  Categoría <span className="text-red-500">*</span>
                </label>
                <select
                  required
                  value={form.categoryId}
                  onChange={(e) => setForm((f) => ({ ...f, categoryId: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
                >
                  <option value="">Seleccioná…</option>
                  {categories?.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.name}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">
                  Precio <span className="text-red-500">*</span>
                </label>
                <input
                  required
                  type="number"
                  min="0"
                  step="0.01"
                  value={form.price}
                  onChange={(e) => setForm((f) => ({ ...f, price: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>

              {!editing && (
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">
                    Stock inicial <span className="text-red-500">*</span>
                  </label>
                  <input
                    required
                    type="number"
                    min="0"
                    value={form.initialStock}
                    onChange={(e) => setForm((f) => ({ ...f, initialStock: e.target.value }))}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
              )}

              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">
                  Stock mínimo
                </label>
                <input
                  type="number"
                  min="0"
                  value={form.minStock}
                  onChange={(e) => setForm((f) => ({ ...f, minStock: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>

              <div className="col-span-2">
                <label className="block text-xs font-medium text-gray-600 mb-1">
                  Descripción
                </label>
                <textarea
                  rows={2}
                  value={form.description}
                  onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
                />
              </div>
            </div>

            {formError && (
              <p className="text-sm text-red-600 bg-red-50 border border-red-200 rounded-lg px-3 py-2">
                {formError}
              </p>
            )}

            <div className="flex justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={closeModal}
                className="px-4 py-2 text-sm font-medium text-gray-600 hover:bg-gray-100 rounded-lg transition-colors"
              >
                Cancelar
              </button>
              <button
                type="submit"
                disabled={isPending}
                className="px-4 py-2 text-sm font-medium bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-60 transition-colors"
              >
                {isPending ? "Guardando…" : editing ? "Guardar cambios" : "Crear producto"}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {/* Confirmar baja */}
      {deleteTarget && (
        <ConfirmDialog
          title="Dar de baja"
          subtitle="Esta acción desactiva el producto."
          message={
            <>
              ¿Dar de baja <span className="font-semibold">{deleteTarget.name}</span>?
            </>
          }
          onConfirm={handleDelete}
          onCancel={() => setDeleteTarget(null)}
          isPending={deleteMutation.isPending}
        />
      )}
    </div>
  );
}
