using System.Collections.Generic;

namespace ProSuite.AGP.Solution.WorkListUI.UiTestData
{
	public static class InvolvedObjectMock
	{
		public static List<InvolvedObjectRow> rows = new List<InvolvedObjectRow>()
		                                             {
			                                             new InvolvedObjectRow(
				                                             "Gewässer", "field a", 222333),
			                                             new InvolvedObjectRow(
				                                             "Gebäude innerhalb 1", "keyfield ddkd",
				                                             222333338),
			                                             new InvolvedObjectRow(
				                                             "Siedlungsgebiet Projektiert", "Name",
				                                             45333338)
		                                             };
	}
}
