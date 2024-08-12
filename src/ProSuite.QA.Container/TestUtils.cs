using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.QA.Container.TestSupport;

namespace ProSuite.QA.Container
{
	public static class TestUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		//private static int _garbageCollectionRequestCount;

		private static readonly char[] _tokenSeparators = { ' ', ',', ';' };

		public static int CompareQaErrors([NotNull] QaError error0,
		                                  [NotNull] QaError error1,
		                                  bool compareIndividualInvolvedRows)
		{
			Assert.ArgumentNotNull(error0, nameof(error0));
			Assert.ArgumentNotNull(error1, nameof(error1));
			Assert.ArgumentCondition(error0.Test == error1.Test,
			                         "Only errors created by the same test can be compared");

			// TODO: if the error has a derived geometry, then don't use the geometry for comparison. 
			//       (as it may be different depending on the current verification context (verified datasets) or 
			//        after irrelevant geometry changes to related features)
			//       https://issuetracker02.eggits.net/browse/PSM-162

			int involvedRowsCount = error0.InvolvedRows.Count;

			if (involvedRowsCount != error1.InvolvedRows.Count)
			{
				// different number of involved rows
				return involvedRowsCount - error1.InvolvedRows.Count;
			}

			// check if geometry box is the same
			int envelopeDifference = error0.CompareEnvelope(error1);
			if (envelopeDifference != 0)
			{
				return envelopeDifference;
			}

			// TODO compare error code / error attributes instead
			int descriptionDifference = Comparer<string>.Default.Compare(
				error0.Description,
				error1.Description);
			if (descriptionDifference != 0)
			{
				return descriptionDifference;
			}

			if (involvedRowsCount > 0 && compareIndividualInvolvedRows)
			{
				// check if involved rows are equal
				var list0 = new List<InvolvedRow>(error0.InvolvedRows);
				var list1 = new List<InvolvedRow>(error1.InvolvedRows);

				SortInvolvedRows(list0);
				SortInvolvedRows(list1);
				int involvedRowDifference = CompareSortedInvolvedRows(list0, list1);
				if (involvedRowDifference != 0)
				{
					return involvedRowDifference;
				}
			}

			// no difference detected
			return 0;
		}

		public static void SortInvolvedRows([NotNull] List<InvolvedRow> involvedRows)
		{
			involvedRows.Sort(new InvolvedRowComparer());
		}

		public static int CompareSortedInvolvedRows([NotNull] IList<InvolvedRow> sorted0,
		                                            [NotNull] IList<InvolvedRow> sorted1,
		                                            bool validateRowCount = false)
		{
			int involvedRowsCount = sorted0.Count;
			if (validateRowCount)
			{
				int diffCount = involvedRowsCount - sorted1.Count;
				if (diffCount != 0)
				{
					return diffCount;
				}
			}

			var rowCompare = new InvolvedRowComparer();
			for (var involvedRowIndex = 0;
			     involvedRowIndex < involvedRowsCount;
			     involvedRowIndex++)
			{
				int involvedRowDifference = rowCompare.Compare(
					sorted0[involvedRowIndex],
					sorted1[involvedRowIndex]);

				if (involvedRowDifference != 0)
				{
					return involvedRowDifference;
				}
			}

			return 0;
		}

		[NotNull]
		public static ITableFilter CreateFilter([CanBeNull] IGeometry queryArea,
		                                        [CanBeNull] IPolygon constraintArea,
		                                        [CanBeNull] string constraint,
		                                        [NotNull] IReadOnlyTable table,
		                                        [CanBeNull] TableView tableView)
		{
			ITableFilter filter;
			if (constraintArea != null ||
			    queryArea != null && table is IReadOnlyFeatureClass)
			{
				if (queryArea == null)
				{
					queryArea = constraintArea;
				}
				else if (constraintArea != null)
				{
					queryArea =
						((ITopologicalOperator) constraintArea).Intersect(
							queryArea, esriGeometryDimension.esriGeometry2Dimension);
				}

				IFeatureClassFilter s = new AoFeatureClassFilter(
					queryArea, esriSpatialRelEnum.esriSpatialRelIntersects);

				filter = s;
			}
			else
			{
				filter = new AoTableFilter();
			}

			if (tableView != null)
			{
				filter.SubFields = tableView.SubFields;
			}

			filter.WhereClause = constraint ?? string.Empty;

			return filter;
		}

		[NotNull]
		public static IEnumerable<IGeometry> GetParts([NotNull] IPolygon shape,
		                                              PolygonPartType perPart)
		{
			if ((perPart & PolygonPartType.Full) == PolygonPartType.Full)
			{
				yield return shape;
			}

			if ((perPart & PolygonPartType.ExteriorRing) == PolygonPartType.ExteriorRing)
			{
				foreach (IPolygon extPoly in GeometryUtils.GetConnectedComponents(shape))
				{
					yield return extPoly;
				}
			}

			if ((perPart & PolygonPartType.Ring) == PolygonPartType.Ring)
			{
				foreach (IRing ring in GeometryUtils.GetRings(shape))
				{
					yield return ring;
				}
			}
		}

		[CanBeNull]
		public static IEnvelope GetFullExtent(
			[NotNull] IEnumerable<IReadOnlyGeoDataset> geoDatasets)
		{
			IEnvelope extentUnion = null;
			double maxXyTolerance = 0;

			foreach (IReadOnlyGeoDataset geoDataset in geoDatasets)
			{
				if (geoDataset == null)
				{
					continue;
				}

				IEnvelope datasetExtent = geoDataset.Extent;

				if (datasetExtent == null || datasetExtent.IsEmpty)
				{
					continue;
				}

				string datasetName = geoDataset is IReadOnlyDataset dataset
					                     ? dataset.Name
					                     : "raster";
				_msg.DebugFormat("Adding extent of {0}: {1}",
				                 datasetName,
				                 GeometryUtils.Format(datasetExtent));

				if (geoDataset is IReadOnlyFeatureClass featureClass)
				{
					double xyTolerance;
					if (DatasetUtils.TryGetXyTolerance(featureClass.SpatialReference,
					                                   out xyTolerance))
					{
						maxXyTolerance = Math.Max(maxXyTolerance, xyTolerance);
					}
				}

				if (extentUnion == null)
				{
					extentUnion = datasetExtent;
				}
				else
				{
					double previousArea = ((IArea) extentUnion).Area;

					extentUnion.Union(datasetExtent);

					double area = ((IArea) extentUnion).Area;

					if ((area - previousArea) / area > 0.5)
					{
						_msg.DebugFormat("Enlarged test extent by more than half: {0}",
						                 datasetName);
					}
				}
			}

			if (extentUnion != null)
			{
				_msg.InfoFormat("Full test extent of all datasets: {0}",
				                GeometryUtils.Format(extentUnion));

				extentUnion = EnsureMinimumSize(extentUnion, maxXyTolerance * 10);
			}

			return extentUnion;
		}

		[CanBeNull]
		public static IGeometry GetShapeCopy([NotNull] IReadOnlyRow row)
		{
			if (row is IReadOnlyFeature feature)
			{
				// TODO optimize
				// - feature.Extent creates a copy (feature.Shape.QueryEnvelope() does not)
				// - the feature may really have no geometry (shapefield was in subfields), in this case the additional get just makes it slow
				if (feature.Extent.IsEmpty)
				{
					// this may be the case when the ShapeField was not queried (i.e. QueryFilter.SubFields = 'OID, Field') 
					if (row.HasOID && row.Table.HasOID)
					{
						feature = GdbQueryUtils.GetFeature((IReadOnlyFeatureClass) feature.Table,
						                                   row.OID);

						if (feature != null)
						{
							return feature.ShapeCopy;
						}
					}
				}
				else
				{
					return feature.ShapeCopy;
				}
			}

			return null;
		}

		[CanBeNull]
		public static IGeometry GetInvolvedShapeCopy([NotNull] IReadOnlyRow row)
		{
			IGeometry involvedShape = GetShapeCopy(row);
			if (involvedShape != null)
			{
				return involvedShape;
			}

			// Try to get shape from an involved row
			// Similar to InvolvedRowUtils.GetInvolvedRows(row);

			if (row.Table.FullName is IQueryName qn)
			{
				foreach (string table in qn.QueryDef.Tables.Split(','))
				{
					string t = table.Trim();
					string oidField = $"{t}.OBJECTID";
					int oidFieldIdx = row.Table.FindField(oidField);
					if (oidFieldIdx >= 0)
					{
						long? oidValue = GdbObjectUtils.ReadRowOidValue(row, oidFieldIdx);

						if (! oidValue.HasValue)
						{
							continue;
						}

						long oid = oidValue.Value;

						if (row.Table.Workspace is IFeatureWorkspace fws
						    && DatasetUtils.OpenTable(fws, t) is IFeatureClass queryFc)
						{
							IFeature involvedFeature = GdbQueryUtils.GetFeature(queryFc, oid);
							IReadOnlyFeature roFeature = ReadOnlyFeature.Create(involvedFeature);
							involvedShape = GetInvolvedShapeCopy(roFeature);
							if (involvedShape != null)
							{
								return involvedShape;
							}
						}
					}
				}
			}

			int baseRowsField = row.Table.Fields.FindField(InvolvedRowUtils.BaseRowField);
			if (baseRowsField >= 0 && row.get_Value(baseRowsField) is IList<IReadOnlyRow> baseRows)
			{
				foreach (var baseRow in baseRows)
				{
					involvedShape = GetInvolvedShapeCopy(baseRow);
					if (involvedShape != null)
					{
						return involvedShape;
					}
				}
			}

			return null;
		}

		[CanBeNull]
		public static ISpatialReference GetUniqueSpatialReference(
			[NotNull] IInvolvesTables test,
			bool requireEqualVerticalCoordinateSystems)
		{
			return GetUniqueSpatialReference(GetSpatialReferences(test),
			                                 requireEqualVerticalCoordinateSystems);
		}

		[NotNull]
		public static IEnumerable<ISpatialReference> GetSpatialReferences(
			[NotNull] IInvolvesTables test)
		{
			Assert.ArgumentNotNull(test, nameof(test));

			foreach (IReadOnlyGeoDataset geoDataset in GetGeodatasets(test))
			{
				if (geoDataset.SpatialReference != null)
				{
					yield return geoDataset.SpatialReference;
				}
			}
		}

		[NotNull]
		public static IEnumerable<IReadOnlyGeoDataset> GetGeodatasets(
			[NotNull] IInvolvesTables test)
		{
			Assert.ArgumentNotNull(test, nameof(test));

			if (test is ContainerTest containerTest)
			{
				foreach (IReadOnlyGeoDataset involvedGeoDataset in containerTest
					         .GetInvolvedGeoDatasets())
				{
					yield return involvedGeoDataset;
				}
			}
			else
			{
				foreach (IReadOnlyTable table in test.InvolvedTables)
				{
					if (table is IReadOnlyGeoDataset geoDataset)
					{
						yield return geoDataset;
					}
				}
			}
		}

		[CanBeNull]
		public static ISpatialReference GetUniqueSpatialReference(IEnumerable<ITest> tests)
		{
			var spatialReferences = new List<ISpatialReference>();

			foreach (ITest test in tests)
			{
				spatialReferences.AddRange(GetSpatialReferences(test));
			}

			return GetUniqueSpatialReference(
				spatialReferences, requireEqualVerticalCoordinateSystems: false);
		}

		[CanBeNull]
		public static ISpatialReference GetUniqueSpatialReference(
			[NotNull] IEnumerable<ISpatialReference> spatialReferences,
			bool requireEqualVerticalCoordinateSystems)
		{
			Assert.ArgumentNotNull(spatialReferences, nameof(spatialReferences));

			IVerticalCoordinateSystem uniqueVcs = null;
			ISpatialReference uniqueSref = null;
			double bestResolution = double.MaxValue;

			foreach (ISpatialReference sref in spatialReferences)
			{
				if (uniqueSref == null)
				{
					uniqueSref = sref;
					bestResolution = SpatialReferenceUtils.GetXyResolution(uniqueSref);
				}
				else
				{
					if (requireEqualVerticalCoordinateSystems)
					{
						IVerticalCoordinateSystem vcs =
							SpatialReferenceUtils.GetVerticalCoordinateSystem(sref);

						if (vcs != null)
						{
							if (uniqueVcs == null)
							{
								uniqueVcs = vcs;
							}
							else
							{
								if (vcs != uniqueVcs &&
								    ! ((IClone) uniqueVcs).IsEqual((IClone) vcs))
								{
									throw new ArgumentException(
										string.Format(
											"Defined vertical coordinate systems are not equal: {0}, {1}",
											vcs.Name, uniqueVcs.Name));
								}
							}
						}
					}

					if (uniqueSref != sref)
					{
						var compareSpatialReferences =
							(ICompareCoordinateSystems) uniqueSref;

						if (! compareSpatialReferences.IsEqualNoVCS(sref))
						{
							throw new ArgumentException(
								string.Format(
									"Coordinate systems are not equal: {0}, {1}",
									sref.Name, uniqueSref.Name));
						}

						// if the resolution is higher --> use as new unique
						double resolution = SpatialReferenceUtils.GetXyResolution(sref);

						if (resolution < bestResolution)
						{
							bestResolution = resolution;
							uniqueSref = sref;
						}
					}
				}
			}

			return uniqueSref;
		}

		/// <summary>
		/// Remark: use ShapeCopy for geometries of features, because the spatial reference of the features is adapted
		/// </summary>
		/// <param name="shape0"></param>
		/// <param name="shape1"></param>
		/// <returns></returns>
		public static IGeometry GetOverlap([NotNull] IGeometry shape0,
		                                   [NotNull] IGeometry shape1)
		{
			// TODO revise implementation, move to IntersectionUtils
			const bool overlap = true;
			return GetIntersection(shape0, shape1, overlap);
		}

		/// <summary>
		/// Remark: use ShapeCopy for geometries of features, because the spatial reference of the features is adapted
		/// </summary>
		/// <param name="geometry0"></param>
		/// <param name="geometry1"></param>
		/// <param name="overlap"></param>
		/// <returns></returns>
		[NotNull]
		private static IGeometry GetIntersection([NotNull] IGeometry geometry0,
		                                         [NotNull] IGeometry geometry1,
		                                         bool overlap)
		{
			Assert.ArgumentNotNull(geometry0, nameof(geometry0));
			Assert.ArgumentNotNull(geometry1, nameof(geometry1));

			// TODO revise implementation, move to IntersectionUtils

			esriGeometryType g0Type = geometry0.GeometryType;
			esriGeometryType g1Type = geometry1.GeometryType;

			if (g0Type == esriGeometryType.esriGeometryMultiPatch)
			{
				geometry0 = GeometryFactory.CreatePolygon((IMultiPatch) geometry0);
				g0Type = esriGeometryType.esriGeometryPolygon;
			}

			if (g1Type == esriGeometryType.esriGeometryMultiPatch)
			{
				geometry1 = GeometryFactory.CreatePolygon((IMultiPatch) geometry1);
				g1Type = esriGeometryType.esriGeometryPolygon;
			}

			esriGeometryDimension dimension =
				GetIntersectionDimension(g0Type, g1Type, overlap);

			GeometryUtils.AllowIndexing(geometry0);
			GeometryUtils.AllowIndexing(geometry1);

			IGeometry intersection = ((ITopologicalOperator) geometry0).Intersect(
				geometry1,
				dimension);

			if (! intersection.IsEmpty)
			{
				const bool allowReorder = true;
				GeometryUtils.Simplify(intersection, allowReorder);
			}

			return intersection;
		}

		//internal static void AddGarbageCollectionRequest()
		//{
		//    _garbageCollectionRequestCount++;

		//    if (_garbageCollectionRequestCount <= 50)
		//    {
		//        return;
		//    }

		//    GC.Collect();
		//    _garbageCollectionRequestCount = 0;
		//}

		/// <summary>
		/// Gets the display name of the field, for inclusion in error descriptions. The
		/// display name consists of the field alias name followed by the field name in brackets,
		/// if the field name is different from the alias name.
		/// </summary>
		/// <param name="row">The row.</param>
		/// <param name="fieldIndex">Index of the field.</param>
		/// <param name="fieldName">Name of the field.</param>
		/// <returns></returns>
		public static string GetFieldDisplayName([NotNull] IReadOnlyRow row,
		                                         int fieldIndex,
		                                         [NotNull] out string fieldName)
		{
			Assert.ArgumentNotNull(row, nameof(row));
			Assert.ArgumentCondition(fieldIndex >= 0, "invalid field index: {0}",
			                         fieldIndex);

			IField field = row.Table.Fields.Field[fieldIndex];

			fieldName = field.Name;
			string fieldAlias = field.AliasName;

			return fieldName.Equals(fieldAlias, StringComparison.OrdinalIgnoreCase)
				       ? fieldAlias
				       : string.Format("{0} ({1})", fieldAlias, fieldName);
		}

		[NotNull]
		public static IEnumerable<string> GetTokens([CanBeNull] string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				yield break;
			}

			foreach (
				string token in
				text.Split(_tokenSeparators, StringSplitOptions.RemoveEmptyEntries))
			{
				if (string.IsNullOrEmpty(token))
				{
					continue;
				}

				yield return token.Trim();
			}
		}

		public static bool IsFeatureFullyChecked(
			[NotNull] IEnvelope featureExtent,
			[NotNull] IEnvelope tileEnvelope,
			[CanBeNull] IEnvelope testRunEnvelope)
		{
			double xMin;
			double yMin;
			double xMax;
			double yMax;
			featureExtent.QueryCoords(out xMin, out yMin, out xMax, out yMax);

			return IsFeatureFullyChecked(xMin, yMin, xMax, yMax, tileEnvelope,
			                             testRunEnvelope);
		}

		public static bool IsFeatureFullyChecked(double featureXMin, double featureYMin,
		                                         double featureXMax, double featureYMax,
		                                         [NotNull] IEnvelope tileEnvelope,
		                                         [CanBeNull] IEnvelope testRunEnvelope)
		{
			double tileXMax;
			double tileYMax;
			tileEnvelope.QueryCoords(out _, out _, out tileXMax, out tileYMax);

			if (featureXMax > tileXMax || featureYMax > tileYMax)
			{
				// the feature overlaps the upper/right boundary of what was tested so far
				return false;
			}

			if (testRunEnvelope != null)
			{
				double runXMin;
				double runYMin;
				double runXMax;
				double runYMax;
				testRunEnvelope.QueryCoords(out runXMin, out runYMin,
				                            out runXMax, out runYMax);

				if (featureXMin < runXMin || featureYMin < runYMin)
				{
					// the feature overlaps the lower/left boundary of the entire 
					// test extent --> it won't ever be fully checked
					return false;
				}

				if (featureXMax > runXMax || featureYMax > runYMax)
				{
					// the feature overlaps the upper/right boundary of the entire 
					// test extent --> it won't ever be fully checked
					return false;
				}
			}

			return true;
		}

		[CanBeNull]
		public static IGeometry GetEnlargedExtentPolygon([NotNull] IGeometry shape,
		                                                 double xyTolerance)
		{
			IEnvelope extent = shape.Envelope;

			if (extent.IsEmpty)
			{
				// if even the extent is null, there's nothing we can do. 
				// this will be an error without geometry
				return null;
			}

			double expandDistance = xyTolerance * 10;

			if (extent.Width < expandDistance &&
			    extent.Height < expandDistance)
			{
				// extent is very small, just return LL corner point
				return extent.LowerLeft;
			}

			const bool asRatio = false;
			extent.Expand(expandDistance, expandDistance, asRatio);

			return GeometryFactory.CreatePolygon(extent);
		}

		[NotNull]
		public static IList<double> GetMTolerances(
			[NotNull] IEnumerable<IReadOnlyFeatureClass> featureClasses)
		{
			Assert.ArgumentNotNull(featureClasses, nameof(featureClasses));

			var result = new List<double>();

			foreach (IReadOnlyFeatureClass featureClass in featureClasses)
			{
				double mTolerance;
				if (! DatasetUtils.TryGetMTolerance(featureClass.SpatialReference, out mTolerance))
				{
					throw new ArgumentException(
						string.Format("{0} has an undefined or invalid M tolerance",
						              featureClass.Name));
				}

				result.Add(mTolerance);
			}

			return result;
		}

		[NotNull]
		public static IList<double> GetXyTolerances(
			[NotNull] IEnumerable<IReadOnlyFeatureClass> featureClasses)
		{
			Assert.ArgumentNotNull(featureClasses, nameof(featureClasses));

			var result = new List<double>();

			foreach (IReadOnlyFeatureClass featureClass in featureClasses)
			{
				double xyTolerance;
				if (! DatasetUtils.TryGetXyTolerance(featureClass.SpatialReference,
				                                     out xyTolerance))
				{
					throw new ArgumentException(
						string.Format("{0} has an undefined or invalid XY tolerance",
						              featureClass.Name));
				}

				result.Add(xyTolerance);
			}

			return result;
		}

		[NotNull]
		public static IList<int> GetFieldIndexes(
			[NotNull] IEnumerable<IReadOnlyFeatureClass> featureClasses,
			[NotNull] IEnumerable<string> fieldNames)
		{
			return GetFieldIndexes(featureClasses.Cast<IReadOnlyTable>(), fieldNames);
		}

		[NotNull]
		public static IList<int> GetFieldIndexes([NotNull] IEnumerable<IReadOnlyTable> tables,
		                                         [NotNull] IEnumerable<string> fieldNames)
		{
			Assert.ArgumentNotNull(tables, nameof(tables));
			Assert.ArgumentNotNull(fieldNames, nameof(fieldNames));

			ICollection<IReadOnlyTable> tableCollection = CollectionUtils.GetCollection(tables);
			ICollection<string> fieldNameCollection =
				CollectionUtils.GetCollection(fieldNames);

			AssertValidFieldNameCount(tableCollection, fieldNameCollection);

			int tableCount = tableCollection.Count;

			var result = new List<int>();

			for (var tableIndex = 0; tableIndex < tableCount; tableIndex++)
			{
				// the list index of the following lists matches the table index
				result.Add(
					GetFieldIndex(tableCollection, tableIndex, fieldNameCollection));
			}

			return result;
		}

		[NotNull]
		public static IEnumerable<IPolygon> GetSmallestPolygons(
			[NotNull] ICollection<IPolygon> polygons,
			int numberOfSmallestPolygons)
		{
			Assert.ArgumentNotNull(polygons, nameof(polygons));
			Assert.ArgumentCondition(numberOfSmallestPolygons <= polygons.Count,
			                         "must be <= number of polygons");

			List<GeometryArea<IPolygon>> polygonAreas =
				polygons.Select(ring => new GeometryArea<IPolygon>(ring))
				        .ToList();

			polygonAreas.Sort((r1, r2) => r1.Area.CompareTo(r2.Area));

			var result = new List<IPolygon>();

			for (var i = 0; i < numberOfSmallestPolygons; i++)
			{
				result.Add(polygonAreas[i].Geometry);
			}

			return result;
		}

		[NotNull]
		public static IEnumerable<IRing> GetSmallestRings(
			[NotNull] ICollection<IRing> rings,
			int numberOfSmallestRings)
		{
			Assert.ArgumentNotNull(rings, nameof(rings));
			Assert.ArgumentCondition(numberOfSmallestRings <= rings.Count,
			                         "must be <= number of rings");

			List<GeometryArea<IRing>> ringAreas =
				rings.Select(ring => new GeometryArea<IRing>(ring))
				     .ToList();

			ringAreas.Sort((r1, r2) => r1.Area.CompareTo(r2.Area));

			var result = new List<IRing>();

			for (var i = 0; i < numberOfSmallestRings; i++)
			{
				result.Add(ringAreas[i].Geometry);
			}

			return result;
		}

		[CanBeNull]
		public static string GetShapeFieldName([NotNull] IReadOnlyRow row)
		{
			Assert.ArgumentNotNull(row, nameof(row));

			var featureClass = row.Table as IReadOnlyFeatureClass;

			return featureClass?.ShapeFieldName;
		}

		[NotNull]
		public static string GetShapeFieldName([NotNull] IReadOnlyFeature feature)
		{
			Assert.ArgumentNotNull(feature, nameof(feature));

			return ((IReadOnlyFeatureClass) feature.Table).ShapeFieldName;
		}

		[NotNull]
		public static string GetShapeFieldNames([NotNull] params IReadOnlyFeature[] features)
		{
			return GetShapeFieldNames((IEnumerable<IReadOnlyFeature>) features);
		}

		[NotNull]
		public static string GetShapeFieldNames([NotNull] IEnumerable<IReadOnlyFeature> features)
		{
			Assert.ArgumentNotNull(features, nameof(features));

			var names = new HashSet<string>(features.Select(GetShapeFieldName),
			                                StringComparer.InvariantCultureIgnoreCase);

			Assert.ArgumentCondition(names.Count > 0, "no features",
			                         nameof(features));

			return StringUtils.ConcatenateSorted(names, ",");
		}

		public static bool UsesSpatialDataset([NotNull] ITest test)
		{
			if (test.InvolvedTables.OfType<IReadOnlyFeatureClass>().Any())
			{
				return true;
			}

			var containerTest = test as ContainerTest;

			return containerTest?.InvolvedTerrains?.Any() ?? false;
		}

		[NotNull]
		public static Dictionary<IReadOnlyTable, IList<T>> GetTestsByTable<T>(
			[NotNull] IEnumerable<T> tests)
			where T : ITest
		{
			Assert.ArgumentNotNull(tests, nameof(tests));

			return GetTestsByInvolvedType(tests, (test) => test.InvolvedTables);
		}

		[NotNull]
		public static Dictionary<TI, IList<T>> GetTestsByInvolvedType<TI, T>(
			[NotNull] IEnumerable<T> tests, [NotNull] Func<T, IEnumerable<TI>> enumInvolved)
			where T : ITest
		{
			Assert.ArgumentNotNull(tests, nameof(tests));

			var result = new Dictionary<TI, IList<T>>();

			foreach (T test in tests)
			{
				IEnumerable<TI> involveds = enumInvolved(test);
				if (involveds == null)
				{
					continue;
				}

				foreach (TI involved in involveds)
				{
					if (! result.TryGetValue(involved, out IList<T> list))
					{
						list = new List<T>();
						result.Add(involved, list);
					}

					list.Add(test);
				}
			}

			return result;
		}

		public static double GetMaximumSearchDistance([NotNull] IEnumerable<ITest> tests)
		{
			Assert.ArgumentNotNull(tests, nameof(tests));

			double result = 0;
			foreach (ITest test in tests)
			{
				if (test is ContainerTest containerTest)
				{
					result = Math.Max(result, containerTest.SearchDistance);
				}
			}

			return result;
		}

		/// <summary>
		/// Groups tests into container and non container tests
		/// </summary>
		/// <param name="tests"></param>
		/// <param name="allowEditing"></param>
		/// <param name="containerTests"></param>
		/// <param name="nonContainerTests"></param>
		public static void ClassifyTests(
			[NotNull] IEnumerable<ITest> tests,
			bool allowEditing,
			[NotNull] out IList<ContainerTest> containerTests,
			[NotNull] out IList<ITest> nonContainerTests)
		{
			// Handle ITest and extract Test classes
			containerTests = new List<ContainerTest>();
			nonContainerTests = new List<ITest>();

			foreach (ITest test in tests)
			{
				var cached = false;

				if (test is ContainerTest containerTest)
				{
					foreach (IReadOnlyTable table in containerTest.InvolvedTables)
					{
						if (! (table is IReadOnlyFeatureClass))
						{
							continue;
						}

						// found a feature class, enable use as container test
						containerTests.Add(containerTest);
						cached = true;
						break;
					}

					if (! cached && containerTest.InvolvedTerrains?.Any() == true)
					{
						containerTests.Add(containerTest);
						cached = true;
					}
				}

				// important: this may also be ContainerTest subclasses, when
				// they don't have any involved feature class
				if (! cached)
				{
					nonContainerTests.Add(test);
				}
			}
		}

		[NotNull]
		public static IDictionary<int, double> GetXyToleranceByTableIndex(
			[NotNull] ICollection<IReadOnlyTable> tables)
		{
			Assert.ArgumentNotNull(tables, nameof(tables));

			var result = new Dictionary<int, double>(tables.Count);

			var index = 0;
			foreach (IReadOnlyTable table in tables)
			{
				var featureClass = table as IReadOnlyFeatureClass;

				double xyTolerance;
				if (featureClass == null ||
				    ! DatasetUtils.TryGetXyTolerance(featureClass.SpatialReference,
				                                     out xyTolerance))
				{
					xyTolerance = 0;
				}

				result.Add(index, xyTolerance);
				index++;
			}

			return result;
		}

		[NotNull]
		public static IDictionary<int, esriGeometryType> GetGeometryTypesByTableIndex(
			[NotNull] ICollection<IReadOnlyTable> tables)
		{
			Assert.ArgumentNotNull(tables, nameof(tables));

			var result = new Dictionary<int, esriGeometryType>(tables.Count);

			var index = 0;
			foreach (IReadOnlyTable table in tables)
			{
				var featureClass = table as IReadOnlyFeatureClass;

				result.Add(
					index, featureClass?.ShapeType ?? esriGeometryType.esriGeometryNull);
				index++;
			}

			return result;
		}

		public static bool IsSameRow([NotNull] IReadOnlyRow row0, [NotNull] IReadOnlyRow row1) =>
			row0 == row1 || row0.OID == row1.OID && row0.Table == row1.Table;

		/// <summary>
		/// Ensures a minimum width/height for a given envelope
		/// </summary>
		/// <param name="envelope">The envelope</param>
		/// <param name="minSize">The minimum width/height</param>
		/// <returns>The input envelope if it is large enough, or a new envelope enlarged to the minimum width and/or height</returns>
		/// <remarks>The returned envelope will not be Z aware</remarks>
		[NotNull]
		public static IEnvelope EnsureMinimumSize([NotNull] IEnvelope envelope,
		                                          double minSize)
		{
			if (envelope.IsEmpty ||
			    envelope.Width >= minSize && envelope.Height >= minSize)
			{
				return envelope;
			}

			_msg.DebugFormat("Enlarging envelope to minimum size {0}", minSize);

			return GeometryFactory.CreateEnvelope(
				((IArea) envelope).Centroid,
				Math.Max(envelope.Width, minSize),
				Math.Max(envelope.Height, minSize));
		}

		/// <summary>
		/// Gets the index of the field in a given table, based on a collection of field names per table, and a table index
		/// </summary>
		/// <param name="tables">The list of tables.</param>
		/// <param name="tableIndex">Index of the table.</param>
		/// <param name="fieldNames">The field names (either a single field to be used for all tables, or one field name per table).</param>
		/// <returns></returns>
		private static int GetFieldIndex([NotNull] IEnumerable<IReadOnlyTable> tables,
		                                 int tableIndex,
		                                 [NotNull] IEnumerable<string> fieldNames)
		{
			Assert.ArgumentNotNull(tables, nameof(tables));
			Assert.ArgumentNotNull(fieldNames, nameof(fieldNames));

			List<IReadOnlyTable> tableList = tables.ToList();
			List<string> fieldList = fieldNames.ToList();

			AssertValidFieldNameCount(tableList, fieldList);

			Assert.ArgumentCondition(tableIndex >= 0, "Invalid table index: {0}",
			                         tableIndex);
			Assert.ArgumentCondition(tableIndex < tableList.Count,
			                         "Table index out of bounds: {0}", tableIndex);

			IReadOnlyTable table = tableList[tableIndex];

			string fieldName = fieldList.Count == 1
				                   ? fieldList[0]
				                   : fieldList[tableIndex];

			int fieldIndex = table.FindField(fieldName);

			if (fieldIndex < 0)
			{
				throw new ArgumentException(
					string.Format("Field {0} not found in table {1}",
					              fieldName, table.Name));
			}

			return fieldIndex;
		}

		private static void AssertValidFieldNameCount(
			[NotNull] IEnumerable<IReadOnlyTable> tables,
			[NotNull] IEnumerable<string> fieldNames)
		{
			int tableCount = tables.Count();
			int fieldCount = fieldNames.Count();
			Assert.ArgumentCondition(
				fieldCount == 1 || fieldCount == tableCount,
				"Invalid number of field names; must be either 1 or equal to the number of tables");
		}

		private static esriGeometryDimension GetIntersectionDimension(
			esriGeometryType g0Type,
			esriGeometryType g1Type,
			bool overlap)
		{
			if (g0Type == esriGeometryType.esriGeometryPolygon)
			{
				if (g1Type == esriGeometryType.esriGeometryPolygon)
				{
					return esriGeometryDimension.esriGeometry2Dimension;
				}

				if (g1Type == esriGeometryType.esriGeometryPolyline)
				{
					return esriGeometryDimension.esriGeometry1Dimension;
				}

				if (g1Type == esriGeometryType.esriGeometryPoint)
				{
					return esriGeometryDimension.esriGeometry0Dimension;
				}

				throw new ArgumentException(
					string.Format("Unhandled geometry type: {0}", g1Type));
			}

			if (g0Type == esriGeometryType.esriGeometryPolyline)
			{
				if (g1Type == esriGeometryType.esriGeometryPolygon)
				{
					return esriGeometryDimension.esriGeometry1Dimension;
				}

				if (g1Type == esriGeometryType.esriGeometryPolyline && overlap)
				{
					return esriGeometryDimension.esriGeometry1Dimension;
				}

				if (g1Type == esriGeometryType.esriGeometryPolyline ||
				    g1Type == esriGeometryType.esriGeometryPoint)
				{
					return esriGeometryDimension.esriGeometry0Dimension;
				}

				throw new ArgumentException(
					string.Format("Unhandled geometry type: {0}", g1Type));
			}

			if (g0Type == esriGeometryType.esriGeometryPoint)
			{
				return esriGeometryDimension.esriGeometry0Dimension;
			}

			throw new ArgumentException(
				string.Format("Unhandled geometry type: {0}", g0Type));
		}

		public static void SetContainer(IDataContainer dataContainer,
		                                IEnumerable<IReadOnlyTable> containerAwareTables)
		{
			foreach (IReadOnlyTable table in containerAwareTables)
			{
				if (table is IDataContainerAware transformed)
				{
					transformed.DataContainer = dataContainer;

					SetContainer(dataContainer, transformed.InvolvedTables);
				}
			}
		}

		#region Nested types

		private class GeometryArea<T> where T : IGeometry
		{
			public GeometryArea([NotNull] T geometry)
			{
				Geometry = geometry;
				Area = Math.Abs(((IArea) geometry).Area);
			}

			[NotNull]
			public T Geometry { get; }

			public double Area { get; }
		}

		private class InvolvedRowComparer : IComparer<InvolvedRow>
		{
			private const char _nameSeparator = '.';

			#region IComparer<InvolvedRow> Members

			public int Compare(InvolvedRow row0, InvolvedRow row1)
			{
				if (row0 == null && row1 == null) return 0;
				if (row0 == null) return -1;
				if (row1 == null) return 1;

				int rowCompare = row0.OID.CompareTo(row1.OID);

				return rowCompare != 0
					       ? rowCompare
					       : CompareTableNames(row0.TableName, row1.TableName);
			}

			#endregion

			// TODO this might be useful elsewhere
			private static int CompareTableNames([NotNull] string tableName0,
			                                     [NotNull] string tableName1)
			{
				int difference = string.Compare(tableName0, tableName1,
				                                StringComparison.OrdinalIgnoreCase);
				if (difference == 0)
				{
					// table names are equal
					return 0;
				}

				// not equal:
				bool name0IsQualified = IsQualifiedName(tableName0);
				bool name1IsQualified = IsQualifiedName(tableName1);

				if (name0IsQualified == name1IsQualified)
				{
					return difference;
				}

				// handle qualified/unqualified table name combination:
				// - if one name is qualified and the other is not: 
				//   - unqualify the qualified name, compare the unqualified names

				string unqualifiedName0 = name0IsQualified
					                          ? GetUnqualifiedName(tableName0)
					                          : tableName0;
				string unqualifiedName1 = name1IsQualified
					                          ? GetUnqualifiedName(tableName1)
					                          : tableName1;

				return string.Compare(unqualifiedName0, unqualifiedName1,
				                      StringComparison.OrdinalIgnoreCase);
			}

			private static bool IsQualifiedName([NotNull] string name)
			{
				int index = name.IndexOf(_nameSeparator);

				return index > 0 && index < name.Length - 1;
			}

			[NotNull]
			private static string GetUnqualifiedName([NotNull] string name)
			{
				string[] tokens = name.Split(_nameSeparator);

				return tokens[tokens.Length - 1];
			}
		}

		#endregion

		[NotNull]
		public static Type GetColumnType([NotNull] IField field)
		{
			return GetColumnType(field.Type);
		}

		[NotNull]
		public static Type GetColumnType(esriFieldType fieldType)
		{
			switch (fieldType)
			{
				case esriFieldType.esriFieldTypeSmallInteger:
					return typeof(short);

				case esriFieldType.esriFieldTypeInteger:
					return typeof(int);

				case esriFieldType.esriFieldTypeSingle:
					return typeof(float);

				case esriFieldType.esriFieldTypeDouble:
					return typeof(double);

				case esriFieldType.esriFieldTypeString:
					return typeof(string);

				case esriFieldType.esriFieldTypeDate:
					return typeof(DateTime);

				case esriFieldType.esriFieldTypeOID:
					return typeof(int);

				case esriFieldType.esriFieldTypeGUID:
				case esriFieldType.esriFieldTypeGlobalID:
					return typeof(Guid);

				case esriFieldType.esriFieldTypeGeometry:
				case esriFieldType.esriFieldTypeBlob:
				case esriFieldType.esriFieldTypeRaster:
				case esriFieldType.esriFieldTypeXML:
					return typeof(object);

				default:
					throw new ArgumentOutOfRangeException(nameof(fieldType), fieldType,
					                                      @"Unsupported field type");
			}
		}
	}
}
