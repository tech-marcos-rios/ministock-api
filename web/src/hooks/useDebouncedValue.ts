import { useEffect, useState } from "react";

/** Devuelve `value` recién después de `delayMs` sin cambios. Usado para debouncear búsquedas. */
export function useDebouncedValue<T>(value: T, delayMs = 400): T {
  const [debounced, setDebounced] = useState(value);

  useEffect(() => {
    const timeout = setTimeout(() => setDebounced(value), delayMs);
    return () => clearTimeout(timeout);
  }, [value, delayMs]);

  return debounced;
}
