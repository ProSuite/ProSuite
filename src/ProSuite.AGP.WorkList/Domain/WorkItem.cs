using System;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Domain
{
	public abstract class WorkItem : NotifyPropertyChangedBase, IWorkItem
	{
		private readonly double _extentExpansionFactor;
		private readonly double _minimumSizeDegrees;
		private readonly double _minimumSizeProjected;

		private double _xmax;
		private double _xmin;
		private double _ymax;
		private double _ymin;
		private double _zmax;
		private double _zmin;
		private bool _isZAware;
		private WorkItemStatus _status;
		private string _description;
		private bool _visited;

		protected WorkItem(int id, [NotNull] Row row,
		                   double extentExpansionFactor = 1.1,
		                   double minimumSizeDegrees = 15,
		                   double minimumSizeProjected = 0.001) :
			this(id, new GdbRowReference(row), extentExpansionFactor, minimumSizeDegrees, minimumSizeProjected)
		{
			var feature = row as Feature;

			SetGeometryFromFeature(feature);
		}

		protected WorkItem(int id, GdbRowReference reference,
		                   double extentExpansionFactor = 1.1,
		                   double minimumSizeDegrees = 15,
		                   double minimumSizeProjected = 0.001)
		{
			OID = id;
			Proxy = reference;

			Status = WorkItemStatus.Todo;

			_extentExpansionFactor = extentExpansionFactor;
			_minimumSizeDegrees = minimumSizeDegrees;
			_minimumSizeProjected = minimumSizeProjected;
		}

		public int OID { get; set;  }

		public bool Visited
		{
			get => _visited;
			set
			{
				_visited = value; 
				OnPropertyChanged();
			}
		}

		public WorkItemStatus Status
		{
			get => _status;
			set
			{
				_status = value;
				OnPropertyChanged();
			}
		}

		public string Description
		{
			get => _description;
			set
			{
				_description = value;
				OnPropertyChanged();
			}
		}

		public GdbRowReference Proxy { get; }

		public bool HasGeometry { get; set; }

		//public bool IsBasedOn([NotNull] Row row)
		//{
		//	return Proxy.References(row);
		//}

		//public bool IsBasedOn(Table table)
		//{
		//	return Proxy.References(table);
		//}

		//[NotNull]
		//public Envelope GetExtent()
		//{
		//	if (! HasGeometry)
		//	{
		//		return EnvelopeBuilder.CreateEnvelope();
		//	}

		//	var sref = SpatialReferenceBuilder.CreateSpatialReference(4326);
		//	return EnvelopeBuilder.CreateEnvelope(new Coordinate2D(_xmin, _ymin), new Coordinate2D(_xmax, _ymax), sref);

		//	// todo daro: what to do with sref?
		//	var builder = new EnvelopeBuilder(SpatialReferenceBuilder.CreateSpatialReference(2056, 5729));

		//	builder.SetXYCoords(new Coordinate2D(_xmin, _ymin), new Coordinate2D(_xmax, _ymax));

		//	if (_isZAware)
		//	{
		//		builder.SetZCoords(_zmin, _zmax);
		//	}

		//	return builder.ToGeometry();
		//}

		public Envelope Extent
		{
			get;
			private set;
		}

		public virtual void SetDone(bool done = true)
		{
			throw new NotImplementedException();
		}

		public virtual void SetVisited(bool visited = true)
		{
			throw new NotImplementedException();
		}

		[CanBeNull]
		public Row GetRow()
		{
			return Proxy.GetRow();
		}

		[CanBeNull]
		public string GetDescription()
		{
			var row = GetRow();

			return row == null
				? "Row not found for work item"
				: GetDescriptionCore(row) ?? string.Empty;
		}

		protected virtual string GetDescriptionCore(Row row)
		{
			// todo daro: GetTable() might be a performance issue?
			return $"{DatasetUtils.GetTableDisplayName(row.GetTable())} ID={row.GetObjectID()}";
		}

		public override string ToString()
		{
			return $"{Proxy}: {Status}, {Visited}";
		}

		[CanBeNull]
		protected virtual Geometry GetGeometry([CanBeNull] Feature feature)
		{
			return feature?.GetShape().Extent;
		}

		private void SetGeometryFromFeature([CanBeNull] Feature feature)
		{
			Geometry geometry = GetGeometry(feature);

			if (geometry == null)
			{
				return;
			}

			Extent = EnvelopeBuilder.CreateEnvelope(geometry.Extent);
			SetGeometry(Extent);
		}

		protected void SetGeometry([CanBeNull] Envelope extent)
		{
			// todo daro: set property Extent here instead of building a new Envelope
			//			  every time GetExtent() is called.

			if (extent == null || extent.IsEmpty)
			{
				HasGeometry = false;

				_xmin = 0;
				_ymin = 0;
				_xmax = 0;
				_ymax = 0;
				_zmin = 0;
				_zmax = 0;
			}
			else
			{
				HasGeometry = true;

				double xmin;
				double ymin;
				double xmax;
				double ymax;

				double minimumSize = GetMinimumSize(extent.SpatialReference);

				// calculate the boundary values in x and y, ensuring a minimum size
				if (extent.Width < minimumSize)
				{
					double centerX = (extent.XMin + (extent.Width / 2));
					xmin = centerX - (minimumSize / 2);
					xmax = centerX + (minimumSize / 2);
				}
				else
				{
					xmin = extent.XMin;
					xmax = extent.XMax;
				}

				if (extent.Height < minimumSize)
				{
					double centerY = (extent.YMin + (extent.Height / 2));
					ymin = centerY - (minimumSize / 2);
					ymax = centerY + (minimumSize / 2);
				}
				else
				{
					ymin = extent.YMin;
					ymax = extent.YMax;
				}

				// calculate the offset to apply for a given expansion factor
				double offset = CalculateExpansionOffset(xmin, xmax, ymin, ymax,
					_extentExpansionFactor);

				// apply the offset
				_xmin = xmin - offset;
				_ymin = ymin - offset;
				_xmax = xmax + offset;
				_ymax = ymax + offset;

				// use the z boundary values unchanged
				if (extent.HasZ)
				{
					_isZAware = true;
					_zmin = extent.ZMin;
					_zmax = extent.ZMax;
				}
			}
		}

		/// <summary>
		/// Gets the minimum size for the work item, for a given spatial reference.
		/// </summary>
		/// <param name="spatialReference">The spatial reference.</param>
		/// <returns></returns>
		private double GetMinimumSize([CanBeNull] SpatialReference spatialReference)
		{
			if (spatialReference == null)
			{
				return _minimumSizeDegrees;
			}

			//return spatialReference is IProjectedCoordinateSystem
			//	? _minimumSizeProjected
			//	: _minimumSizeDegrees;

			return _minimumSizeProjected;
		}

		private static double CalculateExpansionOffset(double xmin, double xmax,
			double ymin, double ymax,
			double expansionFactor)
		{
			double expandX = CalculateExpansionOffset(xmin, xmax, expansionFactor);
			double expandY = CalculateExpansionOffset(ymin, ymax, expansionFactor);

			return Math.Max(expandX, expandY);
		}

		private static double CalculateExpansionOffset(double min, double max,
			double expansionFactor)
		{
			Assert.ArgumentCondition(expansionFactor > 0.01,
				"Expansion factor must be larger than 0.01");

			return Math.Abs(expansionFactor - 1) < double.Epsilon
				? 0
				: ((max - min) * (expansionFactor - 1)) / 2;
		}

		public void QueryExtent([CanBeNull] EnvelopeBuilder builder)
		{
			if (! HasGeometry || builder == null)
			{
				return;
			}

			double xmin = Math.Min(builder.XMin, _xmin);
			double xmax = Math.Max(builder.XMax, _xmax);

			double ymin = Math.Min(builder.YMin, _ymin);
			double ymax = Math.Max(builder.YMax, _ymax);

			builder.SetXYCoords(new Coordinate2D(xmin, xmax), new Coordinate2D(ymin, ymax));

			if (! builder.HasZ)
			{
				return;
			}

			double zmin = Math.Min(builder.ZMin, _zmin);
			double zmax = Math.Max(builder.ZMax, _zmax);
			builder.SetZCoords(zmin, zmax);
		}
	}
}
