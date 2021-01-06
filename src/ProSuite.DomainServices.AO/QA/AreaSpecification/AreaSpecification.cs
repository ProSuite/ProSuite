using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainServices.AO.QA.AreaSpecification
{
	/// <summary>
	/// Represents the applicable area for a quality specification.
	/// </summary>
	[PublicAPI]
	public class AreaSpecification
	{
		private readonly QualitySpecification _qualitySpecification;
		private readonly List<IPolygon> _pendingUnionInput = new List<IPolygon>();
		private readonly SimpleSet<string> _editableDatasets;

		private SimpleSet<QualityCondition> _qualityConditions;
		private IEnvelope _currentTile;
		private TileRelation _currentTileRelation;
		private SimpleSet<string> _verifiedDatasetNames;
		private IPolygon _polygon;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="QualitySpecification"/> class.
		/// </summary>
		/// <param name="qualitySpecification">The quality specification.</param>
		[CLSCompliant(false)]
		public AreaSpecification([NotNull] QualitySpecification qualitySpecification)
		{
			Assert.ArgumentNotNull(qualitySpecification, nameof(qualitySpecification));

			// this will be an unbounded, default area specification
			_qualitySpecification = qualitySpecification;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="QualitySpecification"/> class.
		/// </summary>
		/// <param name="qualitySpecification">The quality specification.</param>
		/// <param name="polygon">The area in the spatial reference of the data model.</param>
		/// <param name="editableDatasets">The editable datasets.</param>
		[CLSCompliant(false)]
		public AreaSpecification([NotNull] QualitySpecification qualitySpecification,
		                         [NotNull] IPolygon polygon,
		                         [NotNull] IEnumerable<Dataset> editableDatasets)
		{
			Assert.ArgumentNotNull(qualitySpecification, nameof(qualitySpecification));
			Assert.ArgumentNotNull(polygon, nameof(polygon));
			Assert.ArgumentNotNull(editableDatasets, nameof(editableDatasets));

			_qualitySpecification = qualitySpecification;
			SetPolygon(polygon);

			_editableDatasets = new SimpleSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (Dataset dataset in editableDatasets)
			{
				_editableDatasets.TryAdd(dataset.Name);
			}
		}

		#endregion

		[NotNull]
		public QualitySpecification QualitySpecification => _qualitySpecification;

		public bool HasPerimeter => _polygon != null;

		public bool AreAllDatasetsEditable([NotNull] ICollection<Dataset> datasets)
		{
			if (EditableDatasetCount != datasets.Count)
			{
				// count is different
				return false;
			}

			if (_editableDatasets == null)
			{
				// both collections are empty
				return true;
			}

			// check if all datasets from the list are contained 
			return datasets.All(dataset => _editableDatasets.Contains(dataset.Name));
		}

		public bool IsAnyDatasetEditable([NotNull] IEnumerable<string> datasetNames)
		{
			return datasetNames.Any(IsEditableDataset);
		}

		private int EditableDatasetCount => _editableDatasets?.Count ?? 0;

		public bool IsEditableDataset([NotNull] string datasetName)
		{
			return _editableDatasets != null && _editableDatasets.Contains(datasetName);
		}

		public bool HasVerifiedDatasets()
		{
			return VerifiedDatasetNames.Count > 0;
		}

		public bool IsVerifiedDataset([NotNull] string datasetName)
		{
			return VerifiedDatasetNames.Contains(datasetName);
		}

		[CLSCompliant(false)]
		public void SetCurrentTile([CanBeNull] IEnvelope currentTile)
		{
			_currentTile = currentTile;
			_currentTileRelation = TileRelation.Unknown;
		}

		[CLSCompliant(false)]
		public bool Intersects([NotNull] IGeometry geometry)
		{
			IPolygon polygon = GetPolygon();
			if (polygon == null)
			{
				return false;
			}

			// TODO: possible exception because of not matching spatial refs?
			//       or make sure the error geometry is also projected in the model spatial
			//       reference (if taken from a non-model involved row).
			return ! ((IRelationalOperator) polygon).Disjoint(geometry);
		}

		[CLSCompliant(false)]
		public bool Contains([NotNull] IGeometry geometry)
		{
			IPolygon polygon = GetPolygon();
			if (polygon == null)
			{
				return false;
			}

			// TODO: possible exception because of not matching spatial refs?
			//       or make sure the error geometry is also projected in the model spatial
			//       reference (if taken from a non-model involved row).
			return ((IRelationalOperator) polygon).Contains(geometry);
		}

		public bool Contains([NotNull] QualityCondition qualityCondition)
		{
			if (_qualityConditions == null)
			{
				IList<QualitySpecificationElement> elements = _qualitySpecification.Elements;

				_qualityConditions = new SimpleSet<QualityCondition>(elements.Count);

				foreach (QualitySpecificationElement element in elements)
				{
					_qualityConditions.Add(element.QualityCondition);
				}
			}

			return _qualityConditions.Contains(qualityCondition);
		}

		/// <summary>
		/// Updates the area specification's polygon by unioning with another polygon
		/// </summary>
		/// <remarks>The spatial references must match.</remarks>
		/// <param name="polygon">The polygon.</param>
		[CLSCompliant(false)]
		public void Union([NotNull] IPolygon polygon)
		{
			Assert.ArgumentNotNull(polygon, nameof(polygon));

			if (_polygon == null)
			{
				SetPolygon(GeometryFactory.Clone(polygon));
			}
			else
			{
				_pendingUnionInput.Add(polygon);
			}
		}

		public override string ToString()
		{
			return string.Format("QualitySpecification: {0}, Polygon: {1}",
			                     _qualitySpecification.Name,
			                     _polygon == null
				                     ? "<null>"
				                     : "<defined>");
		}

		private void SetPolygon([CanBeNull] IPolygon value)
		{
			_polygon = value;
			if (_polygon != null)
			{
				GeometryUtils.AllowIndexing(_polygon);
			}
		}

		[CanBeNull]
		private IPolygon GetPolygon()
		{
			if (_polygon != null && _pendingUnionInput.Count > 0)
			{
				_polygon = GetPendingUnion(_polygon, _pendingUnionInput);
				_pendingUnionInput.Clear();
			}

			return _polygon;
		}

		[NotNull]
		private static IPolygon GetPendingUnion(
			[NotNull] IPolygon polygon,
			[NotNull] IEnumerable<IPolygon> pendingUnionInput)
		{
			var unionInput = new List<IGeometry> {polygon};
			foreach (IPolygon input in pendingUnionInput)
			{
				unionInput.Add(input);
			}

			var result = (IPolygon) GeometryUtils.UnionGeometries(unionInput);
			// result.Weed(1); // seems to make Disjoint SLOWER!!!!!

			const bool allowReorder = true;
			GeometryUtils.Simplify(result, allowReorder);
			GeometryUtils.AllowIndexing(result);

			return result;
		}

		[NotNull]
		private SimpleSet<string> VerifiedDatasetNames => _verifiedDatasetNames ??
		                                                  (_verifiedDatasetNames =
			                                                   GetVerifiedDatasetNames());

		[NotNull]
		private SimpleSet<string> GetVerifiedDatasetNames()
		{
			var result = new SimpleSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (QualitySpecificationElement element in _qualitySpecification.Elements)
			{
				QualityCondition qualityCondition = element.QualityCondition;

				foreach (Dataset dataset in qualityCondition.GetDatasetParameterValues())
				{
					// TODO REFACTORMODEL
					result.TryAdd(dataset.Name.ToUpper());
				}
			}

			return result;
		}

		internal TileRelation CurrentTileRelation
		{
			get
			{
				IPolygon polygon = GetPolygon();

				return polygon == null
					       ? TileRelation.Disjoint
					       : GetCurrentTileRelation(polygon);
			}
		}

		private TileRelation GetCurrentTileRelation([NotNull] IPolygon polygon)
		{
			if (_currentTileRelation == TileRelation.Unknown && _currentTile != null)
			{
				_currentTileRelation = CalculateCurrentTileRelation(_currentTile, polygon);
			}

			return _currentTileRelation;
		}

		private static TileRelation CalculateCurrentTileRelation(
			[NotNull] IEnvelope currentTileEnvelope,
			[NotNull] IPolygon polygon)
		{
			var polygonRelOp = (IRelationalOperator) polygon;

			if (polygonRelOp.Disjoint(currentTileEnvelope))
			{
				return TileRelation.Disjoint;
			}

			return polygonRelOp.Contains(currentTileEnvelope)
				       ? TileRelation.Within
				       : TileRelation.PartialWithin;
		}
	}
}
