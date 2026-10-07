using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using Reflect.Core.Interfaces;

namespace Reflect.Ntfs
{
    public class NtfsManager : INtfsManager
    {
        private const int FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
        private const int FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;
        private const int FSCTL_SET_REPARSE_POINT = 0x000900A4;
        private const uint IO_REPARSE_TAG_MOUNT_POINT = 0xA0000003;
        private const string NON_INTERPRETED_PATH_PREFIX = @"\??\";

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern SafeFileHandle CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            FileShare dwShareMode,
            IntPtr lpSecurityAttributes,
            FileMode dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DeviceIoControl(
            SafeFileHandle hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer,
            int nInBufferSize,
            IntPtr lpOutBuffer,
            int nOutBufferSize,
            out int lpBytesReturned,
            IntPtr lpOverlapped);

        public void CreateJunction(string junctionPoint, string targetDir)
        {
            if (string.IsNullOrWhiteSpace(junctionPoint)) throw new ArgumentNullException(nameof(junctionPoint));
            if (string.IsNullOrWhiteSpace(targetDir)) throw new ArgumentNullException(nameof(targetDir));

            if (!Directory.Exists(targetDir))
                throw new DirectoryNotFoundException($"El directorio destino no existe: {targetDir}");

            // Crear el directorio físico del junction si no existe
            if (!Directory.Exists(junctionPoint))
            {
                Directory.CreateDirectory(junctionPoint);
            }

            string targetPath = Path.GetFullPath(targetDir);
            string junctionTargetPath = NON_INTERPRETED_PATH_PREFIX + targetPath;

            using (SafeFileHandle handle = CreateFile(
                junctionPoint,
                0x40000000, // GENERIC_WRITE
                FileShare.ReadWrite | FileShare.Delete,
                IntPtr.Zero,
                FileMode.Open,
                FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAG_OPEN_REPARSE_POINT,
                IntPtr.Zero))
            {
                if (handle.IsInvalid)
                    throw new IOException($"No se pudo abrir el directorio para crear el junction: {junctionPoint}. Error Win32: {Marshal.GetLastWin32Error()}");

                byte[] substituteNameBytes = Encoding.Unicode.GetBytes(junctionTargetPath);
                byte[] printNameBytes = Encoding.Unicode.GetBytes(targetPath);

                int headerSize = 8; // ReparseTag (4) + ReparseDataLength (2) + Reserved (2)
                int dataSize = 8 + substituteNameBytes.Length + 2 + printNameBytes.Length + 2;
                int bufferSize = headerSize + dataSize;
                IntPtr buffer = Marshal.AllocHGlobal(bufferSize);

                try
                {
                    Marshal.WriteInt32(buffer, 0, unchecked((int)IO_REPARSE_TAG_MOUNT_POINT));
                    Marshal.WriteInt16(buffer, 4, (short)dataSize);
                    Marshal.WriteInt16(buffer, 6, 0); // Reserved
                    
                    Marshal.WriteInt16(buffer, 8, 0); // SubstituteNameOffset
                    Marshal.WriteInt16(buffer, 10, (short)substituteNameBytes.Length);
                    Marshal.WriteInt16(buffer, 12, (short)(substituteNameBytes.Length + 2)); // PrintNameOffset
                    Marshal.WriteInt16(buffer, 14, (short)printNameBytes.Length);

                    // PathBuffer empieza exactamente en el offset 16
                    Marshal.Copy(substituteNameBytes, 0, new IntPtr(buffer.ToInt64() + 16), substituteNameBytes.Length);
                    Marshal.WriteInt16(new IntPtr(buffer.ToInt64() + 16 + substituteNameBytes.Length), 0);
                    
                    Marshal.Copy(printNameBytes, 0, new IntPtr(buffer.ToInt64() + 18 + substituteNameBytes.Length), printNameBytes.Length);
                    Marshal.WriteInt16(new IntPtr(buffer.ToInt64() + 18 + substituteNameBytes.Length + printNameBytes.Length), 0);

                    if (!DeviceIoControl(handle, FSCTL_SET_REPARSE_POINT, buffer, bufferSize, IntPtr.Zero, 0, out int bytesReturned, IntPtr.Zero))
                    {
                        throw new IOException($"Error al establecer el Reparse Point. Win32 Error: {Marshal.GetLastWin32Error()}");
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
        }

        public bool IsJunction(string path)
        {
            if (!Directory.Exists(path) && !File.Exists(path))
                return false;

            var attributes = File.GetAttributes(path);
            return attributes.HasFlag(FileAttributes.ReparsePoint);
        }
    }
}
