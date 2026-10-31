import { useKumoToastManager } from '@cloudflare/kumo'
import { ApiError, parseValidation } from './api'

/** Devuelve un manejador: los errores de negocio van a toast; los de validación se devuelven por campo. */
export function useErrorHandler() {
  const toasts = useKumoToastManager()
  return (err: unknown): Record<string, string> => {
    const { fields, unmapped } = parseValidation(err)
    if (Object.keys(fields).length > 0 && unmapped.length === 0) return fields
    const e = err instanceof ApiError ? err : null
    const isValidation = unmapped.length > 0 || Object.keys(fields).length > 0
    toasts.add({
      title: isValidation ? 'Revisa los datos' : (e?.code ?? 'Error'),
      description: unmapped.length > 0 ? unmapped.join(' ') : (e?.detail ?? 'No se pudo completar la operación.'),
      variant: 'error',
    })
    return fields
  }
}
