using System;
using System.Runtime.InteropServices;

internal class EjectMedia
{
	private const uint GENERICREAD = 2147483648u;

	private const uint OPENEXISTING = 3u;

	private const uint IOCTL_STORAGE_EJECT_MEDIA = 2967560u;

	private const int INVALID_HANDLE = -1;

	private static IntPtr fileHandle;

	private static uint returnedBytes;

	[DllImport("kernel32", SetLastError = true)]
	private static extern IntPtr CreateFile(string fileName, uint desiredAccess, uint shareMode, IntPtr attributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);

	[DllImport("kernel32", SetLastError = true)]
	private static extern int CloseHandle(IntPtr driveHandle);

	[DllImport("kernel32", SetLastError = true)]
	private static extern bool DeviceIoControl(IntPtr driveHandle, uint IoControlCode, IntPtr lpInBuffer, uint inBufferSize, IntPtr lpOutBuffer, uint outBufferSize, ref uint lpBytesReturned, IntPtr lpOverlapped);

	public static void Eject(string driveLetter)
	{
		try
		{
			fileHandle = CreateFile(driveLetter, 2147483648u, 0u, IntPtr.Zero, 3u, 0u, IntPtr.Zero);
			if ((int)fileHandle != -1)
			{
				DeviceIoControl(fileHandle, 2967560u, IntPtr.Zero, 0u, IntPtr.Zero, 0u, ref returnedBytes, IntPtr.Zero);
			}
		}
		catch
		{
			throw new Exception(Marshal.GetLastWin32Error().ToString());
		}
		finally
		{
			CloseHandle(fileHandle);
			fileHandle = IntPtr.Zero;
		}
	}
}
