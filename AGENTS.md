# AGENTS.md

## Comandos

- Build: `dotnet build` (usa `Reflect.slnx`, solo incluye los 8 proyectos de `src/`).
- Tests: `dotnet test tests/Reflect.Tests/Reflect.Tests.csproj` — **el proyecto de tests NO está en la solución**; un `dotnet test` desde la raíz no ejecuta ni compila nada.
- Test individual: `dotnet test tests/Reflect.Tests/Reflect.Tests.csproj --filter "FullyQualifiedName~NtfsManagerTests"`.
- Publicación standalone: `dotnet publish src/Reflect.UI/Reflect.UI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true` (genera un único `Reflect.UI.exe`).
- No hay lint, typecheck, formateador ni CI configurados. Verificación = `dotnet build` + `dotnet test` (verificado: 7 tests, xunit).

## Entorno

- .NET 10 SDK (SDK 10.0.302). Librerías `net10.0`; `Reflect.UI` `net10.0-windows` (WPF).
- Solo Windows: junctions NTFS vía P/Invoke (`kernel32`/`DeviceIoControl`) en `Reflect.Ntfs`.
- `Reflect.UI` exige administrador (`app.manifest`: `requireAdministrator`) → lanza UAC al ejecutarla.
- `Reflect.Tests` apunta a `net10.0` plano pero depende de APIs de Windows (crea junctions reales en `%TEMP%`): solo funciona en Windows.

## Arquitectura

- 7 librerías en `src/` + `Reflect.UI` (WPF, orquestación en code-behind). Contratos y modelos en `Reflect.Core` (`Interfaces/`, `Models/`).
- ADR 0001: `Reflect.Ntfs` es la única capa que toca la API nativa de junctions; el resto usa `INtfsManager`.
- La orquestación real está en `src/Reflect.UI/MainWindow.xaml.cs` (clases concretas cableadas a mano, sin DI): `StandardSnapshotEngine` → `ProcessTrackingRunner` → `BasicChangeAnalyzer` → `TransactionalRelocationEngine` (que envuelve `NtfsManager`).
- `IProposalEngine` está definida en `Reflect.Core` pero **sin implementación**; `Reflect.Reporting` es un stub (`Class1.cs`).

## Convenciones

- Documentación, ADRs y textos de UI en castellano; identificadores de código en inglés.
- Decisiones importantes → nuevo ADR en `docs/adr/NNNN-slug.md` (ver `0001-arquitectura-inicial.md` como formato).
- Restricción de dominio (no negociar): solo se mueven subdirectorios de aplicaciones; jamás `Program Files` completo, `WinSxS`, `Installer`, `Common Files` ni `Microsoft Shared` (detallado en `PROJECT.md` y `README.md`).
- `PROJECT.md` es un documento local (git-ignorado) con la intención y filosofía del proyecto; indica leer `rules/*.md` antes de cada tarea, pero esa carpeta está vacía e ignorada por git.
- El repo usa OpenSpec (comandos `opsx-*` y skills en `.opencode/`, specs en `openspec/`): los cambios de comportamiento se proponen y aplican por ese flujo, no a ciegas.
