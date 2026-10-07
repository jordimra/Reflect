# Reflect

Reflect es una aplicación de escritorio moderna para Windows cuyo objetivo es trasladar instalaciones de software pesado o específico desde sus ubicaciones estándar (como `Program Files`, `ProgramData`, `AppData`) hacia otra unidad o partición, de manera completamente transparente para la aplicación y sin interceptar la instalación original. 

El proyecto reemplaza las carpetas desplazadas con **Junctions NTFS**, logrando que el software instalado crea seguir residiendo en la ruta por defecto del sistema.

## Objetivo y Filosofía

- **Reorganización Post-Instalación**: Reflect no monitoriza ni pausa tu instalador. En su lugar, analiza tu sistema antes y después de instalar algo, calcula la "diferencia" y permite reubicar estos nuevos datos.
- **Transparencia**: Aprovecha la tecnología NTFS incorporada en Windows. No usa *Hooks*, *DLL Injections*, *Drivers* ni filtros complicados que pongan en peligro la estabilidad de tu sistema.
- **Sin Dependencias de Ejecución**: Una vez Reflect ha hecho su trabajo, no necesitas dejarlo corriendo en segundo plano.

## Limitaciones y Compatibilidad

> [!WARNING]  
> Reflect **no garantiza compatibilidad con el 100% de los programas**.  

- Si una aplicación hace uso estricto de rutas físicas (resolviendo *Device Paths*) y espera estar estrictamente en el disco `C:\`, podría dejar de funcionar si se mueve, ignorando el Junction lógico.
- No se deben reubicar jamás componentes troncales de Windows, carpetas críticas como `WinSxS`, ni binarios del núcleo del SO, pues comprometería la estabilidad general.

## Cómo Funciona (Flujo Básico)

1. Seleccionas un instalador.
2. Indicas la carpeta de destino física.
3. Reflect toma una "fotografía" rápida del estado de tus discos.
4. Se ejecuta el instalador normalmente mientras Reflect espera.
5. Al terminar, Reflect detecta qué archivos y carpetas nuevas aparecieron en las rutas del sistema y las asocia a la instalación.
6. Permite aprobar qué mover y se encarga de crear las redirecciones por ti.

## Desarrollo y Compilación

Reflect está desarrollado bajo .NET (versión LTS).
Puedes construir la solución abriendo `Reflect.slnx` con Visual Studio 2022 o utilizando .NET CLI:
```bash
dotnet build
```

Para generar un ejecutable final, autónomo e independiente (*Standalone*), optimizado para Windows:
```bash
dotnet publish src/Reflect.UI/Reflect.UI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```
Esto creará un único archivo `Reflect.UI.exe` listo para distribuir.

## Últimas Mejoras

- **Selección real del destino físico**: Se ha reemplazado el cuadro de diálogo simulado por un `OpenFolderDialog` funcional, permitiendo examinar y elegir visualmente la carpeta de reubicación.
- **Amplio espectro de Monitorización**: La aplicación ya no se limita a `C:\Program Files`. Ahora captura los cambios en paralelo en `Program Files (x86)`, `ProgramData`, y carpetas de configuración del usuario `AppData` (Roaming, Local, LocalLow) previniendo que se pasen por alto componentes instalados de forma distribuida.
- **Elevación de privilegios (UAC)**: Reflect ahora solicita automáticamente permisos de Administrador a través del archivo `app.manifest`. Esto asegura el correcto funcionamiento al manipular atributos protegidos o al escribir/enlazar sobre ubicaciones críticas.
- **Protección de Atributos en Rollback (Bugfix "Access Denied")**: El motor `TransactionalRelocationEngine` ha sido refinado para purgar atributos de "Sólo Lectura" antes de borrar o revertir las copias, evitando cierres o bloqueos catastróficos por permisos nativos.
- **Detalle de Rutas y Exclusión Manual en UI**: 
  - La tabla de reubicación de WPF detalla con precisión las rutas "Origen" y "Destino".
  - Se han implementado casillas (Checkboxes) para la selección granular. De esta forma, si Windows Installer toca directorios colaterales (como `AppData\Local\Microsoft`), el usuario puede desmarcarlos de forma segura antes de realizar los Junctions.
