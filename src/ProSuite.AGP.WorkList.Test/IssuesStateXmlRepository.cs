using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.AGP.Storage;

namespace ProSuite.AGP.WorkList.Test
{
	public class IssuesStateXmlRepository : XmlRepository<GdbRowIdentity>
	{
		//private readonly IList<GdbRowIdentity> _issueStates = new List<GdbRowIdentity>();
		private readonly string _issueStatePath;

		public IssuesStateXmlRepository(string filename) : base(filename)
		{
			_issueStatePath = filename;
		}
		//public IList<GdbRowIdentity> GetStates()
		//{
		//	return GetAll();
		//}

		

	}
}
