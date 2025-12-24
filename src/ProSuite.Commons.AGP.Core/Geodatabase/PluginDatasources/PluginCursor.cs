using System;
using System.Collections.Generic;
using ArcGIS.Core.Data.PluginDatastore;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Core.Geodatabase.PluginDatasources;

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
		try
		{
			return _enumerator.MoveNext();
		}
		catch (Exception ex)
		{
			_msg.Debug($"Error getting next feature ({ex.GetType().Name}): {ex.Message}", ex);
			throw;
		}
	}
}
