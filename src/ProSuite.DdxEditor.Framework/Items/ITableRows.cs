using System.Collections.Generic;

namespace ProSuite.DdxEditor.Framework.Items
{
	public interface ITableRows<T> where T : class
	{
		IEnumerable<T> GetTableRows();
	}
}
