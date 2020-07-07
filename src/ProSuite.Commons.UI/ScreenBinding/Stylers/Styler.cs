using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.ScreenBinding.Stylers
{
	public class Styler
	{
		public static readonly Styler Default = new Styler();

		private readonly List<IControlStyler> _stylers = new List<IControlStyler>();

		public void ClearAll()
		{
			_stylers.Clear();
		}

		public void UseStyleFor<T>([NotNull] Action<T> action)
		{
			IControlStyler styler = new ControlStyler<T>(action);
			_stylers.Add(styler);
		}

		public void ApplyStyle([NotNull] object control)
		{
			foreach (IControlStyler styler in _stylers)
			{
				styler.Style(control);
			}
		}

		#region Nested type: ControlStyler

		internal class ControlStyler<T> : IControlStyler
		{
			[NotNull] private readonly Action<T> _styleAction;

			internal ControlStyler([NotNull] Action<T> styleAction)
			{
				_styleAction = styleAction;
			}

			#region IControlStyler Members

			void IControlStyler.Style(object control)
			{
				if (control is T)
				{
					_styleAction((T) control);
				}
			}

			#endregion
		}

		#endregion

		#region Nested type: IControlStyler

		internal interface IControlStyler
		{
			void Style([NotNull] object control);
		}

		#endregion
	}
}
