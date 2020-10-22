using System;
using System.Collections.Generic;
using System.Data;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	/// <summary>
	/// Class to filter rows
	/// Remark: This class is mainly a clone of QueryFilterHelper
	/// please report relevant changes back to that class
	/// </summary>
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
		[CLSCompliant(false)]
		public static FilterHelper Create([NotNull] ITable table,
		                                  [CanBeNull] string constraint)
		{
			var helper = new FilterHelper();
			if (constraint == null || constraint.Trim() == "")
			{
				helper._constraintView = null;
				return helper;
			}

			helper._sFields = null;

			var fields = new List<string>();
			if (table.HasOID)
			{
				string sOID = table.OIDFieldName.ToUpper();
				fields.Add(sOID);
			}

			string[] sParts = constraint.Split();

			foreach (string sPart in sParts)
			{
				string[] sTests = sPart.Split('=', ',', '(', ')');

				foreach (string sTest in sTests)
				{
					if (table.FindField(sTest) >= 0)
					{
						string sUpper = sTest.ToUpper();
						if (fields.Contains(sUpper) == false)
						{
							fields.Add(sUpper);
						}
					}
				}
			}

			var dataTable = new DataTable(((IDataset) table).Name);
			// dataTable.CaseSensitive = true; TODO allow controlling case-sensitivity

			int nFields = fields.Count;
			helper._fieldIdx = new int[nFields];
			int iField = 0;
			foreach (string sField in fields)
			{
				dataTable.Columns.Add(sField, typeof(object));
				helper._fieldIdx[iField] = table.FindField(sField);

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

			helper._constraintView = new DataView(dataTable) {RowFilter = constraint};

			return helper;
		}

		[CLSCompliant(false)]
		public bool Check(IRow row)
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
			table.Clear();
		}

		private void Add(IRow row)
		{
			if (_constraintView != null)
			{
				DataTable table = _constraintView.Table;
				table.Rows.Add(New(row));
			}
		}

		private DataRow New(IRow row)
		{
			if (_constraintView != null)
			{
				DataTable table = _constraintView.Table;
				DataRow dataRow = table.NewRow();

				for (int iField = 0; iField < _fieldIdx.Count; iField++)
				{
					dataRow[iField] = row.Value[_fieldIdx[iField]];
				}

				return dataRow;
			}

			return null;
		}
	}
}
