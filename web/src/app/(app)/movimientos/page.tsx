"use client";

import { useState, FormEvent } from "react";
import { Plus, X, ChevronLeft, ChevronRight, Loader2, ArrowDownCircle, ArrowUpCircle, RefreshCw } from "lucide-react";
import { useMovements, useRegisterMovement, type MovementType } from "@/hooks/useMovements";
import { useCategoriesAll } from "@/hooks/useCategories";
import { useProducts } from "@/hooks/useProducts";
import { formatDistanceToNow } from "date-fns";
import { es } from "date-fns/locale";

const MOVEMENT_LABELS: Record<MovementType, string> = {
  1: "Ingreso",
  2: "Salida",
  3: "Ajuste",
};

const MOVEMENT_STYLES: Record<MovementType, string> = {
  1: "bg-green-50 text-green-700 border-green-200",
  2: "bg-red-50 text-red-700 border-red-200",
  3: "bg-blue-50 text-blue-700 border-blue-200",
};

const MOVEMENT_ICONS: Record<MovementType, React.ReactNode> = {
  1: <ArrowDownCircle size={14} />,
  2: <ArrowUpCircle size={14} />,
  3: <RefreshCw size={14} />,
};

interface FormData {
  productId: string;
  type: MovementType;
  quantity: string;
  notes: string;
}

const emptyForm: FormData = {
  productId: "",
  type: 1,
  quantity: "",
  notes: "",
};

export default function MovimientosPage() {
  const [page, setPage] = useState(1);
  const [modalOpen, setModalOpen] = useState(false);
  const [form, setForm] = useState<FormData>(emptyForm);
  const [formError, setFormError] = useState<string | null>(null);

  const { data, isLoading } = useMovements(page, 20);
  const { data: allProducts } = useProducts(1, 100);
  const registerMutation = useRegisterMovement();

  // categoriesAll no se usa en esta página pero sí en el dropdown de productos
  // para mostrar la categoría — en este caso usamos directamente la lista de productos
  useCategoriesAll(); // precarga el cache para productos page

  function openModal() {
    setForm(emptyForm);
    setFormError(null);
    setModalOpen(true);
  }

  function closeModal() {
    setModalOpen(false);
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setFormError(null);
    try {
      await registerMutation.mutateAsync({
        productId: form.productId,
        quantity: parseInt(form.quantity),
        type: form.type,
        notes: form.notes || undefined,
      });
      closeModal();
    } catch (err: unknown) {
      const msg =
        (err as { response?: { data?: { error?: string } } })?.response?.data?.error ??
        "Ocurrió un error. Intentá de nuevo.";
      setFormError(msg);
    }
  }

  return (
    <div className="space-y-4">
      {/* Toolbar */}
      <div className="flex items-center justify-end">
        <button
          onClick={openModal}
          className="flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors"
        >
          <Plus size={16} />
          Registrar movimiento
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
                <th className="text-left px-4 py-3 font-medium text-gray-500">Producto</th>
                <th className="text-left px-4 py-3 font-medium text-gray-500">Tipo</th>
                <th className="text-right px-4 py-3 font-medium text-gray-500">Cantidad</th>
                <th className="text-left px-4 py-3 font-medium text-gray-500">Notas</th>
                <th className="text-left px-4 py-3 font-medium text-gray-500">Usuario</th>
                <th className="text-left px-4 py-3 font-medium text-gray-500">Fecha</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-50">
              {data?.items.length === 0 && (
                <tr>
                  <td colSpan={6} className="text-center py-10 text-gray-400">
                    No hay movimientos registrados.
                  </td>
                </tr>
              )}
              {data?.items.map((m) => (
                <tr key={m.id} className="hover:bg-gray-50 transition-colors">
                  <td className="px-4 py-3">
                    <p className="font-medium text-gray-800">{m.productName}</p>
                    <p className="text-xs text-gray-400 font-mono">{m.productSKU}</p>
                  </td>
                  <td className="px-4 py-3">
                    <span
                      className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium border ${MOVEMENT_STYLES[m.type]}`}
                    >
                      {MOVEMENT_ICONS[m.type]}
                      {MOVEMENT_LABELS[m.type]}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right">
                    <span
                      className={`font-bold ${
                        m.type === 1
                          ? "text-green-600"
                          : m.type === 2
                          ? "text-red-600"
                          : "text-blue-600"
                      }`}
                    >
                      {m.type === 1 ? "+" : m.type === 2 ? "−" : "±"}
                      {Math.abs(m.quantity)}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-gray-500 max-w-[200px] truncate">
                    {m.notes ?? <span className="text-gray-300 italic">—</span>}
                  </td>
                  <td className="px-4 py-3 text-gray-600">{m.createdByName}</td>
                  <td className="px-4 py-3 text-gray-400 text-xs whitespace-nowrap">
                    {formatDistanceToNow(new Date(m.createdAt), { addSuffix: true, locale: es })}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}

        {data && data.totalPages > 1 && (
          <div className="flex items-center justify-between px-4 py-3 border-t border-gray-100">
            <span className="text-xs text-gray-500">
              {data.totalCount} movimientos · página {data.page} de {data.totalPages}
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

      {/* Modal registrar movimiento */}
      {modalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40">
          <div className="bg-white rounded-xl shadow-xl w-full max-w-md">
            <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
              <h3 className="font-semibold text-gray-800">Registrar movimiento</h3>
              <button
                onClick={closeModal}
                className="p-1 rounded-lg text-gray-400 hover:text-gray-600 hover:bg-gray-100"
              >
                <X size={18} />
              </button>
            </div>

            <form onSubmit={handleSubmit} className="p-6 space-y-4">
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">
                  Producto <span className="text-red-500">*</span>
                </label>
                <select
                  required
                  value={form.productId}
                  onChange={(e) => setForm((f) => ({ ...f, productId: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
                >
                  <option value="">Seleccioná un producto…</option>
                  {allProducts?.items.map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.name} ({p.sku}) — stock: {p.stock}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">
                  Tipo <span className="text-red-500">*</span>
                </label>
                <div className="grid grid-cols-3 gap-2">
                  {([1, 2, 3] as MovementType[]).map((t) => (
                    <button
                      key={t}
                      type="button"
                      onClick={() => setForm((f) => ({ ...f, type: t }))}
                      className={`py-2 rounded-lg text-sm font-medium border transition-colors ${
                        form.type === t
                          ? MOVEMENT_STYLES[t] + " border"
                          : "border-gray-200 text-gray-600 hover:bg-gray-50"
                      }`}
                    >
                      {MOVEMENT_LABELS[t]}
                    </button>
                  ))}
                </div>
              </div>

              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">
                  Cantidad <span className="text-red-500">*</span>
                </label>
                <input
                  required
                  type="number"
                  min="1"
                  value={form.quantity}
                  onChange={(e) => setForm((f) => ({ ...f, quantity: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="0"
                />
                {form.type === 3 && (
                  <p className="text-xs text-blue-600 mt-1">
                    En ajuste, ingresá el valor absoluto. Si hay pérdida el sistema descuenta.
                  </p>
                )}
              </div>

              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Notas</label>
                <textarea
                  rows={2}
                  value={form.notes}
                  onChange={(e) => setForm((f) => ({ ...f, notes: e.target.value }))}
                  placeholder="Motivo del movimiento…"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
                />
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
                  disabled={registerMutation.isPending}
                  className="px-4 py-2 text-sm font-medium bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-60 transition-colors"
                >
                  {registerMutation.isPending ? "Registrando…" : "Registrar"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
