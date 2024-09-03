using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace dllsupport;

public class Papiesz
{
	public static int height = Screen.PrimaryScreen.Bounds.Height;

	public static int width = Screen.PrimaryScreen.Bounds.Width;

	public static int x = 0;

	public static int y = 0;

	public static Form form;

	public static PictureBox picturebox;

	private static int generateInt = 0;

	public static List<Form> formList = new List<Form>();

	private static int latajInt = 0;

	private static GifImage gifImage = null;

	private static string link = "";

	private static Random rand = new Random();

	public static bool formFocus = false;

	private static IntPtr HWND_TOPMOST = new IntPtr(-1);

	private const uint SWP_NOSIZE = 1u;

	private const uint SWP_NOMOVE = 2u;

	private const uint TOPMOST_FLAGS = 3u;

	[DllImport("user32.dll")]
	public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

	public static void GifAnimated(string patch)
	{
		gifImage = new GifImage(patch);
		gifImage.ReverseAtEnd = false;
		picturebox.Image = gifImage.GetNextFrame();
	}

	public static void generateForm()
	{
		try
		{
			switch (generateInt)
			{
			case 0:
				createPictureBox("dllsupport.Resources.papagif.gif");
				form = new Form();
				form.ShowInTaskbar = false;
				form.Size = new Size(300, 250);
				form.Controls.Add(picturebox);
				createForm(width - form.Width, height - form.Height);
				formList.Add(form);
				link = "http://karachan.org/b/";
				createLinkLabel("[ZOBACZ]", form, picturebox, new Point(14, form.Height));
				break;
			case 1:
				createPictureBox("dllsupport.Resources.masa.jpg");
				form = new Form();
				form.Size = new Size(300, 300);
				form.Controls.Add(picturebox);
				createForm(0, 0);
				formList.Add(form);
				link = "https://www.youtube.com/watch?v=1vZ28SAgzKc";
				createLinkLabel("[ZOBACZ Jak zrobić taką mase]", form, picturebox, new Point(55, form.Height - 205));
				break;
			case 2:
				createPictureBox("dllsupport.Resources.papiezkreci.gif");
				form = new Form();
				form.Size = new Size(300, 300);
				form.Controls.Add(picturebox);
				createForm(width - form.Width, 0);
				formList.Add(form);
				break;
			case 3:
				createPictureBox("dllsupport.Resources.papiezlata.gif");
				form = new Form();
				form.Size = new Size(250, 200);
				form.Controls.Add(picturebox);
				createForm(0, height - form.Height);
				break;
			case 4:
				createPictureBox("dllsupport.Resources.gowniak.gif");
				form = new Form();
				form.Size = new Size(350, 400);
				form.Controls.Add(picturebox);
				createForm(width / 2 - form.Width / 2, height - form.Height);
				formList.Add(form);
				formFocus = true;
				break;
			}
			generateInt++;
		}
		catch
		{
		}
	}

	private static void createPictureBox(string patch)
	{
		picturebox = new PictureBox();
		picturebox.SizeMode = PictureBoxSizeMode.StretchImage;
		picturebox.Dock = DockStyle.Fill;
		GifAnimated(patch);
	}

	private static void createLinkLabel(string text, Form form, PictureBox pb, Point point)
	{
		LinkLabel linkLabel = new LinkLabel();
		linkLabel.Text = text;
		linkLabel.Size = new Size(200, 20);
		linkLabel.Location = point;
		linkLabel.BackColor = Color.Transparent;
		linkLabel.Click += LinkLabel_Click;
		pb.Controls.Add(linkLabel);
	}

	private static void LinkLabel_Click(object sender, EventArgs e)
	{
		Process.Start(link);
	}

	private static void FormClosingEv(object sender, FormClosingEventArgs e)
	{
		e.Cancel = true;
	}

	public static void createForm(int x, int y)
	{
		form.FormClosing += FormClosingEv;
		form.FormBorderStyle = FormBorderStyle.None;
		form.Name = generateInt.ToString();
		form.Text = generateInt.ToString();
		form.Show();
		form.Location = new Point(x, y);
		form.ShowInTaskbar = false;
		SetWindowPos(form.Handle, HWND_TOPMOST, 0, 0, 0, 0, 3u);
		formList.Add(form);
	}
}
