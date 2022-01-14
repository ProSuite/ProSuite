using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	public class CursorImpl : ICursor, IFeatureCursor
	{
		private readonly IEnumerator<IRow> _rowEnumerator;

		public CursorImpl(ITable table, IEnumerable<IRow> rows)
		{
			Fields = table.Fields;
			_rowEnumerator = rows.GetEnumerator();
		}

		IFields ICursor.Fields => Fields;
		IFields IFeatureCursor.Fields => Fields;
		public virtual IFields Fields { get; }

		int ICursor.FindField(string name) => FindField(name);

		int IFeatureCursor.FindField(string name) => FindField(name);

		public virtual int FindField(string name) => Fields.FindField(name);

		IRow ICursor.NextRow() => NextRow();

		IFeature IFeatureCursor.NextFeature() => (IFeature) NextRow();

		public virtual IRow NextRow()
		{
			return _rowEnumerator.MoveNext() ? _rowEnumerator.Current : null;
		}

		void ICursor.UpdateRow(IRow row) => UpdateRow(row);

		void IFeatureCursor.UpdateFeature(IFeature row) => UpdateRow(row);

		public virtual void UpdateRow(IRow row) =>
			throw new NotImplementedException("Implement in derived class");

		void ICursor.DeleteRow() => DeleteRow();

		void IFeatureCursor.DeleteFeature() => DeleteRow();

		public virtual void DeleteRow() =>
			throw new NotImplementedException("Implement in derived class");

		object ICursor.InsertRow(IRowBuffer row) => InsertRow(row);

		object IFeatureCursor.InsertFeature(IFeatureBuffer feature) => InsertRow(feature);

		public virtual object InsertRow(IRowBuffer row) =>
			throw new NotImplementedException("Implement in derived class");

		void ICursor.Flush() => Flush();

		void IFeatureCursor.Flush() => Flush();

		public virtual void Flush() =>
			throw new NotImplementedException("Implement in derived class");
	}
}
