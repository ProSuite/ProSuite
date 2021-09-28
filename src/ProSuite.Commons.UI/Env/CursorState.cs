using System.Collections.Generic;

namespace ProSuite.Commons.UI.Env
{
	public class CursorState
	{
		private readonly int _x;
		private readonly int _y;

		private List<object> _extensions;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="CursorState"/> class.
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		public CursorState(int x, int y)
		{
			_x = x;
			_y = y;
		}

		#endregion

		public int X => _x;

		public int Y => _y;

		public void AddExtension(object extension)
		{
			if (_extensions == null)
			{
				_extensions = new List<object>();
			}

			_extensions.Add(extension);
		}

		/// <summary>
		/// Gets the first extension of a specific type.
		/// </summary>
		public T GetExtension<T>()
		{
			if (_extensions == null)
			{
				return default;
			}

			return (T) _extensions.Find(obj => obj is T);
		}
	}
}
