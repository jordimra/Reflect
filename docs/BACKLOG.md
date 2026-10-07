# Backlog del Proyecto Reflect

Este documento refleja el historial de tareas completadas y el trabajo pendiente a nivel macro, desglosado por fases.

## Fase 1: Arquitectura Inicial y Estructura (Completado)

_Fecha de finalización: 11-Jul-2026_

- [x] Análisis del proyecto y riesgos técnicos.
- [x] Definición de la arquitectura modular en 7 bibliotecas (`Reflect.Core`, `Reflect.Installation`, `Reflect.Filesystem`, `Reflect.Ntfs`, `Reflect.Snapshots`, `Reflect.Analysis`, `Reflect.Reporting`) y el front-end (`Reflect.UI`).
- [x] Creación de la solución `Reflect.slnx` con .NET CLI.
- [x] Creación de referencias y enlaces entre proyectos.
- [x] Creación del archivo público `README.md`.
- [x] Creación del ADR inicial `/docs/adr/0001-arquitectura-inicial.md`.

## Fase 2: Definición del Núcleo (Reflect.Core) (Completado)

_Objetivo: Establecer los contratos e interfaces de todos los módulos para permitir su desarrollo en paralelo._

- [x] Definir los modelos de dominio (`Snapshot`, `FileSystemNode`, `ChangeList`, `ApplicationProposal`).
- [x] Definir las interfaces de los motores (`ISnapshotEngine`, `IChangeAnalyzer`, `IProposalEngine`, `IRelocationEngine`, `INtfsManager`, `IInstallationRunner`).
- [x] Implementar clases base compartidas o excepciones de dominio (`ReflectException`, etc.).

## Fases Futuras (Pendientes)

## Fase 3: Snapshots y Analysis (Completado)

_Objetivo: Implementar la captura de estado del sistema y la heurística de diferencias (Diff)._

- [x] Implementar `StandardSnapshotEngine` (Manejo tolerante a fallos de enumeración).
- [x] Implementar `BasicChangeAnalyzer` (Comparación algorítmica).
## Fase 4: Reflect.Installation (Completado)
*Objetivo: Ejecutar instaladores y asegurar la monitorización del árbol de procesos hijos completo usando WMI.*

- [x] Añadir `System.Management` al proyecto.
- [x] Implementar `ProcessTrackingRunner` (Rastreo recursivo y esperas).
## Fase 5: Archivos Físicos y NTFS (Completado)
*Objetivo: Efectos secundarios físicos, creación de Junctions y capacidad de Rollback.*

- [x] Implementar `NtfsManager` (Abstracción API nativa).
- [x] Implementar `TransactionalRelocationEngine` (Copias de seguridad transaccionales).
## Fase 6: Integración y Reflect.UI (Completado)
*Objetivo: Interfaz visual y orquestación de todos los motores.*

- [x] Crear la ventana principal de WPF (XAML).
- [x] Orquestar de forma asíncrona (CodeBehind).
- **Fase 7**: Testing general, QA y despliegue inicial.
