using BarRaider.SdTools;
using BitFaster.Caching.Lru;
using FocusVolumeControl.UI;
using System;
using System.Drawing;

#nullable enable

namespace FocusVolumeControl.AudioSession
{
	public abstract class IconWrapper
	{
		protected static ConcurrentLru<string, string> _iconCache = new ConcurrentLru<string, string>(10);

		public abstract string GetIconData();

		internal const string FallbackIconData = "Images/encoderIcon";

		/// <summary>
		/// Loads one of the plugin's own bundled PNGs (e.g. "Images/encoderIcon", "Images/systemSounds")
		/// and returns it as a base64 data URI, the same format Tools.ImageToBase64 produces for real
		/// per-app icons. Returning the bare relative path directly (as this code used to do) is not
		/// valid image data for the dial's feedback canvas, which is why fallback icons showed up as a
		/// checkerboard even though the underlying PNG file existed and looked fine on disk.
		/// </summary>
		public static string LoadBundledIcon(string relativePathWithoutExtension)
		{
			return _iconCache.GetOrAdd(relativePathWithoutExtension, (key) =>
			{
				try
				{
					var baseDir = AppDomain.CurrentDomain.BaseDirectory;
					var fileName = key.Replace('/', System.IO.Path.DirectorySeparatorChar) + ".png";
					var path = System.IO.Path.Combine(baseDir, fileName);

					using var bitmap = (Bitmap)Bitmap.FromFile(path);
					return Tools.ImageToBase64(bitmap, true);
				}
				catch
				{
					// If even the bundled icon fails to load, fall back to the bare path.
					// Something is very wrong at that point (missing/corrupt install), but at
					// least this keeps the same (pre-existing, non-fatal) behavior as before
					// rather than throwing.
					return key;
				}
			});
		}
	}

	internal class AppxIcon : IconWrapper
	{
		private readonly string _iconPath;

		public AppxIcon(string iconPath)
		{
			_iconPath = iconPath;
		}

		public override string GetIconData()
		{
			if(string.IsNullOrEmpty(_iconPath))
			{
				return LoadBundledIcon(FallbackIconData);
			}

			return _iconCache.GetOrAdd(_iconPath, (key) =>
			{
				var tmp = (Bitmap)Bitmap.FromFile(_iconPath);
				tmp.MakeTransparent();
				return Tools.ImageToBase64(tmp, true);
			});
		}

	}

	internal class NormalIcon : IconWrapper
	{
		private readonly string _iconPath;

		public NormalIcon(string iconPath)
		{
			_iconPath = iconPath;
		}

		public override string GetIconData()
		{
			if(string.IsNullOrEmpty(_iconPath))
			{
				return LoadBundledIcon(FallbackIconData);
			}

			return _iconCache.GetOrAdd(_iconPath, (key) =>
			{
				var tmp = IconExtraction.GetIcon(_iconPath);
				return Tools.ImageToBase64(tmp, true);
			});
		}
	}

	internal class RawIcon : IconWrapper
	{
		private readonly string _data;

		public RawIcon(string name, Func<Bitmap?> getIcon)
		{
			_data = _iconCache.GetOrAdd(name, (key) =>
			{
				var icon = getIcon();
				if (icon == null)
				{
					return LoadBundledIcon(FallbackIconData);
				}

				if (icon.Height < 48 && icon.Width < 48)
				{
					using var newImage = new Bitmap(48, 48);
					newImage.MakeTransparent();
					using var graphics = Graphics.FromImage(newImage);

					graphics.DrawImage(icon, 4, 4, 40, 40);


					return Tools.ImageToBase64(newImage, true);
				}
				else
				{
					return Tools.ImageToBase64(icon, true);
				}
			});
		}

		public override string GetIconData() => _data;
	}

}
#nullable restore
