using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList
{
	public interface IWorkListDefinition
	{
		/// <summary>
		/// The XML or whatever file format that is used to persist this definition
		/// </summary>
		string Path { get; set; }
	}

	public class IssueWorkListDefinition : IWorkListDefinition
	{
		public string Path { get; set; }

		public string FgdbPath { get; set; }

		/// <summary>
		/// The visited items' gdb reference, which are persisted in the definition file
		/// rather than the geodatabase (this is just a suggestion)
		/// </summary>
		public List<GdbRowIdentity> VisitedItems { get; } = new List<GdbRowIdentity>();
	}
}
