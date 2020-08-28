using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using NUnit.Framework.Internal.Execution;
using ProSuite.Commons.AGP.Storage;

namespace ProSuite.AGP.WorkList.Test
{
	public class WorkItemsGdbTableRepository : GdbRepository<WorkItem,Table>
	{
		public WorkItemsGdbTableRepository(string gdbPath, string className = null) : base(
			gdbPath, className)
		{

		}


	}
}
