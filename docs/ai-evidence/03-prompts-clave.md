# Prompts clave

## 1. Humano → IA (orquestador)

> "Hola, mañana tengo una presentación final de este curso. La rúbrica está en @Rubrica/ […] revísalo y dame 3 ideas para hacerlo, tengo plazo hasta mañana 8 am. Usaremos @Referencia/ para la base de nuestro proyecto en lo que respecta al API […] usaremos frontend con Vite y shadcn."

**Respuesta de la IA:** leyó la rúbrica (PDF de 15 láminas + índice de la presentación) y la plantilla. Propuso 3 ideas y señaló el conflicto Clean vs VSA y que el frontend no puntúa.

> Respuesta a la elección de idea: "Me gustaría un sistema de administración de arriendo/alquiler familiar"

> "Usaremos pg 18.6 para el desarrollo dado que ya tenemos el contenedor, usaremos https://github.com/Lux1L/agentic-sd-proyecto-final.git como repositorio, borra toda referencia de [autor de la plantilla], las skills pásalas a español. Y sigamos discutiendo el plan"

> Sobre el documento del arrendatario: "este sistema es para Perú, iremos por la opción 1"

> "el frontend se construirá al final"

> "invoca agentes sonnet para ejecutar y tú orquestas"

## 2. Orquestador → agentes

Cada brief sigue la misma estructura, que es la que evita el *vibe coding*:

1. **Qué leer primero:** `CLAUDE.md`, la skill que aplica, y los RF/RN y ADR concretos.
2. **Contrato exacto:** nombres de tipos, firmas, códigos de error y tipo de error (Problem/Conflict).
3. **Alcance y qué NO tocar** (para trabajar en paralelo sin conflictos).
4. **Criterio de terminado:** build sin warnings, `dotnet test` en verde y commits con el prefijo del RF.
5. **Reporte:** desviaciones del contrato explicadas, no improvisadas en silencio.

### Fase A · Dominio base (extracto)

```text
Eres el ejecutor de la Fase A (base del dominio) […] los nombres y firmas de abajo son
un contrato: respétalos exactamente.

LeaseStatement — función pura (sin EF, sin DI):
  public static LeaseStatement Calculate(Lease lease, IEnumerable<Payment> payments, DateOnly today);
Reglas (RN-15..17): lista períodos desde el mes de inicio hasta el menor entre el mes de
fin, el mes de today y (si TerminatedOn tiene valor) el mes de TerminatedOn […]
paid ≥ rent → Paid; today > dueDate y paid < rent → Overdue […]

LeaseErrors […] Nunca uses Error.Failure (mapea a 500).

LimaClockTests — borde de medianoche RN-18: UtcNow 2026-10-06T03:00Z → Today 2026-10-05.

Si algo del contrato era contradictorio o imposible, dilo explícitamente en vez de
improvisar en silencio.
```

### Fase B · Slices en paralelo

Se agrega al terminar la Fase A.
