import axios from "axios";
import { getToken, clearToken } from "@/lib/auth";

/**
 * Instancia central de Axios configurada para comunicarse con la API de MiniStock.
 *
 * Por qué una instancia compartida en lugar de llamadas fetch directas:
 * - Centraliza la baseURL: si el endpoint cambia, solo se edita aquí.
 * - Los interceptores se aplican automáticamente a todas las llamadas,
 *   sin repetir lógica de auth o manejo de 401 en cada componente.
 *
 * La baseURL usa la variable de entorno NEXT_PUBLIC_API_URL.
 * En producción (Vercel) apunta a "/api/v1" (ruta relativa al propio dominio)
 * para que las llamadas pasen por el Route Handler proxy de Next.js, evitando
 * el bloqueo de mixed-content del browser (HTTPS → HTTP).
 * En desarrollo local apunta directamente al backend .NET.
 */
export const api = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL || "http://localhost:5197/api/v1",
  headers: {
    "Content-Type": "application/json",
  },
});

/**
 * Interceptor de request: inyecta el JWT en cada llamada autenticada.
 *
 * El token se lee de localStorage antes de cada request (no al iniciar la app)
 * para reflejar siempre el valor más reciente, incluyendo después de un refresh.
 */
api.interceptors.request.use((config) => {
  const token = getToken();
  if (token && config.headers) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

/**
 * Interceptor de response: manejo global de sesión expirada.
 *
 * Un 401 significa que el access token expiró o es inválido.
 * Se limpia el token y se redirige al login sin necesidad de
 * que cada hook/componente implemente esta lógica por separado.
 *
 * El guard `typeof window !== "undefined"` previene errores durante
 * el Server-Side Rendering de Next.js, donde `window` no existe.
 */
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401 && typeof window !== "undefined") {
      clearToken();
      window.location.replace("/login");
    }
    return Promise.reject(error);
  }
);
