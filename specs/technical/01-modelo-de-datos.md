# 01 · Modelo de datos

PostgreSQL 18, esquema `public`, nombres en `snake_case` (convención de la plantilla). Las relaciones son por FK sin propiedades de navegación (convención VSA del repo).

```mermaid
erDiagram
    PROPERTIES ||--o{ LEASES : "tiene"
    LEASES ||--o{ PAYMENTS : "recibe"
    USERS ||--o{ REFRESH_TOKENS : "tiene"

    PROPERTIES {
        uuid id PK
        varchar(100) name
        varchar(200) address
        int type "Apartment|House|Room|Commercial"
        int status "Available|Leased|Inactive"
        timestamptz created_at
    }
    LEASES {
        uuid id PK
        uuid property_id FK
        varchar(150) tenant_full_name
        int tenant_document_type "Dni|Ce|Ruc"
        varchar(12) tenant_document_number
        varchar(9) tenant_phone "nullable"
        varchar(200) tenant_email "nullable"
        date start_date
        date end_date
        numeric(12_2) monthly_rent
        numeric(12_2) deposit
        int payment_day "1..28"
        int status "Active|Terminated"
        date terminated_on "nullable"
        timestamptz created_at
    }
    PAYMENTS {
        uuid id PK
        uuid lease_id FK
        int period_year
        int period_month
        numeric(12_2) amount
        date paid_on
        int method "Cash|BankTransfer|Yape|Plin"
        timestamptz created_at
    }
```

## Decisiones del modelo

| Decisión | Motivo |
|---|---|
| El arrendatario va **embebido en `leases`** (columnas `tenant_*`). | En v1 nadie consulta "todos los contratos de una persona". Evita una tabla y su CRUD. Separarlo luego es una migración simple. |
| Las fechas del contrato y del pago son **`date`** (`DateOnly`), no `timestamptz`. | Son fechas de calendario de negocio en Lima, no instantes. Así se evitan los corrimientos por zona horaria (RN-18). |
| El período se guarda como **`period_year` + `period_month`**. | Permite indexar y agrupar sin parsear texto. En la API se expone como `"YYYY-MM"`. |
| El **estado de cuenta no se persiste**: se calcula a partir de `leases` + `payments` + "hoy". | Nunca queda desactualizado y no necesita jobs. Ver ADR-004. |
| Montos en `numeric(12,2)` (`decimal`). | RNF-05: nunca `double` para dinero. |
| `properties.status` se guarda (no se deriva). | Lo cambian casos de uso explícitos (RN-10, RN-11, CU-04) y el resumen lo lee sin joins. |

## Índices

| Tabla | Índice | Uso |
|---|---|---|
| `leases` | `(property_id, status)` | Chequeo de solapamiento (RN-05) y resumen (RF-07). |
| `payments` | `(lease_id, period_year, period_month)` | Suma por período (RN-14) y estado de cuenta (RF-05). |
