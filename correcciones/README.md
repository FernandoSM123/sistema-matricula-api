# Correcciones

Constancia de los ajustes que se hacen a la API **después** de cerrar sus módulos: brechas que se descubren al integrar un cliente, detalles de redacción, inconsistencias entre documentos y código, etc. No son specs: cada documento registra qué se encontró, qué se decidió y por qué, qué se cambió y cómo se verificó.

Si un ajuste cambia el alcance de un módulo, también se anota en el historial de su spec y, si corresponde, en las decisiones de `specs/mapa-modulos.md`. Este documento queda como el registro detallado.

## Formato

- Un archivo por lote de correcciones: `COR-<nn>-<tema>.md` (por ejemplo, `COR-01-brechas-frontend.md`). Los números no se reutilizan.
- Cabecera con el estado, la fecha, el origen y los módulos afectados.
- Una sección por punto, con: problema, verificación en el código, decisión, cambio propuesto, documentos afectados y estado.
- Un historial de cambios al final.

**Estados de un punto:**
- **Pendiente:** aprobado, sin implementar.
- **En progreso:** en implementación.
- **Implementado:** código listo; falta verificar.
- **Completado:** implementado, verificado y con commit.
- **Descartado:** no se hará; se deja la razón.

**Estados del documento:** **Abierto** (algún punto sin cerrar) o **Cerrado** (todos completados o descartados).

## Índice

| Id | Tema | Fecha | Estado | Puntos |
|---|---|---|---|---|
| [COR-01](COR-01-brechas-frontend.md) | Brechas detectadas al planear el frontend | 2026-11-17 | Cerrado | 3 completados, 1 descartado |
| [COR-02](COR-02-encabezados-cors.md) | Encabezados de respuesta que el frontend no puede leer por CORS | 2026-11-23 | Cerrado | 2 completados |
