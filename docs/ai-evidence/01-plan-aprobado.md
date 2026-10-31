# Proyecto Final — Arriendo Familiar (Agentic SD .NET 10, entrega 8:00 am)

## Context
Presentación final de 10–15 min + entregables (código .NET 10 compilable, repo Git con commits trazables, documento técnico, evidencias IA, PPT). Nota: Specs/trazabilidad 25% · Funcionalidad 25% · Arquitectura 20% · IA evidenciada 15% · Pruebas+docs 10% · Presentación 5%. **El frontend no puntúa** → máximo 1–1.5 h.

**Contexto país:** Perú — soles (S/), documentos DNI/CE/RUC, zona horaria America/Lima. El código va en inglés (Property/Lease/Payment); specs, docs e interfaz en español, con un glosario que relaciona los términos.

**Problema:** una familia que arrienda 2–6 unidades (departamentos, piezas, locales) lleva contratos y pagos en planillas y WhatsApp: no sabe al instante quién está atrasado, cuánto se debe ni cuándo vence cada contrato. **Valor:** un único lugar con estado de cuenta por propiedad, la mora calculada automáticamente y reglas que evitan errores (dos contratos sobre la misma unidad, pagos fuera de período).

**Decisiones tomadas:**
- Base: `Referencia/vertical-slice` (net10.0, Postgres 17, EF Core, Scalar/OpenAPI, JWT+permisos, FluentValidation, Result/Error, tests unitarios + integración Testcontainers + NetArchTest), arrancada con `/bootstrap`.
- Arquitectura: **VSA + ADR-001 justificado** (no se parte en 4 proyectos). El ADR incluye una tabla que mapea capa → ubicación: entidad/reglas/errores = Domain, Command/Handler/Validator = Application, `Database/` = Infrastructure, `Endpoint` = API. Tests de arquitectura nuevos que impiden el acoplamiento entre features.
- Frontend: `web/` con Vite + React + TS + shadcn.
- Repo: `https://github.com/Lux1L/agentic-sd-proyecto-final.git` (vacío), clonado en `ProyectoFinal/agentic-sd-proyecto-final`. Raíz = estructura de la rúbrica: `src/`, `tests/`, `specs/functional|technical/`, `docs/` (adr, ai-evidence), `docker/`, `web/`, `README.md`.
- DB: **postgres:18.6** (imagen ya descargada). Contenedor propio en `docker-compose.yml`, **no** se reutiliza el contenedor local existente (pertenece a un AppHost de Aspire de otro proyecto, con puerto y contraseña aleatorios que maneja Aspire). PG18 cambió el punto de montaje: el volumen va a `/var/lib/postgresql`, no a `/var/lib/postgresql/data`. Testcontainers (`IntegrationTestWebAppFactory`) también pasa a `postgres:18.6`.
- Limpieza de autoría: solo hay 2 menciones (`README.md:42`, `.claude/skills/README.md:54`) y no existe archivo LICENSE. Ambos README se reescriben.
- Skills (`.claude/skills/**`, ~1.000 líneas) y `CLAUDE.md` se traducen al español: solo la prosa; código, identificadores y comandos quedan igual. Lo hace un subagente en segundo plano mientras se escriben las specs.

## Alcance (deliberadamente acotado)
**Usuarios:** solo el administrador familiar (Users/auth del template). El portal del arrendatario queda como mejora pendiente.

**Entidades (3):**
- `Property` — nombre, dirección, tipo (Depto/Casa/Pieza/Local), estado (Disponible/Arrendada/Inactiva).
- `Lease` (contrato) — PropertyId, datos del arrendatario embebidos (nombre, tipo + número de documento, teléfono, email), fecha inicio/fin, monto mensual en soles (S/, PEN), garantía, día de pago (1–28), estado (Activo/Terminado).
- `Payment` — LeaseId, período (año-mes), monto, fecha de pago, medio.

**RF y reglas de negocio:**
- RF-01 CRUD de propiedades. Regla: no se elimina/inactiva una propiedad con contrato activo.
- RF-02 Crear contrato. Reglas: la propiedad debe estar Disponible (no Inactiva); no puede haber otro contrato activo que se solape en fechas; fin > inicio y duración ≥ 1 mes; monto > 0; garantía ≤ 2 meses de arriendo (regla del negocio familiar, configurable; no es una exigencia legal); día de pago 1–28; documento del arrendatario válido según su tipo: **DNI** = 8 dígitos, **CE** (carné de extranjería) = 9 a 12 caracteres alfanuméricos, **RUC** = 11 dígitos, que empiece con 10/15/17/20 y con dígito verificador módulo 11. Al crearse, la propiedad pasa a Arrendada.
- RF-03 Listar/ver contratos (paginado, filtro por estado/propiedad).
- RF-04 Registrar pago. Reglas: el período debe estar dentro del contrato; el contrato debe estar activo; el monto acumulado del período no supera el arriendo (se permiten abonos parciales); monto > 0.
- RF-05 Estado de cuenta del contrato: por período → Pagado / Parcial / Atrasado (hoy > día de pago y saldo > 0) / Pendiente, con días de atraso y deuda total. Se calcula al vuelo con `IDateTimeProvider` (testeable, sin jobs). "Hoy" es la fecha en hora de Lima (`America/Lima`, UTC-5), no en UTC: si no, un pago registrado de noche cambia de día.
- RF-06 Terminar contrato. Regla: no se termina con deuda pendiente; la propiedad vuelve a Disponible.
- RF-07 Resumen: listado de propiedades con arrendatario actual, estado del mes y deuda (lo que se ve en la pantalla de la demo).

**Fuera de alcance (va a "mejoras pendientes" en la presentación):** reajuste anual IPC/UF, recordatorios por email/WhatsApp (hay `IEmailSender` como punto de extensión), portal del arrendatario, gastos comunes/servicios, adjuntar contrato PDF.

**Decisiones adicionales:**
- Los pagos viven dentro de `Features/Leases/` (un pago no existe sin contrato; así no hay acoplamiento entre features).
- Datos de prueba solo en Development: un usuario admin más 3 propiedades (al día / abono parcial / atrasada) con contratos que empiezan en el pasado, para que la demo muestre la mora apenas abre. Las credenciales van en el README.
- CORS en la API para el origen de Vite (`http://localhost:5173`), solo en Development.
- Git: commit + push a `origin/main` por cada RF o bloque, con el prefijo del RF.
- Deck: Slides artifact (6 secciones del índice del instructor) + export .pptx/PDF como entregable.

## Horario
23:30–05:00 construir (pasos 1–6) · **05:00 código congelado** · 05:00–07:00 deck, ensayo y video de respaldo de la demo · 07:00–08:00 colchón.

## Ejecución (~6 h)
1. **Setup (30 min):** copiar la referencia a `ProyectoFinal/arriendo-familiar` (sin bin/obj/.DS_Store), `git init`, `/bootstrap` con nombre `ArriendoFamiliar` (namespace `Web.Api` se mantiene para no romper los skills), ajustar el nombre de la DB/Jwt/compose, dejar build + test en verde. Commit.
2. **Specs primero (60 min):** `specs/functional/` con RF-01..07, HU con criterios Gherkin y casos de uso; `specs/technical/` con modelo de datos, contrato de endpoints y **matriz de trazabilidad RF → HU → CU → endpoint → test**. `docs/adr/`: ADR-001 (VSA vs Clean), ADR-002 (Postgres + EF Core), ADR-003 (Result en vez de excepciones), ADR-004 (estado de cuenta calculado vs persistido). Commit por bloque.
3. **Backend (2 h):** features `Properties/`, `Leases/` y `Payments/` (o pagos dentro de `Leases/`) con `/add-entity`, `/add-feature` y `/add-migration`, y un permiso para el admin. Commits con prefijo del RF (`feat(RF-04): ...`).
4. **Tests (60 min):** `/add-tests` — unitarios por cada regla (solapamiento, garantía, período fuera de contrato, sobrepago, cálculo de mora con fecha fija y borde de medianoche en Lima, terminar con deuda, DNI/CE/RUC válidos e inválidos incluyendo un RUC con dígito verificador incorrecto), integración por endpoint, ArchitectureTests nuevos. Cada test nombrado con su RF para la matriz.
5. **Frontend (≤75 min):** login, resumen de propiedades (RF-07), detalle de contrato con estado de cuenta, diálogo para registrar un pago, formulario para crear contrato. Nada más.
6. **Evidencia IA (continua):** `docs/ai-evidence/` con prompts clave, extractos de respuestas, diffs y una tabla aceptado/rechazado/corregido + por qué. Incluye este plan y las correcciones humanas a la IA: Help Desk → arriendo familiar, Clean → VSA justificado, RUT chileno → DNI/CE/RUC de Perú, contenedor de Aspire → contenedor propio y la salida de `/vsa-review` con lo que se aplicó o descartó.
7. **README + documento técnico + presentación (60 min):** README (stack, `docker compose up`, credenciales de prueba, enlace a evidencias). Deck de 6 secciones siguiendo el índice del instructor.

## Verificación
- `dotnet build ArriendoFamiliar.slnx` sin warnings (TreatWarningsAsErrors) y `dotnet test` en verde (Docker corriendo).
- `docker compose up -d` → API arriba; Scalar en `/scalar` muestra todos los endpoints.
- Guion de demo end-to-end: login → crear propiedad → crear contrato → intentar un segundo contrato solapado (409) → registrar un pago parcial → ver estado de cuenta con mora → intentar terminar con deuda (rechazado) → pagar saldo → terminar. Lo mismo desde el frontend.
- Checklist de la rúbrica (pág. 13) marcado punto por punto; sin secretos en el repo.
