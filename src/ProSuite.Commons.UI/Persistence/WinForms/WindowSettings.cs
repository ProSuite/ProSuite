using System.Windows.Forms;

namespace ProSuite.Commons.UI.Persistence.WinForms
{
	/// <summary>
	/// Stores window geometry settings for xml serialization
	/// </summary>
	public class WindowSettings
	{
		private int _width;
		private int _height;
		private int _left;
		private int _top;
		private bool _topMost;
		private FormWindowState _state;

		private static readonly int _undefined = -1;

		#region Constructors

		public WindowSettings()
		{
			_width = _undefined;
			_height = _undefined;
			_left = _undefined;
			_top = _undefined;
			_topMost = false;

			_state = FormWindowState.Normal;
		}

		#endregion

		public bool HasSize => _width != _undefined && _height != _undefined;

		public bool HasLocation => _left != _undefined && _top != _undefined;

		public int Width
		{
			get { return _width; }
			set { _width = value; }
		}

		public int Height
		{
			get { return _height; }
			set { _height = value; }
		}

		public int Left
		{
			get { return _left; }
			set { _left = value; }
		}

		public int Top
		{
			get { return _top; }
			set { _top = value; }
		}

		public bool TopMost
		{
			get { return _topMost; }
			set { _topMost = value; }
		}

		public FormWindowState WindowState
		{
			get { return _state; }
			set { _state = value; }
		}
	}
}
