using System.Collections.Generic;
using System.Data;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;

namespace ProSuite.Commons.AGP.Core.Geodatabase;

public class FilterHelper
{
	private DataView _constraintView;

	private IList<int> _fieldIdx;

	private string _sFields;

	private FilterHelper() { }

	public string Fields => _sFields;

	/// <summary>
	/// Create a filter to check if rows belonging to 'table' fulfill 'constraint'
	/// </summary>
	/// <param name="table">Table, which rows should be checked</param>
	/// <param name="constraint">constraint to fulfill</param>
	/// <returns>FilterHelper instance, use 'instance.Check(row)' to check if a row fulfills the constraint</returns>
	public static FilterHelper Create([NotNull] Table table,
									  [CanBeNull] string constraint)
	{
		var helper = new FilterHelper();
		if (constraint == null || constraint.Trim() == "")
		{
			helper._constraintView = null;
			return helper;
		}

		helper._sFields = null;

		using TableDefinition definition = table.GetDefinition();

		string oidFieldName = definition.GetObjectIDField();

		var fields = new List<string>();
		if (!string.IsNullOrEmpty(oidFieldName))
		{
			string sOID = oidFieldName.ToUpper();
			fields.Add(sOID);
		}

		string[] sParts = constraint.Split();

		foreach (string sPart in sParts)
		{
			string[] sTests = sPart.Split('=', ',', '(', ')');

			foreach (string sTest in sTests)
			{
				if (definition.FindField(sTest) >= 0)
				{
					string sUpper = sTest.ToUpper();
					if (fields.Contains(sUpper) == false)
					{
						fields.Add(sUpper);
					}
				}
			}
		}

		var dataTable = new DataTable(table.GetName());
		// dataTable.CaseSensitive = true; TODO allow controlling case-sensitivity

		int nFields = fields.Count;
		helper._fieldIdx = new int[nFields];
		int iField = 0;
		foreach (string sField in fields)
		{
			dataTable.Columns.Add(sField, typeof(object));
			helper._fieldIdx[iField] = definition.FindField(sField);

			if (helper._sFields == null)
			{
				helper._sFields = sField;
			}
			else
			{
				helper._sFields += "," + sField;
			}

			iField++;
		}

		helper._constraintView = new DataView(dataTable) { RowFilter = constraint };

		return helper;
	}

	public bool Check(Row row)
	{
		if (_constraintView == null)
		{
			return true;
		}

		ClearData();
		Add(row);

		return _constraintView.Count == 1;
	}

	private void ClearData()
	{
		DataTable table = _constraintView.Table;
		Assert.NotNull(table);

		table.Clear();
	}

	private void Add(Row row)
	{
		if (_constraintView != null)
		{
			DataTable table = _constraintView.Table;
			Assert.NotNull(table);

			table.Rows.Add(New(row.RowValues()));
		}
	}

	private DataRow New(IRowValues row)
	{
		if (_constraintView != null)
		{
			DataTable table = _constraintView.Table;
			Assert.NotNull(table);

			DataRow dataRow = table.NewRow();

			for (int iField = 0; iField < _fieldIdx.Count; iField++)
			{
				dataRow[iField] = row[_fieldIdx[iField]];
			}

			return dataRow;
		}

		return null;
	}
}
