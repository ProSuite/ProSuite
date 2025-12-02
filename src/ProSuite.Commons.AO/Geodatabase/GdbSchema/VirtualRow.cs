using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.GeoDb;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	/// <summary>
	/// template for Feature derived from an implementation of VirtualRow 
	/// </summary>
	public class VirtualFeature : VirtualRow, IFeature
	{
		ITable IFeature.Table => Table;

#if Server11 || ARCGIS_12_0_OR_GREATER
		long IFeature.OID => (int) OID;
#else
		int IFeature.OID => (int) OID;
#endif
	}

	/// <summary>
	/// see VirtualFeature for template for Feature derived from an implementation of VirtualRow 
	/// </summary>
	public class VirtualRow : IObject, IReadOnlyRow
	{
		void IRow.Store() => Store();

		void IObject.Store() => Store();

		public virtual void Store() =>
			throw new NotImplementedException("Implement in derived class");

		void IRow.Delete() => Delete();

		void IObject.Delete() => Delete();

		public virtual void Delete() =>
			throw new NotImplementedException("Implement in derived class");

		object IRow.get_Value(int index) => get_Value(index);

		object IObject.get_Value(int index) => get_Value(index);

		object IRowBuffer.get_Value(int index) => get_Value(index);

		void IRow.set_Value(int index, object value) => set_Value(index, value);

		void IObject.set_Value(int index, object value) => set_Value(index, value);

		void IRowBuffer.set_Value(int index, object value) => set_Value(index, value);

		public virtual object get_Value(int Index) =>
			throw new NotImplementedException("Implement in derived class");

		public virtual void set_Value(int Index, object value) =>
			throw new NotImplementedException("Implement in derived class");

		IFields IRow.Fields => Fields;
		IFields IObject.Fields => Fields;
		IFields IRowBuffer.Fields => Fields;
		public virtual IFields Fields => Table.Fields;

		bool IRow.HasOID => HasOID;
		bool IObject.HasOID => HasOID;

		public virtual bool HasOID =>
			throw new NotImplementedException("Implement in derived class");

#if Server11 || ARCGIS_12_0_OR_GREATER
		long IRow.OID => OID;
		long IObject.OID => OID;
#else
		int IRow.OID => (int) OID;
		int IObject.OID => (int) OID;
#endif

		public virtual long OID =>
			throw new NotImplementedException("Implement in derived class");

		ITable IRow.Table => Table;
		ITable IObject.Table => Table;
		IReadOnlyTable IReadOnlyRow.Table => ReadOnlyTable;

		#region Implementation of IDbRow

		object IDbRow.GetValue(int index)
		{
			return get_Value(index);
		}

		public ITableData DbTable => Table;

		#endregion

		public virtual VirtualTable Table =>
			throw new NotImplementedException("Implement in derived class");

		public virtual IReadOnlyTable ReadOnlyTable => Table;

		IObjectClass IObject.Class => Class;
		public virtual IObjectClass Class => (IObjectClass) Table;

		public virtual IGeometry Shape
		{
			get => throw new NotImplementedException("Implement in derived class");
			set => throw new NotImplementedException("Implement in derived class");
		}

		public virtual IGeometry ShapeCopy => GeometryFactory.Clone(Shape);

		public virtual IEnvelope Extent
		{
			get
			{
				if (Shape != null)
				{
					return Shape.Envelope;
				}

				// To be consistent with AO:
				return new EnvelopeClass();
			}
		}

		public virtual esriFeatureType FeatureType =>
			throw new NotImplementedException("Implement in derived class");
	}
}
