# ADR 0001: Arquitectura Inicial y Modulación

**Fecha**: 11-Jul-2026  
**Estado**: Aceptado

## Contexto

El proyecto Reflect busca mover contenido instalado desde rutas críticas de sistema hacia otros discos y engañar al software mediante Junctions NTFS. Debido a la alta interdependencia y la naturaleza frágil de las operaciones de disco (que requieren capacidad de deshacer o rollback transaccional) es necesario separar las responsabilidades.

## Decisiones

1. **Separación en 7 Bibliotecas Principales (Módulos)**:
   - `Reflect.Core`: Contratos y entidades base compartidas sin lógica pesada externa.
   - `Reflect.Installation`: Control de procesos (PIDs) y su jerarquía temporal.
   - `Reflect.Filesystem`: Abstracción de copia, lectura de atributos y rollback de IO.
   - `Reflect.Ntfs`: Aislamiento total de las llamadas a la API de Junctions (Reparse Points). El proyecto principal nunca debe llamar al API de Windows.
   - `Reflect.Snapshots`: Lógica de recolección de estado base.
   - `Reflect.Analysis`: Comparación heurística de snapshots.
   - `Reflect.Reporting`: Consolidación de logs de ejecución.

2. **Snapshot Heurístico vs USN Journal**:
   - Por simplicidad y robustez inicial, los snapshots se construirán analizando de forma superficial estructuras y estampas de tiempo en vez de procesar el USN Journal. El Journaling requiere hooks/parsers más avanzados y privilegios absolutos a un nivel más profundo. Se reservará para una fase o versión futura ("V2").

3. **UI Independiente**:
   - Se ha elegido separar `Reflect.UI` (basado en WPF moderno). La interfaz debe depender únicamente de orquestadores de `Reflect.Core` y no interactuar con el Filesystem o NTFS directamente, para que pudiese intercambiarse por una UI en consola si fuera necesario.

## Consecuencias

- **Positivo**: Alta capacidad de Test Unitario. Se pueden burlar (Mock) tanto `Ntfs` como `Filesystem` para pruebas puras de `Analysis` y `Snapshots`.
- **Negativo**: Inicialmente puede ser más pesado mover interfaces y DTOs de un proyecto a otro.
