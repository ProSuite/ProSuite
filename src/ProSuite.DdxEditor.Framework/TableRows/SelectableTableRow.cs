using System.ComponentModel;
using ProSuite.Commons.UI.Finder;

namespace ProSuite.DdxEditor.Framework.TableRows
{
	public abstract class SelectableTableRow : ISelectable
	{
		#region ISelectable Members

		[Browsable(false)]
		public bool Selectable { get; set; } = true;

		#endregion

		// TODO don't do this here, because
		// - not all table rows necessarily derive from this class
		// - it's not part of the semantics of a selectable table row

		// Todo: use checked /unchecked pictures instead of GetBooleanAsString
		/// <summary>
		/// Helper for representing booleans
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		protected static string GetBooleanAsString(bool value)
		{
			return value
				       ? "Yes"
				       : "No";
		}
	}
}
