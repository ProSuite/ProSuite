using System.IO;
using System.Windows.Input;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing
{
	public static class ToolUtils
	{
		/// <summary>
		/// Loads a cursor from a byte array
		/// </summary>
		/// <param name="bytes">The byte array</param>
		/// <returns>Cursor instance</returns>
		[NotNull]
		public static Cursor GetCursor([NotNull] byte[] bytes)
		{
			Assert.ArgumentNotNull(bytes, nameof(bytes));

			return new Cursor(new MemoryStream(bytes));
		}
	}
}
