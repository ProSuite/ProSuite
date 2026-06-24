using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Selection;

public abstract class TableSelection : IDisposable
{
	private Table _table;

	protected TableSelection([NotNull] Table table)
	{
		_table = table ?? throw new ArgumentNullException(nameof(table));
	}

	[NotNull]
	public Table Table => _table ?? throw new ObjectDisposedException(GetType().Name);

	public abstract IEnumerable<long> GetOids();

	public abstract int GetCount();

	public void Dispose()
	{
		_table?.Dispose();
		_table = null;
	}
}
