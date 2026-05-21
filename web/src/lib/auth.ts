/**
 * Utilidades para gestión del token JWT en el cliente.
 *
 * El token se persiste en localStorage para sobrevivir recargas de página.
 * Se eligió localStorage sobre cookies httpOnly porque la API vive en un
 * dominio/puerto diferente (Hetzner), lo que complica el envío automático
 * de cookies cross-origin. El acceso desde JavaScript es necesario para
 * inyectarlo en el header Authorization de Axios.
 *
 * Trade-off de seguridad: localStorage es accesible desde JavaScript (XSS).
 * Para producción con datos sensibles se recomendaría cookies httpOnly + BFF.
 * Para este portfolio el enfoque es pragmático y adecuado al alcance.
 */

export const TOKEN_KEY = "ministock_token";

/**
 * Lee el access token JWT almacenado. Retorna `null` si no hay sesión activa.
 * El guard `typeof window === "undefined"` evita errores durante el SSR de Next.js.
 */
export function getToken(): string | null {
  if (typeof window === "undefined") return null;
  return localStorage.getItem(TOKEN_KEY);
}

/** Persiste el access token después de un login o refresh exitoso. */
export function saveToken(token: string): void {
  localStorage.setItem(TOKEN_KEY, token);
}

/** Elimina el token. Usado en logout y cuando el interceptor detecta un 401. */
export function clearToken(): void {
  localStorage.removeItem(TOKEN_KEY);
}

/**
 * Verifica si hay una sesión activa por la presencia del token.
 * No valida firma ni expiración del JWT — eso lo hace el servidor.
 * Se usa solo para decidir si redirigir a /login en el cliente.
 */
export function isAuthenticated(): boolean {
  return getToken() !== null;
}
