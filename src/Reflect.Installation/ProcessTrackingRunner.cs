using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using Reflect.Core.Interfaces;

namespace Reflect.Installation
{
    // Ignoramos el warning de plataforma porque Reflect es nativo para Windows.
#pragma warning disable CA1416
    public class ProcessTrackingRunner : IInstallationRunner
    {
        public async Task<int> RunAndWaitAsync(string installerPath, string arguments = "", CancellationToken cancellationToken = default)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = installerPath,
                Arguments = arguments,
                UseShellExecute = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) throw new InvalidOperationException("No se pudo iniciar el instalador.");

            var activePids = new HashSet<int> { process.Id };
            var allKnownPids = new HashSet<int> { process.Id };

            // Bucle principal de monitoreo mediante WMI
            while (activePids.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT ProcessId, ParentProcessId FROM Win32_Process");
                    using var collection = searcher.Get();

                    var currentProcesses = new Dictionary<int, int>(); // Mapa de PID -> ParentPID
                    
                    foreach (var mo in collection.Cast<ManagementObject>())
                    {
                        var pid = Convert.ToInt32(mo["ProcessId"]);
                        var ppid = Convert.ToInt32(mo["ParentProcessId"]);
                        currentProcesses[pid] = ppid;
                    }

                    // 1. Descubrir hijos huérfanos o nuevos procesos secundarios (Wrappers, msiexec)
                    var newChildren = currentProcesses
                        .Where(kvp => allKnownPids.Contains(kvp.Value) && !allKnownPids.Contains(kvp.Key))
                        .Select(kvp => kvp.Key)
                        .ToList();

                    foreach (var childPid in newChildren)
                    {
                        activePids.Add(childPid);
                        allKnownPids.Add(childPid);
                    }

                    // 2. Limpiar procesos que ya han cerrado
                    var exited = new List<int>();
                    foreach (var activePid in activePids)
                    {
                        // Si el PID activo ya no está en la consulta de WMI, es que ha terminado.
                        if (!currentProcesses.ContainsKey(activePid))
                        {
                            exited.Add(activePid);
                        }
                    }

                    foreach (var pid in exited)
                    {
                        activePids.Remove(pid);
                    }
                }
                catch
                {
                    // Tragar errores transitorios de WMI (Access Denied o Dispose de objetos)
                    // que ocurren si un proceso muere justo en medio de nuestra consulta.
                }

                // Esperamos un momento antes de volver a sondear
                if (activePids.Count > 0)
                {
                    await Task.Delay(1500, cancellationToken);
                }
            }

            // Dado que el árbol completo cerró, devolvemos 0 como genérico 
            // (el código de salida exacto es complejo en sub-árboles).
            return 0; 
        }
    }
#pragma warning restore CA1416
}
