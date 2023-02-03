using System.Xml.Serialization;

namespace ProSuite.Microservices.Server.AO.QA
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

	}
}
