using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Processing.Utils;

namespace ProSuite.Processing.Evaluation
{
	// Migration to Pro SDK: use Processing.Utils.RowValues instead
	//public class RowValues : INamedValues
	//{
	//	private readonly IRowValues _row;
	//	private readonly FindFieldCache _findFieldCache;
	//	private readonly string _datasetName;

	//	private static readonly HashSet<string> _datasetNameFieldNames =
	//		new HashSet<string>(new[] {"_DATASET", "_DATASET_"},
	//		                    StringComparer.OrdinalIgnoreCase);

	//	public RowValues([NotNull] IRowValues row, FindFieldCache findFieldCache = null,
	//	                 string datasetName = null)
	//	{
	//		Assert.ArgumentNotNull(row, nameof(row));

	//		_row = row;
	//		_findFieldCache = findFieldCache;
	//		_datasetName = datasetName;
	//	}

	//	public bool Exists(string name)
	//	{
	//		if (name == null)
	//		{
	//			return false;
	//		}

	//		int index = FindField(name);

	//		if (index >= 0)
	//		{
	//			return true;
	//		}

	//		return _datasetNameFieldNames.Contains(name) && _datasetName != null;
	//	}

	//	public object GetValue(string name)
	//	{
	//		if (name == null)
	//		{
	//			return null;
	//		}

	//		int index = FindField(name);

	//		if (index >= 0)
	//		{
	//			return _row[index];
	//		}

	//		return _datasetNameFieldNames.Contains(name) ? _datasetName : null;
	//	}

	//	private int FindField(string name)
	//	{
	//		if (name == null)
	//		{
	//			return -1;
	//		}

	//		return _findFieldCache?.GetFieldIndex(_row, name) ?? _row.FindField(name);
	//	}
	//}
}
