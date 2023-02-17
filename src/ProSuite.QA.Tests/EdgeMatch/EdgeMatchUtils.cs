using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Tests.Properties;

namespace ProSuite.QA.Tests.EdgeMatch
{
	public static class EdgeMatchUtils
	{
		public static bool IsDisjoint([NotNull] IGeometry geometry1,
		                              [NotNull] IGeometry geometry2)
		{
			return ((IRelationalOperator) geometry1).Disjoint(geometry2);
		}

		public static bool VerifyHandled([NotNull] IReadOnlyFeature feature,
		                                 WKSEnvelope tileEnvelope,
		                                 WKSEnvelope allEnvelope)
		{
			return VerifyHandled(feature.Shape, tileEnvelope, allEnvelope);
		}

		public static bool VerifyHandled([NotNull] IGeometry geometry,
		                                 WKSEnvelope tileEnvelope,
		                                 WKSEnvelope allEnvelope)
		{
			WKSEnvelope geometryEnvelope = QaGeometryUtils.GetWKSEnvelope(geometry);

			return (tileEnvelope.XMax >= allEnvelope.XMax ||
			        geometryEnvelope.XMax < tileEnvelope.XMax) &&
			       (tileEnvelope.YMax >= allEnvelope.YMax ||
			        geometryEnvelope.YMax < tileEnvelope.YMax);
		}

		[NotNull]
		public static IPolyline GetCommonBorder(
			[NotNull] IPolyline areaBoundaryAlongBorder,
			[NotNull] IPolyline neighborAreaBoundaryAlongBorder,
			double xyTolerance)
		{
			return GetLinearIntersection(areaBoundaryAlongBorder,
			                             neighborAreaBoundaryAlongBorder,
			                             xyTolerance);
		}

		[CanBeNull]
		public static IPolyline GetNotEqualLine(
			[NotNull] IPolyline commonBorder,
			[NotNull] IPolyline alongBorder,
			[NotNull] IPolyline neighborAlongBorder,
			double distance,
			ref BufferFactory bufferFactory,
			double? xyTolerance = null)
		{
			IPolyline restNeighbor;
			if (! commonBorder.IsEmpty)
			{
				// restNeighbor = GetNearPart(commonBorder, neighborAreaBoundaryAlongBorder) ?? neighborAreaBoundaryAlongBorder;
				restNeighbor = GetDifference(neighborAlongBorder, commonBorder, xyTolerance);
			}
			else
			{
				restNeighbor = neighborAlongBorder;
				double offset = ((IProximityOperator) restNeighbor).ReturnDistance(alongBorder);
				if (offset > distance)
				{
					return null;
				}
			}

			return restNeighbor.IsEmpty
				       ? null
				       : GetNearPart(restNeighbor, alongBorder, distance, ref bufferFactory);
		}

		[NotNull]
		public static IPolyline GetDifference([NotNull] IPolyline polyline,
		                                      [NotNull] IPolyline otherLine,
		                                      double? xyTolerance = null)
		{
			// The lines are often very short and close to each other --> xy cluster tolerance workaround is used far too often
			// --> test for IRelationalOperator.Equals at least if lines are very short?
			// --> if equal, return empty geometry? or null?

			if (xyTolerance != null)
			{
				double maxLengthWorkaround = xyTolerance.Value * 6;

				if (polyline.Length <= maxLengthWorkaround &&
				    otherLine.Length < maxLengthWorkaround)
				{
					if (((IRelationalOperator) polyline).Equals(otherLine))
					{
						return GeometryFactory.CreateEmptyPolyline(polyline);
					}
				}
			}

			return (IPolyline) IntersectionUtils.Difference(polyline, otherLine);
		}

		[CanBeNull]
		public static IPolyline GetNearPart([NotNull] IPolyline toBuffer,
		                                    [NotNull] IPolyline line,
		                                    double distance,
		                                    ref BufferFactory bufferFactory)
		{
			IPolygon buffer = CreateSearchBuffer(toBuffer, distance, ref bufferFactory);
			return buffer == null
				       ? null
				       : GetLinearIntersection(line, buffer);
		}

		[CanBeNull]
		private static IPolygon CreateSearchBuffer(
			[NotNull] IGeometry geometry,
			double distance,
			[CanBeNull] ref BufferFactory bufferFactory)
		{
			// create exact buffer (not densified) to avoid slivers that result in errors
			if (bufferFactory == null)
			{
				bufferFactory = new BufferFactory
				                {
					                // NOTE: EndOption must be set FIRST, otherwise the result is not accurate
					                // (line end points don't touch buffer in some situations, some tolerance appears to be applied)
					                EndOption = esriBufferConstructionEndEnum.esriBufferFlat,
					                DensifyDeviation = 0.0001,
					                // NOTE this is required even when generating curves, otherwise the flat-end buffer is not accurate!! TODO use resolution?
					                GenerateCurves = true,
					                ExplodeBuffers = false,
					                UnionOverlappingBuffers = true
				                };
			}

			IList<IPolygon> result = bufferFactory.Buffer(geometry, distance);

			// NOTE flat end buffer result list (at least) can be empty for very short input geometries
			if (result.Count == 0)
			{
				return null;
			}

			Assert.AreEqual(1, result.Count, "Unexpected buffer result count");

			return result[0];
		}

		[NotNull]
		public static IPolyline GetLinearIntersection([NotNull] IPolyline line,
		                                              [NotNull] IGeometry geometry,
		                                              double? xyTolerance = null)
		{
			// the lines are often very short and close to each other --> xy cluster tolerance workaround is used far too often
			// --> test for IRelationalOperator.Equals at least if lines are very short?
			// --> if equal, return a clone of 'line'?

			if (xyTolerance != null)
			{
				double maxLengthWorkaround = xyTolerance.Value * 6;

				if (line.Length <= maxLengthWorkaround)
				{
					var otherLine = geometry as IPolyline;

					if (otherLine != null && otherLine.Length < maxLengthWorkaround)
					{
						if (((IRelationalOperator) line).Equals(otherLine))
						{
							return GeometryFactory.Clone(line);
						}
					}
				}
			}

			return (IPolyline) IntersectionUtils.Intersect(
				line, geometry,
				esriGeometryDimension.esriGeometry1Dimension);
		}

		[NotNull]
		public static IPolyline Union(
			[NotNull] IList<IPolyline> commonLines,
			[CanBeNull] ISpatialReference highResolutionSpatialReference)
		{
			if (highResolutionSpatialReference == null)
			{
				return (IPolyline) GeometryUtils.Union(commonLines);
			}

			ISpatialReference originalSpatialReference = commonLines[0].SpatialReference;

			foreach (IPolyline commonLine in commonLines)
			{
				commonLine.SpatialReference = highResolutionSpatialReference;
			}

			var result = (IPolyline) GeometryUtils.Union(commonLines);
			result.SpatialReference = originalSpatialReference;
			foreach (IPolyline commonLine in commonLines)
			{
				commonLine.SpatialReference = originalSpatialReference;
			}

			return result;
		}

		public static IEnumerable<AttributeConstraintViolation>
			GetAttributeConstraintViolations(
				[NotNull] IReadOnlyFeature feature, int classIndex,
				[NotNull] IReadOnlyFeature neighborFeature, int neighborClassIndex,
				[NotNull] RowPairCondition rowPairCondition,
				[NotNull] EqualFieldValuesCondition equalFieldValuesCondition,
				bool reportIndividually = false)
		{
			if (reportIndividually)
			{
				string message;
				IColumnNames errorColumnNames;
				if (! rowPairCondition.IsFulfilled(feature, classIndex,
				                                   neighborFeature,
				                                   neighborClassIndex,
				                                   out message,
				                                   out errorColumnNames))
				{
					var affectedFields = new HashSet<string>(
						errorColumnNames.GetInvolvedColumnNames(),
						StringComparer.OrdinalIgnoreCase);

					yield return new AttributeConstraintViolation(
						string.Format(
							LocalizableStrings
								.EdgeMatchUtils_AttributeConstraints_ConstraintNotFulfilled,
							message), affectedFields, message);
				}

				IEnumerable<UnequalField> unequalFields =
					equalFieldValuesCondition.GetNonEqualFields(feature, classIndex,
					                                            neighborFeature,
					                                            neighborClassIndex);
				foreach (UnequalField unequalField in unequalFields)
				{
					yield return new AttributeConstraintViolation(
						string.Format(
							LocalizableStrings.EdgeMatchUtils_AttributeConstraints_ValuesNotEqual,
							unequalField.Message), unequalField.FieldName, unequalField.Message);
				}
			}
			else
			{
				string description;
				string affectedComponents;
				string textValue;
				bool areConstraintsFulfilled = AreAttributeConstraintsFulfilled(
					feature, classIndex,
					neighborFeature, neighborClassIndex,
					rowPairCondition,
					equalFieldValuesCondition,
					out description, out affectedComponents, out textValue);

				if (! areConstraintsFulfilled)
				{
					yield return new AttributeConstraintViolation(description,
						affectedComponents,
						textValue);
				}
			}
		}

		[CanBeNull]
		public static string FormatAffectedComponents(
			[NotNull] ICollection<string> affectedFields)
		{
			return affectedFields.Count > 0
				       ? StringUtils.ConcatenateSorted(
					       affectedFields.Select(name => name.ToUpper()), " ")
				       : null;
		}

		public static bool IsWithinTolerance(double value, double tolerance)
		{
			if (value <= tolerance)
			{
				return true;
			}

			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (tolerance == 0)
			{
				// allow for insignificant double value difference to zero-tolerance value
				return MathUtils.AreSignificantDigitsEqual(value, tolerance);
			}

			return false;
		}

		private static bool HasUnequalFields(
			[NotNull] IEnumerable<UnequalField> unequalFields,
			[CanBeNull] out string message,
			[CanBeNull] HashSet<string> unequalFieldNames = null)
		{
			StringBuilder sb = null;

			foreach (UnequalField unequalField in unequalFields)
			{
				if (sb == null)
				{
					sb = new StringBuilder();
				}

				sb.AppendFormat(sb.Length == 0
					                ? "{0}"
					                : ";{0}", unequalField.Message);

				unequalFieldNames?.Add(unequalField.FieldName.ToUpper());
			}

			if (sb != null)
			{
				message = sb.ToString();
				return true;
			}

			message = null;
			return false;
		}

		[ContractAnnotation(
			"=>true, affectedComponents:canbenull;=>false, affectedComponents:notnull")]
		private static bool AreAttributeConstraintsFulfilled(
			[NotNull] IReadOnlyFeature feature, int classIndex,
			[NotNull] IReadOnlyFeature neighborFeature, int neighborClassIndex,
			[NotNull] RowPairCondition rowPairCondition,
			[NotNull] EqualFieldValuesCondition equalFieldValuesCondition,
			[NotNull] out string errorDescription,
			[CanBeNull] out string affectedComponents,
			[NotNull] out string textValue)
		{
			var affectedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			// compare attributes that are required to match
			var descriptionBuilder = new StringBuilder();
			var textValueBuilder = new StringBuilder();
			string message;
			IColumnNames errorColumnNames;
			if (! rowPairCondition.IsFulfilled(feature, classIndex,
			                                   neighborFeature,
			                                   neighborClassIndex,
			                                   out message,
			                                   out errorColumnNames))
			{
				descriptionBuilder.AppendFormat(
					LocalizableStrings.EdgeMatchUtils_AttributeConstraints_ConstraintNotFulfilled,
					message);

				foreach (string affectedColumn in errorColumnNames.GetInvolvedColumnNames())
				{
					affectedFields.Add(affectedColumn);
				}

				textValueBuilder.Append(message);
			}

			IEnumerable<UnequalField> unequalFields =
				equalFieldValuesCondition.GetNonEqualFields(feature, classIndex,
				                                            neighborFeature, neighborClassIndex);
			if (HasUnequalFields(unequalFields, out message, affectedFields))
			{
				if (descriptionBuilder.Length > 0)
				{
					descriptionBuilder.Append(". ");
				}

				descriptionBuilder.AppendFormat(
					LocalizableStrings.EdgeMatchUtils_AttributeConstraints_ValuesNotEqual,
					message);

				if (textValueBuilder.Length > 0)
				{
					textValueBuilder.Append("|");
				}

				textValueBuilder.Append(message);
			}

			errorDescription = descriptionBuilder.ToString();
			affectedComponents = FormatAffectedComponents(affectedFields);
			textValue = textValueBuilder.ToString();

			return errorDescription.Length == 0;
		}
	}
}
