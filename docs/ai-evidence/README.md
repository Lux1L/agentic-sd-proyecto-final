# Evidencia de uso de IA

Herramienta: **Claude Code** (CLI). Modelo orquestador: Claude Opus. Agentes ejecutores: Claude Sonnet.
Responsable de las decisiones y de la revisión: el desarrollador (Luis Avalos).

## Cómo se trabajó

```
Especificar ──► Planificar ──► Delegar ──► Revisar diff ──► Probar ──► Commit
 (humano + IA)   (plan mode,    (agentes     (orquestador +   (dotnet    (por RF,
                  aprobado por   Sonnet con   humano: acepta/  test)      push)
                  el humano)     contrato     corrige/rechaza)
                                 explícito)
```

1. **Spec-first.** Ninguna línea de código de negocio se escribió antes de `specs/` (commit `2670edc`). Los prompts a los agentes citan RF/RN y la trazabilidad, no ideas sueltas: así se evita el *vibe coding*.
2. **Plan mode.** El plan se discutió y aprobó antes de ejecutar ([01-plan-aprobado.md](01-plan-aprobado.md)).
3. **Orquestación con contrato.** El orquestador escribe para cada agente un brief con nombres, firmas y reglas exactas (ver [03-prompts-clave.md](03-prompts-clave.md)). Los agentes trabajan en paralelo en *git worktrees* separados, y el orquestador revisa e integra.
4. **Revisión humana.** Cada propuesta se acepta, se corrige o se rechaza, y queda registrada en [02-decisiones-y-revision.md](02-decisiones-y-revision.md).
5. **Skills del repo como guardarraíl.** `.claude/skills/` (`/add-feature`, `/add-entity`, `/add-tests`, `/vsa-review`…) fija las convenciones, para que la IA no invente estructura.

## Índice

| Archivo | Contenido |
|---|---|
| [01-plan-aprobado.md](01-plan-aprobado.md) | Plan generado en plan mode y aprobado por el humano. |
| [02-decisiones-y-revision.md](02-decisiones-y-revision.md) | Registro de propuestas de la IA: aceptadas, corregidas o rechazadas, y por qué. |
| [03-prompts-clave.md](03-prompts-clave.md) | Prompts relevantes (humano → IA y orquestador → agentes). |
| Historial de Git | Todos los commits se hicieron desde Claude Code bajo la supervisión del desarrollador. Los de los agentes Sonnet llevan `Co-Authored-By`. `git log` muestra la secuencia plantilla → bootstrap → specs → dominio → slices → pruebas. |
