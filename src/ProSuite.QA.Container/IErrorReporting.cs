using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	/// <summary>
	/// Interface for reporting QA errors
	/// </summary>
	public interface IErrorReporting
	{
		int Report([NotNull] string description,
		           params IReadOnlyRow[] rows);

		int Report([NotNull] string description,
		           [CanBeNull] IssueCode issueCode,
		           [CanBeNull] string affectedComponent,
		           params IReadOnlyRow[] rows);

		int Report([NotNull] string description,
		           [CanBeNull] IGeometry errorGeometry,
		           params IReadOnlyRow[] rows);

		int Report([NotNull] string description,
		           [CanBeNull] IGeometry errorGeometry,
		           [CanBeNull] IssueCode issueCode,
		           [CanBeNull] string affectedComponent,
		           params IReadOnlyRow[] rows);

		int Report([NotNull] string description,
		           [CanBeNull] IGeometry errorGeometry,
		           [CanBeNull] IssueCode issueCode,
		           bool reportIndividualParts,
		           params IReadOnlyRow[] rows);

		int Report([NotNull] string description,
		           [CanBeNull] IGeometry errorGeometry,
		           [CanBeNull] IssueCode issueCode,
		           [CanBeNull] string affectedComponent,
		           [CanBeNull] IEnumerable<object> values,
		           params IReadOnlyRow[] rows);

		int Report([NotNull] string description,
		           [CanBeNull] IGeometry errorGeometry,
		           [CanBeNull] IssueCode issueCode,
		           [CanBeNull] string affectedComponent,
		           bool reportIndividualParts,
		           params IReadOnlyRow[] rows);
	}
}
