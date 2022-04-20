using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	public class InvolvedRows : List<InvolvedRow>
	{
		public bool HasAdditionalRows { get; set; }

		[NotNull]
		public List<IRow> TestedRows => _testedRows ?? (_testedRows = new List<IRow>());

		private List<IRow> _testedRows;
	}
}
