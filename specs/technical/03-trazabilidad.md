# 03 · Matriz de trazabilidad

RF → HU → CU → RN → código (slice) → endpoint → pruebas. Las pruebas unitarias están en `tests/Web.Api.UnitTests`, las de integración en `tests/IntegrationTests` y las de arquitectura en `tests/ArchitectureTests`.

| RF | HU | CU | RN | Slice (`src/Web.Api/Features/…`) | Endpoint | Pruebas |
|---|---|---|---|---|---|---|
| RF-01 | HU-01 | CU-01 | RN-01 | `Properties/CreateProperty.cs` | `POST /properties` | `Properties/CreatePropertyValidatorTests` · `IntegrationTests/Properties/PropertiesTests` |
| RF-01 | HU-01 | CU-02 | RN-01 | `Properties/UpdateProperty.cs` | `PUT /properties/{id}` | `IntegrationTests/Properties/PropertiesTests` |
| RF-01 | HU-01 | CU-03 | — | `Properties/GetProperties.cs`, `GetPropertyById.cs` | `GET /properties[/{id}]` | `IntegrationTests/Properties/PropertiesTests` |
| RF-01 | HU-01 | CU-04 | RN-02 | `Properties/DeactivateProperty.cs`, `ActivateProperty.cs` | `POST /properties/{id}/deactivate\|activate` | `Properties/DeactivatePropertyHandlerTests` · `PropertiesTests` |
| RF-01 | HU-01 | CU-05 | RN-03 | `Properties/DeleteProperty.cs` | `DELETE /properties/{id}` | `Properties/DeletePropertyHandlerTests` |
| RF-02 | HU-02 | CU-06 | RN-04, RN-05, RN-10 | `Leases/CreateLease.cs` | `POST /leases` | `Leases/CreateLeaseHandlerTests` · `IntegrationTests/Leases/LeasesTests` |
| RF-02 | HU-02 | CU-06 | RN-06, RN-07, RN-08, RN-09 | `Leases/CreateLease.cs` (Validator), `Leases/TenantDocument.cs` | `POST /leases` | `Leases/CreateLeaseValidatorTests` · `Leases/TenantDocumentTests` |
| RF-03 | HU-03 | CU-07 | — | `Leases/GetLeases.cs`, `GetLeaseById.cs` | `GET /leases[/{id}]` | `IntegrationTests/Leases/LeasesTests` |
| RF-04 | HU-04 | CU-08 | RN-12, RN-13, RN-14 | `Leases/RecordPayment.cs` | `POST /leases/{id}/payments` | `Leases/RecordPaymentHandlerTests` · `LeasesTests` |
| RF-05 | HU-05 | CU-09 | RN-15, RN-16, RN-17, RN-18 | `Leases/GetLeaseStatement.cs`, `Leases/LeaseStatement.cs`, `Common/LimaClock.cs` | `GET /leases/{id}/statement` | `Leases/LeaseStatementTests` · `LeasesTests` |
| RF-06 | HU-06 | CU-10 | RN-11 | `Leases/TerminateLease.cs` | `POST /leases/{id}/terminate` | `Leases/TerminateLeaseHandlerTests` · `LeasesTests` |
| RF-07 | HU-07 | CU-11 | RN-16, RN-17 | `Portfolio/GetPortfolioSummary.cs` | `GET /portfolio/summary` | `IntegrationTests/Portfolio/PortfolioTests` |
| RNF-06 | — | — | — | Todas las features | — | `ArchitectureTests/Layers/LayerTests`, `ArchitectureTests/Layers/FeatureIsolationTests` |

## Cobertura por regla de negocio

| RN | Prueba unitaria | Prueba de integración |
|---|---|---|
| RN-01 | `CreatePropertyValidatorTests` | `PropertiesTests.Create_Should_ReturnBadRequest_When_NameMissing` |
| RN-02 | `DeactivatePropertyHandlerTests` | `PropertiesTests.Deactivate_Should_ReturnConflict_When_ActiveLease` |
| RN-03 | `DeletePropertyHandlerTests` | — |
| RN-04 | `CreateLeaseHandlerTests` | — |
| RN-05 | `CreateLeaseHandlerTests` | `LeasesTests.Create_Should_ReturnConflict_When_DatesOverlap` |
| RN-06/07/08 | `CreateLeaseValidatorTests` | — |
| RN-09 | `TenantDocumentTests`, `CreateLeaseValidatorTests` | — |
| RN-10 | `CreateLeaseHandlerTests` | `LeasesTests` (flujo completo) |
| RN-11 | `TerminateLeaseHandlerTests` | `LeasesTests` (flujo completo) |
| RN-12/13/14 | `RecordPaymentHandlerTests` | `LeasesTests.RecordPayment_Should_ReturnBadRequest_When_ExceedsRent` |
| RN-15/16/17 | `LeaseStatementTests` | `LeasesTests` (flujo completo) |
| RN-18 | `LeaseStatementTests` (borde de medianoche), `LimaClockTests` | — |
