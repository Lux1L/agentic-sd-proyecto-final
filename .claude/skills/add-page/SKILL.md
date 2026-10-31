---
name: add-page
description: Agrega una pantalla, ruta, formulario o dialog nuevo al frontend React + Kumo (web/) siguiendo lo que ya existe: página lazy, tipos de la API, errores, fechas en hora de Lima y componentes de Kumo. Úsala cuando el usuario pida una página, vista, ruta, formulario o dialog nuevo en web/.
argument-hint: <descripción de la pantalla, p. ej. "historial de pagos de un contrato" o "dialog para editar el teléfono">
---

# Agregar una página al frontend

Frontend en `web/` (React, react-router-dom, `@cloudflare/kumo`, pnpm). Cada regla de abajo previene un defecto real que ya encontramos (códigos E*/I* en `docs/ai-evidence/02-decisiones-y-revision.md`). Para reglas visuales usa la skill `kumo-design`; para las props de un componente, `npx @cloudflare/kumo doc <Componente>`. Nunca inventes props.

## Flujo

1. **Contrato primero.** Lee el endpoint en `specs/technical/02-contrato-api.md` y pruébalo con `curl` (con `Authorization: Bearer`) antes de tipar. Agrega las interfaces en `web/src/api.ts` con la forma real de la respuesta (fechas como `string` ISO `YYYY-MM-DD`, períodos `YYYY-MM`, enums como unión de literales).
2. **Página** en `web/src/pages/<Nombre>.tsx`, `export default function <Nombre>()`. Modelo de lectura: `Summary.tsx`; detalle con dialogs: `LeaseDetail.tsx`; formulario: `NewLease.tsx`; **listado** (Table + Pagination + filtros con debounce): `Leases.tsx`.
3. **Ruta lazy** en `web/src/App.tsx`, dentro del `<Route element={<Layout />}>` (exige sesión) salvo que sea pública:
   ```tsx
   const Nombre = lazy(() => import('./pages/Nombre'))
   <Route path="/nombre/:id" element={<Nombre />} />
   ```
4. **Enlace de navegación**: `Layout.tsx` tiene el logo, los enlaces "Resumen" y "Contratos" y "Cerrar sesión". Si la pantalla es navegable desde el encabezado, agrega ahí otro `Link` de Kumo junto a esos; si se llega desde otra página, usa `navigate()` o `Link`/`Breadcrumbs` allí.
5. **Pruebas y cierre**: sigue el checklist del final.

## Datos y errores

- Toda llamada pasa por `api<T>(path, { method, body })` (`api.ts`): pone el token, el prefijo `/api` y redirige a `/login` en 401. Nunca `fetch` directo.
- Carga inicial: `useEffect` + `api<T>(...).then(setData).catch((e) => handle(e))`; mientras `!data`, `<Loader size={32} />` centrado.
- Errores con `const handle = useErrorHandler()` (`errors.ts`):
  - **Negocio** (ProblemDetails `title` + `detail`): `handle(e)` los muestra como toast. Úsalo en lecturas y acciones sin formulario.
  - **Validación** (`errors: { campo: [..] }`): `setErrors(handle(e, knownFields))` devuelve `{ campo: mensaje }` con **clave en minúsculas**; pásalo a cada campo (`errors.tenantphone`, `errors.paidon`). Claves no conocidas van al toast "Revisa los datos".
  - Éxito: `useKumoToastManager().add({ title, description, variant: 'success' })`.
- **El campo viene en la clave del error, nunca se infiere del texto del mensaje** (I12: la API ya agrupa por campo en camelCase; si falta, es un defecto del backend, no del frontend).
- Formularios: `noValidate`, estado `busy` en `<Button loading={busy}>`, `setErrors({})` antes de enviar, y valida en cliente lo que no se puede serializar (fechas vacías, I14).

## Fechas y dinero (regla dura, E1)

"Hoy" es **hora de Lima** (la API valida con `America/Lima`, RN-18). Todo está en `web/src/format.ts`:

| Necesito | Usa |
|---|---|
| fecha de hoy / período actual | `todayIso()` / `currentPeriod()` |
| ISO ↔ `Date` (para DatePicker) | `isoToDate` / `dateToIso` |
| meses de un contrato / mes por defecto | `periodsBetween(start, end)` / `defaultPeriod(lista)` |
| mostrar período | `periodLabel` ("octubre de 2026") o `formatPeriod` ("10/2026") |
| mostrar fecha | `formatDate` (`dd/mm/aaaa`, "—" si falta) |
| mostrar monto | `formatMoney` (soles) |
| etiquetas de enums | `propertyTypeLabel`, `docTypeLabel` |

Nunca `new Date()` para fechas de negocio ni `toISOString()` (corre el día según la zona; los tests unitarios no lo detectan, por eso existe `web/e2e/fecha-lima.spec.ts` con `timezoneId: Asia/Tokyo`).

## Necesito X → componente Kumo

Todo se importa de `@cloudflare/kumo`. Los ejemplos son copia del código real; ver [references/ejemplos.md](references/ejemplos.md) para formulario, Select, InputGroup y dialogs completos.

| Necesito | Componente | Ejemplo mínimo |
|---|---|---|
| Título de página | `Text` | `<Text as="h1" variant="heading" size="lg">Resumen</Text>` |
| Texto secundario | `Text` | `<Text variant="secondary">{label}</Text>` |
| Navegación | `Link`, `Breadcrumbs` | `<Breadcrumbs><Breadcrumbs.Link href="/">Resumen</Breadcrumbs.Link><Breadcrumbs.Separator /><Breadcrumbs.Current>{name}</Breadcrumbs.Current></Breadcrumbs>` (el `LinkProvider` con `AppLink` ya está en `main.tsx`: `href` navega sin recargar) |
| Layout en columnas | `Grid` | `<Grid variant="2up" gap="base">` (también `3up`, `1-2-4up`) |
| Tarjeta | `LayerCard` (+ `.Secondary` cabecera, `.Primary` cuerpo), sin anidar | `<LayerCard><LayerCard.Secondary>Contrato</LayerCard.Secondary><LayerCard.Primary>…</LayerCard.Primary></LayerCard>` |
| KPI | `Surface` | `<Surface className="rounded-lg px-5 py-4"><Text variant="secondary">…</Text>…</Surface>` |
| Fecha | `DateField` (`components/`) | `<DateField label="Inicio" value={f.startDate} onChange={(v) => setF({ ...f, startDate: v })} error={errors.startdate} />` |
| Opciones cerradas | `Select` **con `items`** | `<Select label="Día de pago" hideLabel={false} className="w-full" items={dayItems} value={v} onValueChange={(x) => set(x as string)}>` + `Select.Option` |
| Monto | `InputGroup` con `InputGroup.Addon` "S/" | `<InputGroup label="Monto"><InputGroup.Addon>S/</InputGroup.Addon><InputGroup.Input type="number" min="0" step="0.01" …/></InputGroup>` |
| Texto libre | `Input` | `<Input label="Celular" value={v} onChange={…} error={…} />` |
| Aviso persistente | `Banner` | `<Banner variant="error" icon={<WarningIcon weight="fill" />} title="…" />` |
| Confirmación destructiva | `Dialog.Root role="alertdialog"` | siempre montado, controlado con `open={endOpen} onOpenChange={setEndOpen}` |
| Dialog con formulario | `Dialog.Root` + `Dialog` | igual, sin `role`; `Dialog.Title`, `Dialog.Close` |
| Tabla | `Table` en `LayerCard className="p-0"` | `Table.Header/Row/Head/Body/Cell` |
| Paginador | `Pagination` con `labels` en español | por defecto sale en inglés; pasa `labels={{ navigation: 'Paginación', firstPage: 'Primera página', previousPage: 'Página anterior', nextPage: 'Página siguiente', lastPage: 'Última página', pageNumber: 'Número de página' }}` (ver `paginationLabels` en `Leases.tsx`) |
| Filtros de listado | `Select` (con `items`) + `Input` de búsqueda | estado en `useState`; el `Input` escribe `searchInput` y un `useEffect` con `setTimeout` ~300 ms lo copia a `search` y vuelve a la página 1; la carga se cancela con un flag `cancelled` (ver `Leases.tsx`) |
| Lista vacía | `Empty` | `<Empty title="Aún no hay propiedades" description="…" />` |
| Avance / proporción | `Meter` | `<Meter label={`Mes actual: ${formatMoney(pagado)} de ${formatMoney(total)}`} value={pct} showValue={false} />` (el dato tiene que venir de la API; si no viene, no lo inventes: pide el campo, E7) |
| Carga | `Loader` | `<Loader size={32} />` |
| Estado | `StatusBadge` (`components/`) / `Badge` | `<StatusBadge status={p.status} />`; para un enum nuevo agrega su entrada al mapa de `StatusBadge.tsx` |
| Botón | `Button` | `variant` `primary`/`secondary`/`destructive`/`secondary-destructive`; iconos de `@phosphor-icons/react` con sufijo `Icon` |

Reglas de la tabla:
- **Fecha** (E5): jamás `type="date"` ni `type="month"` (selector nativo en inglés, sin diseño). Períodos: `Select` con `periodsBetween` + `periodLabel`, como en el dialog de pago.
- **`Select` siempre con `items`** (`{ valor: etiqueta }`): sin él el trigger muestra el valor crudo ("Ruc", "Cash"; I15). Pasa también `className="w-full"` y `hideLabel={false}`.
- **Dialogs**: `Dialog.Root` queda siempre montado y se abre con `open`; no lo renderices condicionalmente. Padding `px-8 py-6` (`text-spacing`).
- **Excepción documentada** (I15): los montos KPI usan `text-3xl font-semibold` en un `div` dentro de `Surface`, por legibilidad en proyector; deja el comentario como en `Summary.tsx`. Ningún otro texto sale de 14px.
- Tarjeta clickeable completa: ver el `div role="link" tabIndex={0}` con `onKeyDown` de `Summary.tsx` (I14: solo media tarjeta respondía y no usaba teclado).

## Qué NO hacer

- Copiar a mano estilos (`<h1 className=…>`, `<a>`, `<span>` sueltos) donde hay componente de Kumo (I16).
- Mostrar valores crudos de enums: pasa por `propertyTypeLabel`, `docTypeLabel`, `StatusBadge` o `items` (I15).
- Usar la fecha del navegador (E1) o selectores nativos de fecha (E5).
- Inferir el campo de un error leyendo el texto del mensaje (I12).
- Mostrar vacío en blanco: usa `lease.tenantPhone || '—'` o `formatDate` (E3).

## Checklist de terminado

Desde `web/`:

- [ ] `pnpm run build` y `pnpm run lint` sin errores ni warnings nuevos.
- [ ] `grep -rnE 'type="(date|month)"|toISOString|fetch\(' web/src` no devuelve nada fuera de `api.ts` (que es el único que llama a `fetch`) y `format.ts` (solo un comentario).
- [ ] La página tiene su spec en `web/e2e/` (usa `/add-e2e`).
- [ ] Revisión visual con captura de pantalla del flujo completo, en estados vacío, con datos y con error (el build no detecta defectos como los de I14).
- [ ] Commit en español con el prefijo del RF, p. ej. `feat(RF-03): página de historial de pagos`.
