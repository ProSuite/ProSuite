using System.Windows.Forms;

namespace ProSuite.Commons.UI.Persistence.WinForms
{
	/// <summary>
	/// Stores form state for xml serialization
	/// </summary>
	public class FormState : IFormState
	{
		private int _width = _undefined;
		private int _height = _undefined;
		private int _left = _undefined;
		private int _top = _undefined;
		private bool _topMost;
		private FormWindowState _state = FormWindowState.Normal;

		private const int _undefined = -1;

		public bool HasSize => (_width != _undefined && _height != _undefined);

		public bool HasLocation => (_left != _undefined && _top != _undefined);

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
