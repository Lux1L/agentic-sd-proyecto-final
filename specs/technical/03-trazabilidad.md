# 03 · Matriz de trazabilidad

RF → HU → CU → RN → código (slice) → endpoint → pruebas. Las pruebas unitarias están en `tests/Web.Api.UnitTests`, las de integración en `tests/IntegrationTests` y las de arquitectura en `tests/ArchitectureTests`.

| RF | HU | CU | RN | Slice (`src/Web.Api/Features/…`) | Endpoint | Pruebas |
|---|---|---|---|---|---|---|
| RF-01 | HU-01 | CU-01 | RN-01 | `Properties/CreateProperty.cs` | `POST /properties` | `Properties/CreatePropertyValidatorTests` · `IntegrationTests/Properties/PropertiesTests` |
| RF-01 | HU-01 | CU-02 | RN-01 | `Properties/UpdateProperty.cs` | `PUT /properties/{id}` | `PropertiesTests.Update_Should_ChangeNameAndAddress` |
| RF-01 | HU-01 | CU-03 | — | `Properties/GetProperties.cs`, `GetPropertyById.cs` | `GET /properties[/{id}]` | `IntegrationTests/Properties/PropertiesTests` |
| RF-01 | HU-01 | CU-04 | RN-02 | `Properties/DeactivateProperty.cs`, `ActivateProperty.cs` | `POST /properties/{id}/deactivate\|activate` | `Properties/DeactivatePropertyHandlerTests` · `PropertiesTests` |
| RF-01 | HU-01 | CU-05 | RN-03 | `Properties/DeleteProperty.cs` | `DELETE /properties/{id}` | `Properties/DeletePropertyHandlerTests` |
| RF-02 | HU-02 | CU-06 | RN-04, RN-05, RN-10 | `Leases/CreateLease.cs` | `POST /leases` | `Leases/CreateLeaseHandlerTests` · `IntegrationTests/Leases/LeasesTests` |
| RF-02 | HU-02 | CU-06 | RN-06, RN-07, RN-08, RN-09 | `Leases/CreateLease.cs` (Validator), `Leases/TenantDocument.cs` | `POST /leases` | `Leases/CreateLeaseValidatorTests` · `Leases/TenantDocumentTests` |
| RF-03 | HU-03 | CU-07 | — | `Leases/GetLeases.cs`, `GetLeaseById.cs` · UI: `web/src/pages/Leases.tsx` | `GET /leases[/{id}]` | `LeasesTests.GetLeases_Should_FilterByPropertyAndStatus` (escenario Gherkin de HU-03, agregado tras `/vsa-review`) · `web/e2e/contratos.spec.ts` |
| RF-04 | HU-04 | CU-08 | RN-12, RN-13, RN-14 | `Leases/RecordPayment.cs` | `POST /leases/{id}/payments` | `Leases/RecordPaymentHandlerTests` · `LeasesTests` |
| RF-05 | HU-05 | CU-09 | RN-15, RN-16, RN-17, RN-18 | `Leases/GetLeaseStatement.cs`, `Leases/LeaseStatement.cs`, `Common/LimaClock.cs` | `GET /leases/{id}/statement` | `Leases/LeaseStatementTests`, `Common/LimaClockTests` · `LeasesTests` |
| RF-06 | HU-06 | CU-10 | RN-11 | `Leases/TerminateLease.cs` | `POST /leases/{id}/terminate` | `Leases/TerminateLeaseHandlerTests` · `LeasesTests` |
| RF-07 | HU-07 | CU-11 | RN-15, RN-16, RN-17, RN-19 | `Portfolio/GetPortfolioSummary.cs`, `Portfolio/CollectionSnapshot.cs` | `GET /portfolio/summary` | `Portfolio/CollectionSnapshotTests` · `IntegrationTests/Portfolio/PortfolioTests` |
| RNF-06 | — | — | — | Todas las features | — | `ArchitectureTests/Layers/LayerTests`, `ArchitectureTests/Layers/FeatureIsolationTests` |

## Cobertura por regla de negocio (verificada contra el código)

Generada a partir de los comentarios `// RN-xx` de cada test. Al 2026-10-31: **157 tests** (123 unitarios, 28 de integración y 6 de arquitectura), todos en verde.

| RN | Pruebas unitarias (`tests/Web.Api.UnitTests`) | Pruebas de integración (`tests/IntegrationTests`) |
|---|---|---|
| RN-01 | `Properties/CreatePropertyValidatorTests` (6) | `PropertiesTests.Create_Should_ReturnBadRequest_When_NameMissing`, `Create_Should_ReturnId`, `GetProperties_Should_FilterByStatus` |
| RN-02 | `Properties/DeactivatePropertyHandlerTests` (3) | `PropertiesTests.Deactivate_Should_ReturnConflict_When_ActiveLease` |
| RN-03 | `Properties/DeletePropertyHandlerTests` (3) | `PropertiesTests.Delete_Should_ReturnConflict_When_LeaseHistory`, `Delete_Should_ReturnNoContent_When_NoHistory` |
| RN-04 | `Leases/CreateLeaseHandlerTests` | — |
| RN-05 | `Leases/CreateLeaseHandlerTests` (incluye que un contrato terminado no cuenta como solapamiento) | `LeasesTests.Create_Should_ReturnConflict_When_DatesOverlap` |
| RN-06, RN-07, RN-08 | `Leases/CreateLeaseValidatorTests` (14) | — |
| RN-09 | `Leases/TenantDocumentTests` (6, RUC con dígito módulo 11), `CreateLeaseValidatorTests` | — |
| RN-10 | `Leases/CreateLeaseHandlerTests` | `LeasesTests.FullFlow_Should_TrackDebt_And_Terminate_When_Paid` |
| RN-11 | `Leases/TerminateLeaseHandlerTests` (3) | `LeasesTests.FullFlow_Should_TrackDebt_And_Terminate_When_Paid` |
| RN-12, RN-13, RN-14 | `Leases/RecordPaymentHandlerTests` (7), `Leases/RecordPaymentValidatorTests` (8, RN-12) | `LeasesTests.RecordPayment_Should_ReturnBadRequest_When_ExceedsRent` |
| RN-15 | `Leases/LeaseStatementTests` (incluye el primer período que vence en la fecha de inicio), `Leases/PeriodTests`, `Portfolio/CollectionSnapshotTests.From_Should_UseStartDateAsNextDueDate_When_LeaseStartsNextMonthAfterPaymentDay` | `LeasesTests.FullFlow_…`, `PortfolioTests.GetSummary_Should_ReportNextDueDate_When_UpToDate` |
| RN-16, RN-17 | `Leases/LeaseStatementTests` (11), `Portfolio/CollectionSnapshotTests.From_Should_ListOverduePeriodsOldestFirst_When_SeveralMonthsUnpaid`, `From_Should_NotListCurrentPeriod_When_NotYetDue` | `PortfolioTests.GetSummary_Should_ReportCurrentPeriodStatusAndOverdueDebt`, `GetSummary_Should_ListOverduePeriods_When_SeveralMonthsUnpaid` |
| RN-18 | `Common/LimaClockTests.Today_Should_ConvertToLimaDate_When_UtcCrossesMidnight` | — |
| RN-19 | `Portfolio/CollectionSnapshotTests` (9: períodos atrasados del más antiguo al más reciente, días de atraso máximos, próximo vencimiento dentro del contrato, último mes del contrato, terminación, contrato que empieza después y sin contrato) | `PortfolioTests.GetSummary_Should_ListOverduePeriods_When_SeveralMonthsUnpaid`, `GetSummary_Should_ReportNextDueDate_When_UpToDate`, `GetSummary_Should_ReturnEmptyCollectionFields_When_NoActiveLease`, `GetSummary_Should_HaveNoNextDueDate_When_CurrentMonthIsLastOfLease` |
| RNF-03 | — | `PropertiesTests.Requests_Should_ReturnUnauthorized_When_NoToken`, `PortfolioTests.GetSummary_Should_ReturnUnauthorized_WhenNoToken` |

## Pruebas de arquitectura (ADR-001)

| Test | Qué garantiza |
|---|---|
| `LayerTests.CommandAndQueryHandlers_Should_BeSealed`, `Endpoints_Should_BeSealed` | Convenciones de la plantilla. |
| `FeatureIsolationTests.Features_Should_NotReferenceSliceTypesOfOtherFeatures` | Un slice no usa el Command/Handler/Endpoint de otra feature. |
| `FeatureIsolationTests.IsolationCheck_Should_DetectCrossFeatureSliceReference` | Prueba negativa: el detector falla ante una referencia cruzada. |
| `FeatureIsolationTests.PureDomainFunctions_Should_NotDependOnFrameworks` | `LeaseStatement`, `TenantDocument`, `Period`, `CollectionSnapshot` y `LimaClock` no dependen de EF Core ni ASP.NET (capa "Domain" lógica). |
| `FeatureIsolationTests.EntityTypeConfigurations_Should_LiveInDatabaseConfigurations` | La persistencia vive en `Database/` (capa "Infrastructure" lógica). |

## Pruebas E2E en Google Chrome (`web/e2e/`, Playwright)

Se corren con `cd web && pnpm e2e` (con la API y Vite levantados). Son 22 tests sobre el Chrome instalado, con `locale es-PE` y `timezoneId America/Lima`. Cada spec que muta datos crea su propia propiedad por API.

| Spec | Tests | HU / RF | RN |
|---|---|---|---|
| `login.spec.ts` | 2 | RF-00 | RNF-03 (credenciales inválidas: mensaje genérico en español) |
| `sesion.spec.ts` | 2 | RF-00 | RNF-03 (sin token → /login) |
| `resumen.spec.ts` | 4 | HU-07 (RF-07), HU-04 (RF-04) | RN-16, RN-17 (bandeja de cobranza: orden de grupos, copy del atraso, registrar pago desde el inicio) |
| `banner-deuda.spec.ts` | 2 | HU-05 (RF-05), HU-02 (RF-02) | RN-17 (deuda vencida), RN-08 (día de pago 1–28) |
| `detalle-errores.spec.ts` | 3 | HU-04 (RF-04), HU-06 (RF-06) | RN-14 (sobrepago), RN-11 (terminar con deuda), RN-13 (período dentro del contrato) |
| `nuevo-contrato.spec.ts` | 1 (flujo completo) | HU-02, HU-04, HU-05 | RN-09 (RUC módulo 11), RN-15, RN-16 (Parcial), RN-05 (solapamiento) |
| `fecha-lima.spec.ts` | 2 | HU-04 | RN-18 (corre en `Asia/Tokyo`: la fecha por defecto debe ser la de Lima, en el detalle y desde el inicio) |
| `contratos.spec.ts` | 6 | HU-03 (RF-03) | Filtro por estado, búsqueda, estado vacío, navegación y paginación |
