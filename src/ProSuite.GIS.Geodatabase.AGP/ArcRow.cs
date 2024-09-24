using System;
using ArcGIS.Core;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Logging;
using ProSuite.GIS.Geodatabase.API;
using ProSuite.GIS.Geometry.AGP;
using ProSuite.GIS.Geometry.API;

namespace ProSuite.GIS.Geodatabase.AGP
{
	public class ArcRow : IObject
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private Row _proRow;
		private readonly ITable _parentTable;

		public static ArcRow Create(Row proRow, ITable parentTable)
		{
			Assert.NotNull(proRow, "No row provided");

			var result = proRow is Feature feature
				             ? new ArcFeature(feature, (IFeatureClass) parentTable)
				             : new ArcRow(proRow, parentTable);

			return result;
		}

		protected ArcRow(Row proRow, ITable parentTable)
		{
			_proRow = proRow;
			OID = proRow.GetObjectID();

			_parentTable = parentTable;
		}

		public Row ProRow => _proRow;

		#region Implementation of IRowBuffer

		public virtual object get_Value(int index)
		{
			object result = null;

			TryOrRefreshRow<Row>(r => result = r[index]);

			return result;
		}

		public void set_Value(int index, object value)
		{
			TryOrRefreshRow<Row>(r => r[index] = value);
		}

		public IFields Fields => _parentTable.Fields;

		#endregion

		#region Implementation of IRow

		// TODO: Discuss this
		//public bool HasOID => _proRow.HasOID;
		public bool HasOID => OID >= 0;

		public long OID { get; }

		public ITable Table => _parentTable;

		public void Store()
		{
			TryOrRefreshRow<Row>(r => r.Store());
		}

		public void Delete()
		{
			TryOrRefreshRow<Row>(r => r.Delete());
		}

		#endregion

		#region Implementation of IObject

		public IObjectClass Class => (IObjectClass) _parentTable;

		#endregion

		protected bool IsDisposed
		{
			get
			{
				try
				{
					_proRow.GetObjectID();
					return false;
				}
				catch (ObjectDisposedException)
				{
					return true;
				}
				catch (ObjectDisconnectedException)
				{
					return true;
				}
			}
		}

		protected void TryOrRefreshRow<T>(Action<T> action) where T : Row
		{
			// This is a workaround because the original Pro row could be disposed.
			// TODO: A more deterministic approach, such as
			// delta.RefreshAll(_workspaceContext.FeatureWorkspace);
			// In the OperationCompleting event (see original rule engine).
			if (IsDisposed)
			{
				ArcRow refreshedArcRow = _parentTable.GetRow(OID) as ArcRow;

				_proRow = refreshedArcRow?.ProRow;
			}

			try
			{
				action((T) _proRow);
			}
			catch (Exception e)
			{
				_msg.Debug("Row operation failed", e);
				throw;
			}
		}
	}

	public class ArcFeature : ArcRow, IFeature
	{
		private readonly Feature _proFeature;

		public ArcFeature(Feature proFeature, IFeatureClass parentClass)
			: base(proFeature, parentClass as ITable)
		{
			_proFeature = proFeature;
		}

		protected virtual ArcGIS.Core.Geometry.Geometry GetProGeometry()
		{
			ArcGIS.Core.Geometry.Geometry result = null;

			TryOrRefreshRow<Feature>(r => result = r.GetShape());

			return result;
		}

		#region Implementation of IFeature

		public IGeometry ShapeCopy
		{
			get
			{
				ArcGIS.Core.Geometry.Geometry clone = GetProGeometry().Clone();
				return new ArcGeometry(clone);
			}
		}

		public IGeometry Shape
		{
			get
			{
				ArcGIS.Core.Geometry.Geometry proGeometry = GetProGeometry();

				if (proGeometry is Polygon polygon)
				{
					return new ArcPolygon(polygon);
				}

				if (proGeometry is Polyline polyline)
				{
					return new ArcPolycurve(polyline);
				}

				if (proGeometry is Multipoint multipoint)
				{
					return new ArcMultipoint(multipoint);
				}

				if (proGeometry is Multipatch multipatch)
				{
					return new ArcMultipatch(multipatch);
				}

				if (proGeometry is MapPoint point)
				{
					return new ArcPoint(point);
				}

				return new ArcGeometry(proGeometry);
			}
			set
			{
				ArcGIS.Core.Geometry.Geometry proGeometry =
					((ArcGeometry) value).ProGeometry;

				TryOrRefreshRow<Feature>(r => r.SetShape(proGeometry));
			}
		}

		public IEnvelope Extent
		{
			get
			{
				ArcGIS.Core.Geometry.Geometry geometry = null;

				TryOrRefreshRow<Feature>(r => geometry = _proFeature.GetShape());

				return new ArcEnvelope(geometry.Extent);
			}
		}

		#endregion
	}
}
