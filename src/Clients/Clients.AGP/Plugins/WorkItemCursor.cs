using System;
using System.Collections.Generic;
using ArcGIS.Core.Data.PluginDatastore;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.PluginDatasource
{
	public class WorkItemCursor : PluginCursorTemplate, IDisposable
	{
		private readonly IEnumerator<object[]> _enumerator;

		public WorkItemCursor([NotNull] IEnumerable<object[]> items)
		{
			if (items == null)
				throw new ArgumentNullException(nameof(items));

			_enumerator = items.GetEnumerator();
		}

		public override PluginRow GetCurrentRow()
		{
			return new PluginRow(_enumerator.Current);
		}

		public override bool MoveNext()
		{
			return _enumerator.MoveNext();
		}

		public void Dispose()
		{
			_enumerator?.Dispose();
		}
	}
}
