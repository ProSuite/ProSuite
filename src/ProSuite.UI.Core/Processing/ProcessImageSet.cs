using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.UI.Core.Processing
{
	public class ProcessImageSet
	{
		[NotNull]
		public Bitmap Icon { get; private set; }

		[CanBeNull]
		public Cursor Cursor { get; private set; }

		[CanBeNull]
		public Cursor ShiftCursor { get; private set; }

		public ProcessImageSet([CanBeNull] Bitmap icon, [CanBeNull] Cursor cursor = null,
		                       [CanBeNull] Cursor shiftCursor = null)
		{
			Icon = icon ?? new Bitmap(16, 16);
			Cursor = cursor;
			ShiftCursor = shiftCursor;
		}
	}
}
