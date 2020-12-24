using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProSuite.AGP.WorkList.Domain;

namespace ProSuite.AGP.Solution.WorkListUI
{
	public class InvolvedObjectRow
	{
		public InvolvedObjectRow(string name, string keyField, int objectId)
		{
			Name = name;
			KeyField = keyField;
			ObjectId = objectId;
		}

		public string Name { get; set; }
		public string KeyField { get; set; }
		public int ObjectId { get; set; }

		public static List<InvolvedObjectRow> CreateObjectRows(InvolvedTable table)
		{
			List<InvolvedObjectRow> rows = new List<InvolvedObjectRow>();
			foreach (var reference in table.RowReferences)
			{
				rows.Add(new InvolvedObjectRow(table.TableName, table.KeyField, reference.OID));
			}

			return rows;
		}
	}
}
