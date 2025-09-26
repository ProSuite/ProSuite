using ArcGIS.Core.Data.Exceptions;
using ArcGIS.Core.Data.PluginDatastore;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using System;
using System.Collections.Generic;

namespace ProSuite.Commons.AGP.Core.Geodatabase.PluginDatasources
{
	/// <summary>
	/// Generic cursor for plugin data sources.
	/// </summary>
	public class PluginCursor : PluginCursorTemplate
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly IEnumerator<object[]> _enumerator;

		public PluginCursor(IEnumerable<object[]> rows)
		{
			Assert.ArgumentNotNull(rows, nameof(rows));

			_enumerator = rows.GetEnumerator();
		}

		public override PluginRow GetCurrentRow()
		{
			if (_enumerator.Current == null)
			{
				return null;
			}

			return new PluginRow(_enumerator.Current);
		}

		public override bool MoveNext()
		{
			bool result = false;

			try
			{
				result = _enumerator.MoveNext();
			}
			catch (GeodatabaseCursorException gdbException)
			{
				_msg.Error($"Error getting next feature: {gdbException.Message}", gdbException);
				throw;
			}
			catch (Exception ex)
			{
				_msg.Error($"Error getting next feature: {ex.Message}", ex);
			}

			return result;
		}
	}
}
