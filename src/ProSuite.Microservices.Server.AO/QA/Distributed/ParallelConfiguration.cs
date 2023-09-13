using System.Collections.Generic;
using System.Xml.Serialization;
using ProSuite.DomainServices.AO.QA;

namespace ProSuite.Microservices.Server.AO.QA.Distributed
{
	public class ParallelConfiguration
	{
		/// <summary>
		/// Maximum number of tasks handling non container tests
		/// if &lt;= 0: no limitation 
		/// </summary>
		[XmlAttribute]
		public int MaxNonContainerTasks { get; set; }

		/// <summary>
		/// Maximum tasks for container tests that cannot be tiled
		/// if &lt;= 0: number of processes - number of split area tasks, but at least number of processes / 2  
		/// </summary>
		[XmlAttribute]
		public int MaxFullAreaTasks { get; set; }

		/// <summary>
		/// Maximum split areas for test that can be tiled 
		/// if &lt;= 0: corresponding to MinimumSplitAreaExtent  
		/// </summary>
		[XmlAttribute]
		public int MaxSplitAreaTasks { get; set; }

		/// <summary>
		/// Minimum extent of a split area. 
		/// if &lt;= 0: tile size  
		/// </summary>
		[XmlAttribute]
		public double MinimumSplitAreaExtent { get; set; }

		[XmlArray]
		public List<QualityConditionExecType> TypePriority { get; set; }

		/// <summary>
		/// if yes: Count the number of objects per parallel tile verification in a seperate task
		/// and execute these verifications in descending order, after the task finished
		/// </summary>
		[XmlAttribute]
		public bool SortByNumberOfObjects { get; set; }
	}
}
