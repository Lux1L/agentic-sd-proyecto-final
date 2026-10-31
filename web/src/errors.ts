import { useKumoToastManager } from '@cloudflare/kumo'
import { ApiError, parseValidation } from './api'

/**
 * Devuelve un manejador de errores. Los de negocio (title/detail) van a un toast;
 * los de validación se devuelven por campo (clave en minúsculas) y las claves que no
 * corresponden a un campo del formulario (`known`) también van al toast.
 */
export function useErrorHandler() {
  const toasts = useKumoToastManager()
  return (err: unknown, known: string[] = []): Record<string, string> => {
    const { fields, unmapped } = parseValidation(err, known)
    const e = err instanceof ApiError ? err : null
    const hasValidation = !!e && Object.keys(e.errors ?? {}).length > 0
    if (hasValidation && unmapped.length === 0) return fields
    toasts.add({
      title: hasValidation ? 'Revisa los datos' : (e?.code ?? 'Error'),
      description: hasValidation ? unmapped.join(' ') : (e?.detail ?? 'No se pudo completar la operación.'),
      variant: 'error',
    })
    return fields
  }
}
