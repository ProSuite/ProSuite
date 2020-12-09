using System.Linq;
using ProSuite.AGP.WorkList.Domain;

namespace ProSuite.AGP.Solution.WorkListUI
{
	public class InvolvedTableVm
	{
		private readonly InvolvedTable _table;

		public InvolvedTableVm(InvolvedTable table)
		{
			_table = table;
		}

		public string Table
		{
			get { return _table.TableName; }
			set { }
		}

		public int Oid
		{
			get { return _table.RowReferences.First().OID; }
			set { }
		}

		public string KeyField
		{
			get { return _table.KeyField; }
			set { }
		}
	}
}
