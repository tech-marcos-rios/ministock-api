/**
 * Extrae un mensaje legible de un error de Axios contra la API.
 *
 * El backend responde errores como ProblemDetails (RFC 7807): `detail` para errores de
 * negocio y excepciones, o `errors` (diccionario campo → mensajes) para fallos de
 * validación de FluentValidation. Este es el único lugar que conoce ese contrato — si
 * cambia, solo hay que tocar acá.
 */
interface ApiProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

const DEFAULT_MESSAGE = "Ocurrió un error. Intentá de nuevo.";

export function getErrorMessage(err: unknown, fallback = DEFAULT_MESSAGE): string {
  const data = (err as { response?: { data?: ApiProblemDetails } })?.response?.data;
  if (!data) return fallback;

  const validationMessages = data.errors && Object.values(data.errors).flat();
  if (validationMessages && validationMessages.length > 0) {
    return validationMessages.join(" ");
  }

  return data.detail || data.title || fallback;
}
