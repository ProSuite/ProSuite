using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainServices.AO.QA
{
	public interface ILocationBasedQualitySpecification
	{
		void ResetCurrentFeature();

		void SetCurrentTile([CanBeNull] IEnvelope currentTile);

		/// <summary>
		/// Determines whether the current feature is to be tested for the given quality condition.
		/// </summary>
		/// <param name="feature">The feature.</param>
		/// <param name="recycled">true if feature is created by a recycling cursor</param>
		/// <param name="recycleUnique">unique Guid for each row of a recyling cursor</param>
		/// <param name="qualityCondition">The quality condition.</param>
		/// <param name="ignoreTestArea">do not clip location based specifications to test area</param>
		/// <returns>
		///   <c>true</c> if the feature is to be tested for the quality condition given its location; otherwise, <c>false</c>.
		/// </returns>
		bool IsFeatureToBeTested([NotNull] IReadOnlyFeature feature,
		                         bool recycled,
		                         Guid recycleUnique,
		                         [NotNull] QualityCondition qualityCondition,
		                         bool ignoreTestArea);

		/// <summary>
		/// Determines whether a specified error is relevant for its location.
		/// </summary>
		/// <param name="errorGeometry">The error geometry.</param>
		/// <param name="qualityCondition">The quality condition.</param>
		/// <param name="involvedRows">The involved rows.</param>
		/// <returns>
		///   <c>true</c> if the error is relevant for the location; otherwise, <c>false</c>.
		/// </returns>
		bool IsErrorRelevant(
			[NotNull] IGeometry errorGeometry,
			[NotNull] QualityCondition qualityCondition,
			[NotNull] ICollection<InvolvedRow> involvedRows);

		/// <summary>
		/// The unionized specification consisting of all conditions of all locations.
		/// </summary>
		QualitySpecification QualitySpecification { get; }
	}
}
