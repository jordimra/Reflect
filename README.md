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
*(Para mayores detalles de arquitectura o reglas internas del proyecto, consulta la documentación privada en `/docs` local si cuentas con el código fuente completo).*
