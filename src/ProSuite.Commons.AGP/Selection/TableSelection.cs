using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Selection;

public abstract class TableSelection : IDisposable
{
	protected TableSelection([NotNull] Table table)
	{
		Table = table ??
		        throw new ArgumentNullException(nameof(table));
	}

	// TODO dispose when done (implement IDisposable)
	[NotNull]
	public Table Table { get; }

	public abstract IEnumerable<long> GetOids();

	public abstract int GetCount();

	public void Dispose()
	{
		Table.Dispose();
	}
}
