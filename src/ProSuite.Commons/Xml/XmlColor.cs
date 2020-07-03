using System;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.Xml
{
	/// <summary>
	/// Xml-serializable color
	/// </summary>
	public class XmlColor
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private string _knownColor;
		private string _colorName;
		private int _red;
		private int _green;
		private int _blue;

		private Color _color;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlColor"/> class.
		/// </summary>
		/// <remarks>Required for xml desrialization</remarks>
		public XmlColor() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlColor"/> class.
		/// </summary>
		/// <param name="red">The red.</param>
		/// <param name="green">The green.</param>
		/// <param name="blue">The blue.</param>
		public XmlColor(int red, int green, int blue)
		{
			_red = red;
			_green = green;
			_blue = blue;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlColor"/> class.
		/// </summary>
		/// <param name="colorName">The name.</param>
		public XmlColor([NotNull] string colorName)
		{
			Assert.ArgumentNotNullOrEmpty(colorName, nameof(colorName));

			_colorName = colorName;
		}

		public XmlColor(KnownColor knownColor)
		{
			_knownColor = knownColor.ToString();
		}

		public XmlColor(Color color)
		{
			_color = color;
		}

		#endregion

		[XmlAttribute("system")]
		public string KnownColor
		{
			get { return _knownColor; }
			set
			{
				_knownColor = value;
				_color = Color.Empty;
			}
		}

		[XmlAttribute("name")]
		public string ColorName
		{
			get { return _colorName; }
			set
			{
				_colorName = value;
				_color = Color.Empty;
			}
		}

		[XmlAttribute("red")]
		[DefaultValue(0)]
		public int Red
		{
			get { return _red; }
			set
			{
				_red = value;
				_color = Color.Empty;
			}
		}

		[XmlAttribute("green")]
		[DefaultValue(0)]
		public int Green
		{
			get { return _green; }
			set
			{
				_green = value;
				_color = Color.Empty;
			}
		}

		[XmlAttribute("blue")]
		[DefaultValue(0)]
		public int Blue
		{
			get { return _blue; }
			set
			{
				_blue = value;
				_color = Color.Empty;
			}
		}

		[XmlIgnore]
		public Color Color
		{
			get
			{
				if (_color.IsEmpty)
				{
					_color = CreateColor();
				}

				return _color;
			}
		}

		private Color CreateColor()
		{
			Color defaultColor = Color.White;

			if (! string.IsNullOrEmpty(_knownColor))
			{
				try
				{
					return Color.FromKnownColor(
						(KnownColor) Enum.Parse(typeof(KnownColor), _knownColor));
				}
				catch (Exception)
				{
					_msg.ErrorFormat("Unknown 'known color' value: {0})", _knownColor);
					return defaultColor;
				}
			}

			if (! string.IsNullOrEmpty(_colorName))
			{
				try
				{
					return Color.FromName(_colorName);
				}
				catch (Exception)
				{
					_msg.ErrorFormat("Unknown color name: {0})", _colorName);
					return defaultColor;
				}
			}

			try
			{
				return Color.FromArgb(_red, _green, _blue);
			}
			catch (Exception e)
			{
				_msg.ErrorFormat(
					"Error creating color from RGB values {0},{1},{2} ({3})",
					_red, _green, _blue, e.Message);
				return defaultColor;
			}
		}
	}
}