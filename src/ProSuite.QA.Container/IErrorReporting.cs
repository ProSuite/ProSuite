using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core.IssueCodes;

namespace ProSuite.QA.Container
{
	/// <summary>
	/// Interface for reporting QA errors
	/// </summary>
	public interface IErrorReporting
	{
		int Report([NotNull] string description,
				   [NotNull] InvolvedRows rows,
		           [CanBeNull] IGeometry errorGeometry,
		           [CanBeNull] IssueCode issueCode,
		           [CanBeNull] string affectedComponent,
				   bool reportIndividualParts = false,
		           [CanBeNull] IEnumerable<object> values = null);
	}
}
