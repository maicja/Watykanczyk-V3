using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Runtime.CompilerServices;

namespace dllsupport.Properties;

[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
[DebuggerNonUserCode]
[CompilerGenerated]
internal class Resources
{
	private static ResourceManager resourceMan;

	private static CultureInfo resourceCulture;

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	internal static ResourceManager ResourceManager
	{
		get
		{
			if (resourceMan == null)
			{
				ResourceManager resourceManager = new ResourceManager("dllsupport.Properties.Resources", typeof(Resources).Assembly);
				resourceMan = resourceManager;
			}
			return resourceMan;
		}
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	internal static CultureInfo Culture
	{
		get
		{
			return resourceCulture;
		}
		set
		{
			resourceCulture = value;
		}
	}

	internal static Bitmap gowniak
	{
		get
		{
			object @object = ResourceManager.GetObject("gowniak", resourceCulture);
			return (Bitmap)@object;
		}
	}

	internal static UnmanagedMemoryStream Jan_DJ_2___GabBarka => ResourceManager.GetStream("Jan_DJ_2___GabBarka", resourceCulture);

	internal static Bitmap masa
	{
		get
		{
			object @object = ResourceManager.GetObject("masa", resourceCulture);
			return (Bitmap)@object;
		}
	}

	internal static Bitmap papagif
	{
		get
		{
			object @object = ResourceManager.GetObject("papagif", resourceCulture);
			return (Bitmap)@object;
		}
	}

	internal static Icon papieszIkonka
	{
		get
		{
			object @object = ResourceManager.GetObject("papieszIkonka", resourceCulture);
			return (Icon)@object;
		}
	}

	internal static Bitmap papież
	{
		get
		{
			object @object = ResourceManager.GetObject("papież", resourceCulture);
			return (Bitmap)@object;
		}
	}

	internal static Bitmap papiezinba
	{
		get
		{
			object @object = ResourceManager.GetObject("papiezinba", resourceCulture);
			return (Bitmap)@object;
		}
	}

	internal static Bitmap papiezkreci
	{
		get
		{
			object @object = ResourceManager.GetObject("papiezkreci", resourceCulture);
			return (Bitmap)@object;
		}
	}

	internal static Bitmap papiezlata
	{
		get
		{
			object @object = ResourceManager.GetObject("papiezlata", resourceCulture);
			return (Bitmap)@object;
		}
	}

	internal static Bitmap papiezrower
	{
		get
		{
			object @object = ResourceManager.GetObject("papiezrower", resourceCulture);
			return (Bitmap)@object;
		}
	}

	internal Resources()
	{
	}
}
