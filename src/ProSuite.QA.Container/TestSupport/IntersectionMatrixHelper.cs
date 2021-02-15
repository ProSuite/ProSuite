using System.Collections.Generic;
using System.Globalization;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public class IntersectionMatrixHelper : RowPairCondition
	{
		private readonly IntersectionMatrix _intersectionMatrix;
		private readonly ICollection<esriGeometryDimension> _validIntersectionDimensions;
		private readonly GeometryConstraint _intersectionGeometryConstraint;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="IntersectionMatrixHelper" /> class.
		/// </summary>
		/// <param name="intersectionMatrix">The intersection matrix.</param>
		/// <param name="validRelationConstraint">The match constraint. If the constraint is defined and fulfilled
		/// for a given row pair that matches the intersection matrix, then no error is reported.</param>
		/// <param name="constraintIsDirected">if set to <c>true</c> the constraint is directed,
		/// i.e. row1/row2 only maps to G1/G2 and not also to G2/G1.</param>
		/// <param name="validIntersectionDimensions">The valid intersection dimensions.</param>
		/// <param name="constraintIsCaseSensitive">Indicates if the constraint is case sensitive</param>
		/// <param name="intersectionGeometryConstraint">The intersection geometry constraint.</param>
		public IntersectionMatrixHelper(
			[NotNull] IntersectionMatrix intersectionMatrix,
			[CanBeNull] string validRelationConstraint = null,
			bool constraintIsDirected = false,
			[CanBeNull] ICollection<esriGeometryDimension> validIntersectionDimensions = null,
			bool constraintIsCaseSensitive = false,
			GeometryConstraint intersectionGeometryConstraint = null)
			: base(validRelationConstraint, constraintIsDirected, false,
			       constraintIsCaseSensitive)
		{
			Assert.ArgumentNotNull(intersectionMatrix, nameof(intersectionMatrix));

			_intersectionMatrix = intersectionMatrix;
			_validIntersectionDimensions = validIntersectionDimensions;
			_intersectionGeometryConstraint = intersectionGeometryConstraint;
		}

		#endregion

		public int ReportErrors([NotNull] IFeature feature1, int tableIndex1,
		                        [NotNull] IFeature feature2, int tableIndex2,
		                        [NotNull] IErrorReporting reportError,
		                        [CanBeNull] IssueCode issueCode,
		                        bool reportIndividualErrors)
		{
			Assert.ArgumentNotNull(feature1, nameof(feature1));
			Assert.ArgumentNotNull(feature2, nameof(feature2));
			Assert.ArgumentNotNull(reportError, nameof(reportError));

			if (feature1 == feature2)
			{
				return 0;
			}

			string conditionMessage;
			if (IsFulfilled(feature1, tableIndex1,
			                feature2, tableIndex2,
			                out conditionMessage))
			{
				// currently: no error, even if intersection geometry constraint would be violated
				// (parameter for constraint combination operation could be added)
				return 0;
			}

			// geometries of multiple dimensions possible
			var errorCount = 0;

			foreach (IGeometry geometry in GetIntersections(feature1, feature2))
			{
				foreach (IGeometry reportableGeometry in GetGeometries(geometry,
				                                                       reportIndividualErrors))
				{
					if (_validIntersectionDimensions != null &&
					    _validIntersectionDimensions.Contains(reportableGeometry.Dimension))
					{
						continue;
					}

					if (_intersectionGeometryConstraint != null)
					{
						if (_intersectionGeometryConstraint.IsFulfilled(reportableGeometry))
						{
							continue;
						}

						string displayValues =
							_intersectionGeometryConstraint.FormatValues(reportableGeometry,
							                                             CultureInfo.CurrentCulture)
							                               .Replace("$", string.Empty);
						string rawValues =
							_intersectionGeometryConstraint.FormatValues(reportableGeometry,
							                                             CultureInfo
								                                             .InvariantCulture);

						// format for display to the user (current culture)
						string description = GetErrorDescription(
							conditionMessage,
							_intersectionGeometryConstraint.Constraint.Replace("$", string.Empty),
							displayValues);

						errorCount += reportError.Report(description, reportableGeometry,
						                                 issueCode, null,
						                                 new object[] {rawValues},
						                                 feature1, feature2);
					}
					else
					{
						string description = GetErrorDescription(conditionMessage);

						errorCount += reportError.Report(description, reportableGeometry,
						                                 issueCode, null,
						                                 feature1, feature2);
					}
				}
			}

			return errorCount;
		}

		#region Non-public

		[NotNull]
		private static string GetErrorDescription(
			[CanBeNull] string conditionMessage,
			[NotNull] string intersectionGeometryConstraint,
			[NotNull] string intersectionConstraintValues)
		{
			return string.Format(
				"{0}; intersection geometry constraint is not fulfilled: {1} ({2})",
				GetErrorDescription(conditionMessage),
				intersectionGeometryConstraint,
				intersectionConstraintValues);
		}

		[NotNull]
		private static string GetErrorDescription([CanBeNull] string conditionMessage)
		{
			if (string.IsNullOrEmpty(conditionMessage))
			{
				return "Features have an invalid spatial relationship";
			}

			return string.Format(
				"Features involved in spatial relationship " +
				"don't fulfill constraint: {0}", conditionMessage);
		}

		[NotNull]
		private static IEnumerable<IGeometry> GetGeometries([NotNull] IGeometry geometry,
		                                                    bool reportIndividualErrors)
		{
			if (geometry.IsEmpty)
			{
				yield break;
			}

			if (reportIndividualErrors)
			{
				foreach (IGeometry part in GeometryUtils.Explode(geometry))
				{
					if (! part.IsEmpty)
					{
						yield return part;
					}
				}
			}
			else
			{
				yield return geometry;
			}
		}

		[NotNull]
		private IEnumerable<IGeometry> GetIntersections([NotNull] IFeature feature1,
		                                                [NotNull] IFeature feature2)
		{
			IGeometry g1 = feature1.Shape;
			IGeometry g2 = feature2.Shape;

			return _intersectionMatrix.GetIntersections(g1, g2);
		}

		#endregion
	}
}
