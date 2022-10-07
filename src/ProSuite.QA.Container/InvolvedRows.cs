using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	public class InvolvedRows : List<InvolvedRow>
	{
		public bool HasAdditionalRows { get; set; }

		[NotNull]
		public List<IReadOnlyRow> TestedRows =>
			_testedRows ?? (_testedRows = new List<IReadOnlyRow>());

		private List<IReadOnlyRow> _testedRows;
	}
}
