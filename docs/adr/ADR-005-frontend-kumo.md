# ADR-005 · Frontend con Vite + React + Kumo, en el mismo repositorio

- **Estado:** Aceptada
- **Fecha:** 2026-10-30

## Contexto

La rúbrica evalúa la API. El frontend existe solo para que la demo cuente la historia (problema → solución) en una pantalla y no en Scalar. El plan inicial usaba shadcn/ui. Antes de construirlo, el desarrollador decidió usar **Kumo** (`@cloudflare/kumo`).

## Decisión

- `web/` en la raíz del **mismo repositorio**: un solo entregable, con commits trazables de RF a API y a UI.
- **Vite + React + TypeScript + Tailwind v4 + Kumo.** Kumo es una librería npm basada en Base UI; los componentes se importan, no se copian al proyecto como en shadcn.
- **Proxy de Vite** `/api` → `http://localhost:5080`: en desarrollo no hay CORS que configurar. La API igual permite `http://localhost:5173` en Development.
- Alcance mínimo: login, resumen de la cartera (RF-07), detalle con estado de cuenta (RF-05), registrar pago (RF-04), terminar contrato (RF-06) y nuevo contrato (RF-02).
- Los errores de negocio (`ProblemDetails.title`) se muestran como toast, y los de validación debajo de cada campo.

## Consecuencias

- El frontend no tiene pruebas automatizadas: se verifica con build, lint y la demo. Es aceptable porque la rúbrica no lo puntúa; queda como mejora.
- Kumo exige un orden estricto de imports de CSS y `isolation: isolate` en la raíz: está documentado en `web/README.md`.
