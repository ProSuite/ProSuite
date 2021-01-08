using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	/// <summary>
	/// Interface for reporting QA errors
	/// </summary>
	[CLSCompliant(false)]
	public interface IErrorReporting
	{
		int Report([NotNull] string description,
		           params IRow[] rows);

		int Report([NotNull] string description,
		           [CanBeNull] IssueCode issueCode,
		           [CanBeNull] string affectedComponent,
		           params IRow[] rows);

		int Report([NotNull] string description,
		           [CanBeNull] IGeometry errorGeometry,
		           params IRow[] rows);

		int Report([NotNull] string description,
		           [CanBeNull] IGeometry errorGeometry,
		           [CanBeNull] IssueCode issueCode,
		           [CanBeNull] string affectedComponent,
		           params IRow[] rows);

		int Report([NotNull] string description,
		           [CanBeNull] IGeometry errorGeometry,
		           [CanBeNull] IssueCode issueCode,
		           bool reportIndividualParts,
		           params IRow[] rows);

		int Report([NotNull] string description,
		           [CanBeNull] IGeometry errorGeometry,
		           [CanBeNull] IssueCode issueCode,
		           [CanBeNull] string affectedComponent,
		           [CanBeNull] IEnumerable<object> values,
		           params IRow[] rows);

		int Report([NotNull] string description,
		           [CanBeNull] IGeometry errorGeometry,
		           [CanBeNull] IssueCode issueCode,
		           [CanBeNull] string affectedComponent,
		           bool reportIndividualParts,
		           params IRow[] rows);
	}
}
