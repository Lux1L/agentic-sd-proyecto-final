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
| RF-07 | HU-07 | CU-11 | RN-16, RN-17 | `Portfolio/GetPortfolioSummary.cs` | `GET /portfolio/summary` | `IntegrationTests/Portfolio/PortfolioTests` |
| RNF-06 | — | — | — | Todas las features | — | `ArchitectureTests/Layers/LayerTests`, `ArchitectureTests/Layers/FeatureIsolationTests` |

## Cobertura por regla de negocio (verificada contra el código)

Generada a partir de los comentarios `// RN-xx` de cada test. Al 2026-10-31: **144 tests** (114 unitarios, 24 de integración y 6 de arquitectura), todos en verde.

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
| RN-15 | `Leases/LeaseStatementTests` (incluye el primer período que vence en la fecha de inicio), `Leases/PeriodTests` | `LeasesTests.FullFlow_…` |
| RN-16, RN-17 | `Leases/LeaseStatementTests` (11) | `PortfolioTests.GetSummary_Should_ReportCurrentPeriodStatusAndOverdueDebt` |
| RN-18 | `Common/LimaClockTests.Today_Should_ConvertToLimaDate_When_UtcCrossesMidnight` | — |
| RNF-03 | — | `PropertiesTests.Requests_Should_ReturnUnauthorized_When_NoToken`, `PortfolioTests.GetSummary_Should_ReturnUnauthorized_WhenNoToken` |

## Pruebas de arquitectura (ADR-001)

| Test | Qué garantiza |
|---|---|
| `LayerTests.CommandAndQueryHandlers_Should_BeSealed`, `Endpoints_Should_BeSealed` | Convenciones de la plantilla. |
| `FeatureIsolationTests.Features_Should_NotReferenceSliceTypesOfOtherFeatures` | Un slice no usa el Command/Handler/Endpoint de otra feature. |
| `FeatureIsolationTests.IsolationCheck_Should_DetectCrossFeatureSliceReference` | Prueba negativa: el detector falla ante una referencia cruzada. |
| `FeatureIsolationTests.PureDomainFunctions_Should_NotDependOnFrameworks` | `LeaseStatement`, `TenantDocument`, `Period` y `LimaClock` no dependen de EF Core ni ASP.NET (capa "Domain" lógica). |
| `FeatureIsolationTests.EntityTypeConfigurations_Should_LiveInDatabaseConfigurations` | La persistencia vive en `Database/` (capa "Infrastructure" lógica). |
