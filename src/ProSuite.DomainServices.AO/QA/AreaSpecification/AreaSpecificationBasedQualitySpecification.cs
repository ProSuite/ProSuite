using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainServices.AO.QA.AreaSpecification
{
	[PublicAPI]
	public class AreaSpecificationBasedQualitySpecification :
		ILocationBasedQualitySpecification
	{
		[NotNull] private readonly List<AreaSpecification> _areaSpecifications;
		[NotNull] private readonly IEnvelope _envelopeTemplate = new EnvelopeClass();

		private HashSet<string> _verifiedDatasetNames;

		[CanBeNull] private IEnvelope _currentTile;
		[CanBeNull] private IReadOnlyFeature _currentFeature;
		private Guid _currentRecycleUnique;

		[CanBeNull] private ICollection<AreaSpecification> _currentFeatureAreaSpecifications;

		/// <summary>
		/// Initializes a new instance of the <see cref="AreaSpecificationBasedQualitySpecification"/> class.
		/// </summary>
		/// <param name="areaSpecifications">The area specifications.</param>
		/// <param name="qualitySpecificationName">Name of the unioned quality specification.</param>
		public AreaSpecificationBasedQualitySpecification(
			[NotNull] ICollection<AreaSpecification> areaSpecifications,
			[NotNull] string qualitySpecificationName)
		{
			Assert.ArgumentNotNull(areaSpecifications, nameof(areaSpecifications));
			Assert.ArgumentNotNullOrEmpty(qualitySpecificationName,
			                              nameof(qualitySpecificationName));

			_areaSpecifications = new List<AreaSpecification>(areaSpecifications);
			QualitySpecification = Union(areaSpecifications, qualitySpecificationName);
		}

		[NotNull]
		public QualitySpecification QualitySpecification { get; }

		public void ResetCurrentFeature()
		{
			_currentFeature = null;
			_currentRecycleUnique = Guid.Empty;
			_currentFeatureAreaSpecifications = null;
		}

		public void SetCurrentTile(IEnvelope currentTile)
		{
			_currentTile = currentTile;

			foreach (AreaSpecification areaSpecification in _areaSpecifications)
			{
				areaSpecification.SetCurrentTile(currentTile);
			}
		}

		public bool IsFeatureToBeTested(IReadOnlyFeature feature,
		                                bool recycled, Guid recycleUnique,
		                                QualityCondition qualityCondition,
		                                bool ignoreTestArea)
		{
			IEnumerable<AreaSpecification> areaSpecifications =
				GetAreaSpecifications(feature, recycled, recycleUnique, ignoreTestArea);

			if (areaSpecifications == null)
			{
				return true;
			}

			// cancel if there is no area specification that intersects the feature and
			// which is relevant for the feature's dataset
			return IsInvolvedInAnyAreaSpecification(qualityCondition, areaSpecifications);
		}

		/// <summary>
		/// Determines whether a specified error is relevant for its location.
		/// </summary>
		/// <param name="errorGeometry">The error geometry.</param>
		/// <param name="qualityCondition">The quality condition.</param>
		/// <param name="involvedRows">The involved rows.</param>
		/// <returns>
		///   <c>true</c> if the error is relevant for the location; otherwise, <c>false</c>.
		/// </returns>
		/// <remarks>Must be called within a domain transaction.</remarks>
		public bool IsErrorRelevant(IGeometry errorGeometry,
		                            QualityCondition qualityCondition,
		                            ICollection<InvolvedRow> involvedRows)
		{
			return IsInvolvedInAnyAreaSpecification(
				qualityCondition,
				GetIntersectedAreaSpecifications(errorGeometry,
				                                 qualityCondition,
				                                 involvedRows));
		}

		private bool IsVerifiedDataset([NotNull] string datasetName)
		{
			if (_verifiedDatasetNames == null)
			{
				_verifiedDatasetNames = GetVerifiedDatasetNames(QualitySpecification);
			}

			return _verifiedDatasetNames.Contains(datasetName);
		}

		[NotNull]
		private static QualitySpecification Union(
			[NotNull] IEnumerable<AreaSpecification> areaSpecifications,
			[NotNull] string qualitySpecificationName)
		{
			var result = new QualitySpecification(assignUuid: true);

			foreach (AreaSpecification spec in areaSpecifications)
			{
				result = spec.QualitySpecification.Union(result);
			}

			result.Name = qualitySpecificationName;

			return result;
		}

		[NotNull]
		private static string GetDatasetName([NotNull] IReadOnlyFeature feature)
		{
			//return feature is TerrainRow
			//		   ? ((TerrainRow) feature).DatasetName
			//		   : ((IDataset) feature.Class).Name;
			return feature.Table.Name;
		}

		[NotNull]
		private static HashSet<string> GetVerifiedDatasetNames(
			[NotNull] QualitySpecification qualitySpecification)
		{
			var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (QualitySpecificationElement element in qualitySpecification.Elements)
			{
				QualityCondition qualityCondition = element.QualityCondition;
				if (qualityCondition == null)
				{
					continue;
				}

				foreach (Dataset dataset in qualityCondition.GetDatasetParameterValues(
					         includeSourceDatasets: true))
				{
					result.Add(dataset.Name);
				}
			}

			return result;
		}

		[CanBeNull]
		private List<AreaSpecification> CalculateAreaSpecifications(
			[NotNull] IReadOnlyFeature feature, bool ignoreTestPerimeter)
		{
			if (_areaSpecifications.Count == 0)
			{
				return null;
			}

			var result = new List<AreaSpecification>();

			var featurePerimeterRelation = new FeaturePerimeterRelation(
				feature, _envelopeTemplate, ! ignoreTestPerimeter ? _currentTile : null,
				ignoreCurrentTileRelation: ignoreTestPerimeter);

			// TODO REFACTORMODEL invalid use of class name
			string datasetName = GetDatasetName(feature).ToUpper();
			bool belongsToVerifiedDataset = IsVerifiedDataset(datasetName);
			var containingPrimaryAreaSpecificationFound = false;

			int areaSpecCount = _areaSpecifications.Count;
			for (int index = areaSpecCount - 1; index >= 0; index--)
			{
				AreaSpecification areaSpecification = _areaSpecifications[index];

				if (belongsToVerifiedDataset &&
				    areaSpecification.HasVerifiedDatasets() &&
				    ! areaSpecification.IsVerifiedDataset(datasetName))
				{
					// the area specification has involved datasets, but the (verified)
					// dataset is not part of it --> skip this area specification

					// BUT: the area specification may still be the primary specification, if
					// the dataset is editable in the area specification (and the feature intersects)
					// -> rearrange logic

					continue;
				}

				// either the dataset is not (directly) involved in any quality condition (of all qspecs involved in the run)
				//   (could be indirect, e.g. feature class involved in topology or geometric network)
				// OR it IS involved (generally) AND it is involved in this area specification
				// OR the area specification has no involved datasets (??)

				if (featurePerimeterRelation.Intersects(areaSpecification))
				{
					if (areaSpecification.IsEditableDataset(datasetName) &&
					    featurePerimeterRelation.IsWithin(areaSpecification))
					{
						containingPrimaryAreaSpecificationFound = true;
					}

					// the shape intersects the (defined) area specification polygon
					result.Add(areaSpecification);
				}
			}

			if (! containingPrimaryAreaSpecificationFound)
			{
				AreaSpecification defaultAreaSpecification =
					GetDefaultAreaSpecification(datasetName);
				if (defaultAreaSpecification != null)
				{
					result.Add(defaultAreaSpecification);
				}
			}

			return result;
		}

		/// <summary>
		/// Gets the intersected area specifications.
		/// </summary>
		/// <param name="errorGeometry">The error geometry.</param>
		/// <param name="qualityCondition">The quality condition.</param>
		/// <param name="involvedRows">The involved rows.</param>
		/// <returns></returns>
		/// <remarks>Must be called within a domain transaction</remarks>
		[NotNull]
		private IEnumerable<AreaSpecification> GetIntersectedAreaSpecifications(
			[NotNull] IGeometry errorGeometry,
			[NotNull] QualityCondition qualityCondition,
			[NotNull] ICollection<InvolvedRow> involvedRows)
		{
			if (_areaSpecifications.Count == 0)
			{
				yield break;
			}

			var returnedCount = 0;
			int totalCount = _areaSpecifications.Count;
			IList<string> involvedDatasetNames = null;

			for (int index = totalCount - 1; index >= 0; index--)
			{
				AreaSpecification areaSpecification = _areaSpecifications[index];

				if (involvedDatasetNames == null)
				{
					// TOP-5903: Involved rows does not always contain everything (e.g. editable Terrain + non-editable vector dataset)
					// -> Use datasets from condition parameters rather than from involved error rows
					involvedDatasetNames = 
						qualityCondition.GetDatasetParameterValues(includeSourceDatasets: true)
						                .Select(dataset => dataset.Name).ToList();
				}

				if (! areaSpecification.IsAnyDatasetEditable(involvedDatasetNames))
				{
					// none of the involved datasets is editable in the area specification
					continue;
				}

				if (areaSpecification.Intersects(errorGeometry))
				{
					returnedCount++;

					// the area spec is relevant for the geometry
					yield return areaSpecification;
				}
			}

			if (returnedCount == 0)
			{
				// no area specification found so far

				AreaSpecification defaultAreaSpecification = GetDefaultAreaSpecification(null);
				if (defaultAreaSpecification != null)
				{
					yield return defaultAreaSpecification;
				}
			}
		}

		[CanBeNull]
		private AreaSpecification GetDefaultAreaSpecification(
			[CanBeNull] string datasetName)
		{
			if (_areaSpecifications.Count == 0)
			{
				return null;
			}

			// TODO make this more explicit

			AreaSpecification candidate = _areaSpecifications[0];

			if (candidate.HasPerimeter)
			{
				// it's not a default specification 
				return null;
			}

			// TODO REFACTORMODEL invalid use of class name
			if (datasetName != null && ! candidate.IsVerifiedDataset(datasetName))
			{
				return null;
			}

			// the first non-polygon area spec is used as default
			// if either the dataset name is not specified or if the
			// first area spec involves the specified dataset name.
			return candidate;
		}

		[CanBeNull]
		private IEnumerable<AreaSpecification> GetAreaSpecifications(
			[NotNull] IReadOnlyFeature feature, bool recycled, Guid recycleUnique,
			bool ignoreTestArea)
		{
			if (feature != _currentFeature ||
			    recycled && _currentRecycleUnique != recycleUnique)
			{
				_currentFeature = feature;
				_currentRecycleUnique = recycleUnique;

				if (_areaSpecifications.Count == 0 ||
				    _areaSpecifications.Count == 1 && ! _areaSpecifications[0].HasPerimeter)
				{
					// there are no area specifications, or there is only one
					// (the default specification), or the row is not a feature
					// -> don't cancel, proceed with testing the row
					_currentFeatureAreaSpecifications = null;
				}
				else if (feature.Extent.IsEmpty)
				{
					// when shape was not queried (i.e. QueryFilter.SubFields = 'OID, Field') 

					_currentFeatureAreaSpecifications = null;

					// --> run the test for the feature. If an error occurs, it will (should) have geometry.
					// this error geometry is then tested against the area specification
					// (by protected method IsErrorRelevantForLocation)
				}
				else
				{
					_currentFeatureAreaSpecifications = CalculateAreaSpecifications(feature,
						ignoreTestArea);
				}
			}

			return _currentFeatureAreaSpecifications;
		}

		/// <summary>
		/// Determines whether [is involved in any area specification] [the specified quality condition].
		/// </summary>
		/// <param name="qualityCondition">The quality condition.</param>
		/// <param name="areaSpecifications">The area specifications.</param>
		/// <returns>
		///   <c>true</c> if the quality condition is involved in any area specification; otherwise, <c>false</c>.
		/// </returns>
		/// <remarks>Must be called within a domain transaction</remarks>
		private static bool IsInvolvedInAnyAreaSpecification(
			[NotNull] QualityCondition qualityCondition,
			[NotNull] IEnumerable<AreaSpecification> areaSpecifications)
		{
			return
				areaSpecifications.Any(
					areaSpecification => areaSpecification.Contains(qualityCondition));
		}

		#region Nested types

		private class FeaturePerimeterRelation
		{
			private readonly IReadOnlyFeature _feature;
			private readonly IEnvelope _featureEnvelopeTemplate;
			private readonly IEnvelope _tileEnvelope;
			private readonly bool _ignoreCurrentTileRelation;
			private bool _shapeRead;
			private IGeometry _shape;
			private bool? _shapeIsWithinTile;

			/// <summary>
			/// Initializes a new instance of the <see cref="FeaturePerimeterRelation"/> class.
			/// </summary>
			/// <param name="feature">The feature.</param>
			/// <param name="featureEnvelopeTemplate">The template for the feature envelope.</param>
			/// <param name="tileEnvelope">The envelope of the currently processed tile.</param>
			/// <param name="ignoreCurrentTileRelation"></param>
			public FeaturePerimeterRelation([NotNull] IReadOnlyFeature feature,
			                                [NotNull] IEnvelope featureEnvelopeTemplate,
			                                [CanBeNull] IEnvelope tileEnvelope,
			                                bool ignoreCurrentTileRelation)
			{
				_feature = feature;
				_featureEnvelopeTemplate = featureEnvelopeTemplate;
				_tileEnvelope = tileEnvelope;
				_ignoreCurrentTileRelation = ignoreCurrentTileRelation;
			}

			public bool Intersects([NotNull] AreaSpecification areaSpecification)
			{
				// area spec has no polygon: false
				// shape is empty: false

				if (Shape == null)
				{
					return false;
				}

				if (! _ignoreCurrentTileRelation)
				{
					switch (areaSpecification.CurrentTileRelation)
					{
						case TileRelation.Within:
							if (FeatureIsWithinCurrentTile)
							{
								// feature is within tile, and tile is within area spec
								// -> feature is within area spec
								return true;
							}

							break;

						case TileRelation.Disjoint:
							if (FeatureIsWithinCurrentTile)
							{
								// feature is within tile, and area spec is disjoint from tile
								// -> feature is disjoint from area spec
								return false;
							}

							break;

						case TileRelation.Unknown:
						case TileRelation.PartialWithin:
							// in all other cases, the relationship between feature and
							// area specification will have to be calculated
							break;

						default:
							throw new ArgumentOutOfRangeException();
					}
				}

				// no shortcuts, the feature must be compared to the area specification polygon
				return areaSpecification.Intersects(Shape);
			}

			public bool IsWithin([NotNull] AreaSpecification areaSpecification)
			{
				// area spec has no polygon: false
				// shape is empty: false

				if (Shape == null)
				{
					return false;
				}

				if (! _ignoreCurrentTileRelation)
				{
					switch (areaSpecification.CurrentTileRelation)
					{
						case TileRelation.Within:
							if (FeatureIsWithinCurrentTile)
							{
								// feature is within tile, and tile is within area spec
								// -> feature is within area spec
								return true;
							}

							break;

						case TileRelation.Disjoint:
							if (FeatureIsWithinCurrentTile)
							{
								// feature is within tile, and area spec is disjoint from tile
								// -> feature is disjoint from area spec
								return false;
							}

							break;

						case TileRelation.Unknown:
						case TileRelation.PartialWithin:
							// in all other cases, the relationship between feature and
							// area specification will have to be calculated
							break;

						default:
							throw new ArgumentOutOfRangeException();
					}
				}

				// no shortcuts, the feature must be compared to the area specification polygon
				return areaSpecification.Contains(Shape);
			}

			private bool FeatureIsWithinCurrentTile
			{
				get
				{
					if (! _shapeIsWithinTile.HasValue)
					{
						_shapeIsWithinTile = IsWithinTile(Shape);
					}

					return _shapeIsWithinTile.Value;
				}
			}

			[CanBeNull]
			private IGeometry Shape
			{
				get
				{
					if (! _shapeRead)
					{
						_shape = ReadShape();
						_shapeRead = true;
					}

					return _shape;
				}
			}

			private bool IsWithinTile([CanBeNull] IGeometry shape)
			{
				if (shape == null)
				{
					return false;
				}

				if (_tileEnvelope == null)
				{
					return false;
				}

				shape.QueryEnvelope(_featureEnvelopeTemplate);

				return ((IRelationalOperator) _tileEnvelope)
					.Contains(_featureEnvelopeTemplate);
			}

			[CanBeNull]
			private IGeometry ReadShape()
			{
				IGeometry shape = _feature.Shape;

				return shape == null || shape.IsEmpty
					       ? null
					       : shape;
			}
		}

		#endregion
	}
}
