using System;
using ArcGIS.Core;
using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Logging;
using ProSuite.GIS.Geodatabase.API;
using ProSuite.GIS.Geometry.AGP;
using ProSuite.GIS.Geometry.API;
using Subtype = ArcGIS.Core.Data.Subtype;

namespace ProSuite.GIS.Geodatabase.AGP
{
	public class ArcRow : IObject, IRowSubtypes
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly ITable _parentTable;

		private SimpleValueList _cachedValues;
		private Row _proRow;

		public static ArcRow Create(Row proRow, ITable parentTable)
		{
			Assert.NotNull(proRow, "No row provided");

			var result = proRow is Feature feature
				             ? new ArcFeature(feature, (IFeatureClass) parentTable)
				             : new ArcRow(proRow, parentTable);

			return result;
		}

		public static Func<ArcGIS.Core.Geometry.Geometry, IGeometry> GeometryFactory { get; set; } =
			ArcGeometry.Create;

		protected ArcRow([CanBeNull] Row proRow, [NotNull] ITable parentTable)
		{
			ProRow = proRow;
			OID = proRow?.GetObjectID() ?? -1;

			_parentTable = parentTable;
		}

		public Row ProRow
		{
			get => _proRow;
			protected set
			{
				_proRow = value;
				InvalidateCache();
			}
		}

		/// <summary>
		/// Caches all field values from the underlying row for improved performance.
		/// </summary>
		/// <remarks>
		/// This method should be called when a row will be accessed frequently,
		/// especially in non-CIM threads where direct access to Pro SDK objects isn't available.
		/// </remarks>
		public void CacheValues()
		{
			if (_cachedValues != null)
			{
				return; // Values are already cached
			}

			try
			{
				var fields = ProRow.GetFields();
				int fieldCount = fields.Count;
				_cachedValues = new SimpleValueList(fieldCount);

				// Populate the cache for all fields
				for (int i = 0; i < fieldCount; i++)
				{
					try
					{
						object value = ProRow[i];
						_cachedValues[i] = value;
					}
					catch (Exception ex)
					{
						// Log the error but continue caching other fields
						_msg.Debug($"Error caching field at index {i}: {ex.Message}", ex);
						throw;
					}
				}
			}
			catch (Exception ex)
			{
				_msg.Warn($"Failed to cache row values for row {OID}: {ex.Message}", ex);
				_cachedValues = null;
			}
		}

		/// <summary>
		/// Invalidates and re-reads the cached values.
		/// </summary>
		public void InvalidateCache()
		{
			_cachedValues = null;
			CacheValues();
		}

		#region Implementation of IRowBuffer

		public virtual object get_Value(int index)
		{
			if (TryGetCachedValue(index, out object value))
			{
				return value;
			}

			object result = null;

			TryOrRefreshRow<Row>(r => result = r[index]);

			return result ?? DBNull.Value;
		}

		protected bool TryGetCachedValue(int index, out object value)
		{
			// Use cached values if available
			if (_cachedValues != null && index >= 0 && index < _cachedValues.Count)
			{
				value = _cachedValues[index];
				return true;
			}

			value = null;

			return false;
		}

		public virtual void set_Value(int index, object value)
		{
			TryOrRefreshRow<Row>(r => r[index] = value);

			// Update the cache if it's being used
			if (_cachedValues != null && index >= 0 && index < _cachedValues.Count)
			{
				_cachedValues[index] = value;
			}
		}

		public IFields Fields => _parentTable.Fields;

		#endregion

		#region Implementation of IRow

		// TODO: Discuss this
		//public bool HasOID => _proRow.HasOID;
		public bool HasOID => OID >= 0;

		public long OID { get; protected set; }

		public ITable Table => _parentTable;

		public virtual void Store()
		{
			OnStoring();

			TryOrRefreshRow<Row>(r => r.Store());

			// After storing, refresh the cache if it was being used before
			if (_cachedValues != null)
			{
				InvalidateCache();
			}
		}

		protected virtual void OnStoring() { }

		public void Delete()
		{
			TryOrRefreshRow<Row>(r => r.Delete());
		}

		public object NativeImplementation => ProRow;

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
					ProRow.GetObjectID();
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

				ProRow = refreshedArcRow?.ProRow;
			}

			try
			{
				action((T) ProRow);
			}
			catch (Exception e)
			{
				_msg.Debug("Row operation failed", e);
				throw;
			}
		}

		#region Implementation of IRowSubtypes

		public int SubtypeCode
		{
			get
			{
				int? subtypeCode =
					GdbObjectUtils.GetSubtypeCode(ProRow);

				return subtypeCode ?? -1;
			}
			set => GdbObjectUtils.SetSubtypeCode(ProRow, value);
		}

		public void InitDefaultValues()
		{
			Subtype subtype =
				GdbObjectUtils.GetSubtype(ProRow);

			ArcTable arcTable = (ArcTable) _parentTable;

			GdbObjectUtils.SetNullValuesToGdbDefault(
				ProRow, arcTable.ProTableDefinition, subtype);
		}

		#endregion

		#region Equality members

		protected bool Equals(ArcRow other)
		{
			return Equals(_parentTable, other._parentTable) && OID == other.OID;
		}

		public override bool Equals(object obj)
		{
			if (obj is null)
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != GetType())
			{
				return false;
			}

			return Equals((ArcRow) obj);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(_parentTable, OID);
		}

		#endregion
	}

	public class ArcFeature : ArcRow, IFeature, IFeatureChanges
	{
		private readonly Feature _proFeature;
		private readonly IFeatureClass _parentFeatureClass;
		private IGeometry _mutableGeometry;

		public ArcFeature(Feature proFeature, IFeatureClass parentClass)
			: base(proFeature, parentClass as ITable)
		{
			_proFeature = proFeature;
			_parentFeatureClass = parentClass;
		}

		protected virtual ArcGIS.Core.Geometry.Geometry GetProGeometry(IGeometry fromShape)
		{
			ArcGIS.Core.Geometry.Geometry result;

			if (fromShape is ArcGeometry arcGeometry)
			{
				result = arcGeometry.ProGeometry;
			}
			else if (fromShape is IMutableGeometry mutable)
			{
				result = (ArcGIS.Core.Geometry.Geometry) mutable.ToNativeImplementation();
			}
			else
			{
				result = ArcGeometryUtils.CreateProGeometry(fromShape);
			}

			return result;
		}

		#region Implementation of IFeature

		public new IFeatureClass Class => (IFeatureClass) base.Class;

		public IGeometry ShapeCopy
		{
			get
			{
				if (_mutableGeometry != null)
				{
					return _mutableGeometry.Clone();
				}

				ArcGIS.Core.Geometry.Geometry proGeometry = GetProGeometry(Shape);
				ArcGIS.Core.Geometry.Geometry clone = proGeometry.Clone();
				return GeometryFactory(clone);
			}
		}

		public IGeometry Shape
		{
			get
			{
				if (_mutableGeometry != null)
				{
					return _mutableGeometry;
				}

				int shapeFieldIdx = Fields.FindField(Class.ShapeFieldName);

				ArcGIS.Core.Geometry.Geometry proGeometry = null;

				if (TryGetCachedValue(shapeFieldIdx, out object cachedValue))
				{
					proGeometry = (ArcGIS.Core.Geometry.Geometry) cachedValue;
				}
				else
				{
					TryOrRefreshRow<Feature>(r => proGeometry = r.GetShape());
				}

				return GeometryFactory(proGeometry);
			}
			set
			{
				if (value is IMutableGeometry)
				{
					_mutableGeometry = value;
				}
				else if (value is ArcGeometry arcGeometry)
				{
					ArcGIS.Core.Geometry.Geometry proGeometry =
						arcGeometry.ProGeometry;

					TryOrRefreshRow<Feature>(r => r.SetShape(proGeometry));
				}
				else
				{
					throw new NotSupportedException("Unsupported geometry implementation");
				}
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

		#region Implementation of IFeatureChanges

		public bool ShapeChanged
		{
			get
			{
				int shapeFieldIdx =
					_parentFeatureClass.FindField(_parentFeatureClass.ShapeFieldName);

				return _proFeature.HasValueChanged(shapeFieldIdx);
			}
		}

		public IGeometry OriginalShape
		{
			get
			{
				int shapeFieldIdx =
					_parentFeatureClass.FindField(_parentFeatureClass.ShapeFieldName);

				ArcGIS.Core.Geometry.Geometry originalProGeometry =
					(ArcGIS.Core.Geometry.Geometry) _proFeature.GetOriginalValue(shapeFieldIdx);

				return GeometryFactory(originalProGeometry);
			}
		}

		#endregion

		#region Overrides of ArcRow

		protected override void OnStoring()
		{
			if (_mutableGeometry == null)
			{
				return;
			}

			ArcGIS.Core.Geometry.Geometry newGeometry;

			if (_mutableGeometry is IMutableGeometry mutableImpl)
			{
				newGeometry =
					(ArcGIS.Core.Geometry.Geometry) mutableImpl.ToNativeImplementation();
			}
			else
			{
				newGeometry = ArcGeometryUtils.CreateProGeometry(_mutableGeometry);
			}

			TryOrRefreshRow<Feature>(f => { f.SetShape(newGeometry); });
		}

		#endregion
	}
}
