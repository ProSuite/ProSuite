using ESRI.ArcGIS.Geodatabase;
using System;
using System.Collections.Generic;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	public class CursorImpl : ICursor, IFeatureCursor
	{
		private readonly IEnumerator<IRow> _rowEnumerator;

		public CursorImpl(ITable table, IEnumerable<IRow> rows)
		{
			VirtualFields = table.Fields;
			_rowEnumerator = rows.GetEnumerator();
		}

		IFields ICursor.Fields => VirtualFields;
		IFields IFeatureCursor.Fields => VirtualFields;
		protected virtual IFields VirtualFields { get; }

		int ICursor.FindField(string name) => VirtualFindField(name);

		int IFeatureCursor.FindField(string name) => VirtualFindField(name);

		protected virtual int VirtualFindField(string name) => VirtualFields.FindField(name);

		IRow ICursor.NextRow() => VirtualNextRow();

		IFeature IFeatureCursor.NextFeature() => (IFeature)VirtualNextRow();

		protected virtual IRow VirtualNextRow()
		{
			return _rowEnumerator.MoveNext() ? _rowEnumerator.Current : null;
		}

		void ICursor.UpdateRow(IRow row) => VirtualUpdateRow(row);

		void IFeatureCursor.UpdateFeature(IFeature row) => VirtualUpdateRow(row);

		protected virtual void VirtualUpdateRow(IRow row) =>
			throw new NotImplementedException("Implement in derived class");

		void ICursor.DeleteRow() => VirtualDeleteRow();

		void IFeatureCursor.DeleteFeature() => VirtualDeleteRow();

		protected virtual void VirtualDeleteRow() =>
			throw new NotImplementedException("Implement in derived class");

		object ICursor.InsertRow(IRowBuffer row) => VirtualInsertRow(row);

		object IFeatureCursor.InsertFeature(IFeatureBuffer feature) => VirtualInsertRow(feature);

		protected virtual object VirtualInsertRow(IRowBuffer row) =>
			throw new NotImplementedException("Implement in derived class");

		void ICursor.Flush() => VirtualFlush();

		void IFeatureCursor.Flush() => VirtualFlush();

		protected virtual void VirtualFlush() =>
			throw new NotImplementedException("Implement in derived class");
	}
}
