"use client";

import { useState, useEffect, FormEvent } from "react";
import { Plus, Search, Pencil, Trash2, AlertTriangle, X, ChevronLeft, ChevronRight, Loader2 } from "lucide-react";
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
  const [debouncedSearch, setDebouncedSearch] = useState("");

  // Modal state
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<Product | null>(null);
  const [form, setForm] = useState<ProductFormData>(emptyForm);
  const [formError, setFormError] = useState<string | null>(null);

  // Delete confirm
  const [deleteTarget, setDeleteTarget] = useState<Product | null>(null);

  // Debounce search
  useEffect(() => {
    const t = setTimeout(() => {
      setDebouncedSearch(search);
      setPage(1);
    }, 400);
    return () => clearTimeout(t);
  }, [search]);

  const { data, isLoading } = useProducts(page, 15, debouncedSearch || undefined);
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
      const msg =
        (err as { response?: { data?: { error?: string } } })?.response?.data?.error ??
        "Ocurrió un error. Intentá de nuevo.";
      setFormError(msg);
    }
  }

  async function handleDelete() {
    if (!deleteTarget) return;
    try {
      await deleteMutation.mutateAsync(deleteTarget.id);
      setDeleteTarget(null);
    } catch {
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

        {/* Paginación */}
        {data && data.totalPages > 1 && (
          <div className="flex items-center justify-between px-4 py-3 border-t border-gray-100">
            <span className="text-xs text-gray-500">
              {data.totalCount} productos · página {data.page} de {data.totalPages}
            </span>
            <div className="flex gap-1">
              <button
                onClick={() => setPage((p) => p - 1)}
                disabled={!data.hasPreviousPage}
                className="p-1.5 rounded-lg text-gray-500 hover:bg-gray-100 disabled:opacity-30 disabled:cursor-not-allowed"
              >
                <ChevronLeft size={16} />
              </button>
              <button
                onClick={() => setPage((p) => p + 1)}
                disabled={!data.hasNextPage}
                className="p-1.5 rounded-lg text-gray-500 hover:bg-gray-100 disabled:opacity-30 disabled:cursor-not-allowed"
              >
                <ChevronRight size={16} />
              </button>
            </div>
          </div>
        )}
      </div>

      {/* ── Modal crear / editar ──────────────────────────────────────────── */}
      {modalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40">
          <div className="bg-white rounded-xl shadow-xl w-full max-w-lg">
            <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
              <h3 className="font-semibold text-gray-800">
                {editing ? "Editar producto" : "Nuevo producto"}
              </h3>
              <button
                onClick={closeModal}
                className="p-1 rounded-lg text-gray-400 hover:text-gray-600 hover:bg-gray-100"
              >
                <X size={18} />
              </button>
            </div>

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
          </div>
        </div>
      )}

      {/* ── Modal confirmar baja ──────────────────────────────────────────── */}
      {deleteTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40">
          <div className="bg-white rounded-xl shadow-xl w-full max-w-sm p-6 space-y-4">
            <div className="flex items-center gap-3">
              <div className="p-2 bg-red-50 rounded-lg">
                <Trash2 size={20} className="text-red-600" />
              </div>
              <div>
                <h3 className="font-semibold text-gray-800">Dar de baja</h3>
                <p className="text-sm text-gray-500">Esta acción desactiva el producto.</p>
              </div>
            </div>
            <p className="text-sm text-gray-700">
              ¿Dar de baja{" "}
              <span className="font-semibold">{deleteTarget.name}</span>?
            </p>
            <div className="flex justify-end gap-3">
              <button
                onClick={() => setDeleteTarget(null)}
                className="px-4 py-2 text-sm font-medium text-gray-600 hover:bg-gray-100 rounded-lg transition-colors"
              >
                Cancelar
              </button>
              <button
                onClick={handleDelete}
                disabled={deleteMutation.isPending}
                className="px-4 py-2 text-sm font-medium bg-red-600 text-white rounded-lg hover:bg-red-700 disabled:opacity-60 transition-colors"
              >
                {deleteMutation.isPending ? "Procesando…" : "Dar de baja"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
