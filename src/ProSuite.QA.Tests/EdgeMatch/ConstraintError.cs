using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core.IssueCodes;

namespace ProSuite.QA.Tests.EdgeMatch
{
	internal class ConstraintError<T> where T : EdgeMatchBorderConnection
	{
		public T BorderConnection { get; set; }

		public T NeighborBorderConnection { get; set; }

		public IPolyline ErrorLine { get; set; }

		public IssueCode IssueCode { get; set; }

		public string AffectedComponents { get; set; }

		public string ConstraintDescription { get; set; }

		[CanBeNull]
		public string TextValue { get; set; }
	}
}
