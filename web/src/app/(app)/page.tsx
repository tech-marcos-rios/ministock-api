"use client";

import { Package, Tags, AlertTriangle, DollarSign, Loader2 } from "lucide-react";
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from "recharts";
import { useDashboardSummary, useStockByCategory, useRecentMovements } from "@/hooks/useDashboard";
import { formatDistanceToNow } from "date-fns";
import { es } from "date-fns/locale";

export default function DashboardPage() {
  const { data: summary, isLoading: isLoadingSummary } = useDashboardSummary();
  const { data: stockByCategory, isLoading: isLoadingStock } = useStockByCategory();
  const { data: recentMovements, isLoading: isLoadingMovements } = useRecentMovements(5);

  if (isLoadingSummary || isLoadingStock || isLoadingMovements) {
    return (
      <div className="h-full flex items-center justify-center">
        <Loader2 className="w-8 h-8 animate-spin text-blue-600" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* KPI Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <div className="bg-white p-6 rounded-xl border border-gray-100 shadow-sm flex items-center space-x-4 hover:shadow-md transition-shadow">
          <div className="p-3 bg-blue-50 text-blue-600 rounded-lg">
            <Package size={24} />
          </div>
          <div>
            <p className="text-sm font-medium text-gray-500">Total Productos</p>
            <h3 className="text-2xl font-bold text-gray-900">{summary?.totalProducts || 0}</h3>
          </div>
        </div>

        <div className="bg-white p-6 rounded-xl border border-gray-100 shadow-sm flex items-center space-x-4 hover:shadow-md transition-shadow">
          <div className="p-3 bg-purple-50 text-purple-600 rounded-lg">
            <Tags size={24} />
          </div>
          <div>
            <p className="text-sm font-medium text-gray-500">Categorías</p>
            <h3 className="text-2xl font-bold text-gray-900">{summary?.totalCategories || 0}</h3>
          </div>
        </div>

        <div className="bg-white p-6 rounded-xl border border-gray-100 shadow-sm flex items-center space-x-4 hover:shadow-md transition-shadow">
          <div className="p-3 bg-amber-50 text-amber-600 rounded-lg">
            <AlertTriangle size={24} />
          </div>
          <div>
            <p className="text-sm font-medium text-gray-500">Stock Bajo</p>
            <h3 className="text-2xl font-bold text-gray-900">{summary?.lowStockProducts || 0}</h3>
          </div>
        </div>

        <div className="bg-white p-6 rounded-xl border border-gray-100 shadow-sm flex items-center space-x-4 hover:shadow-md transition-shadow">
          <div className="p-3 bg-green-50 text-green-600 rounded-lg">
            <DollarSign size={24} />
          </div>
          <div>
            <p className="text-sm font-medium text-gray-500">Valor Inventario</p>
            <h3 className="text-2xl font-bold text-gray-900">
              ${summary?.totalInventoryValue?.toLocaleString("es-AR") || 0}
            </h3>
          </div>
        </div>
      </div>

      {/* Charts & Lists Area */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2 bg-white p-6 rounded-xl border border-gray-100 shadow-sm">
          <h3 className="text-lg font-bold text-gray-800 mb-6">Stock por Categoría</h3>
          <div className="h-[300px] w-full">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={stockByCategory || []}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#f0f0f0" />
                <XAxis
                  dataKey="categoryName"
                  axisLine={false}
                  tickLine={false}
                  tick={{ fill: "#6b7280" }}
                />
                <YAxis axisLine={false} tickLine={false} tick={{ fill: "#6b7280" }} />
                <Tooltip
                  cursor={{ fill: "#f3f4f6" }}
                  contentStyle={{
                    borderRadius: "8px",
                    border: "none",
                    boxShadow: "0 4px 6px -1px rgb(0 0 0 / 0.1)",
                  }}
                />
                <Bar dataKey="totalStock" fill="#3b82f6" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>

        <div className="bg-white p-6 rounded-xl border border-gray-100 shadow-sm">
          <h3 className="text-lg font-bold text-gray-800 mb-4">Movimientos Recientes</h3>
          <div className="space-y-4">
            {recentMovements?.map((mov) => (
              <div
                key={mov.id}
                className="flex items-center justify-between p-3 bg-gray-50 rounded-lg hover:bg-gray-100 transition-colors"
              >
                <div>
                  <p className="text-sm font-semibold text-gray-800">{mov.productName}</p>
                  <p className="text-xs text-gray-500 capitalize">
                    {formatDistanceToNow(new Date(mov.createdAt), {
                      addSuffix: true,
                      locale: es,
                    })}{" "}
                    •{" "}
                    {mov.type === 1
                      ? "Ingreso"
                      : mov.type === 2
                      ? "Salida"
                      : "Ajuste"}
                  </p>
                </div>
                <span
                  className={`text-sm font-bold ${
                    mov.quantity > 0 ? "text-green-600" : "text-red-600"
                  }`}
                >
                  {mov.quantity > 0 ? `+${mov.quantity}` : mov.quantity}
                </span>
              </div>
            ))}
            {(!recentMovements || recentMovements.length === 0) && (
              <p className="text-sm text-gray-500 text-center py-4">
                No hay movimientos recientes.
              </p>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
