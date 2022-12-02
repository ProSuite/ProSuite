using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Core.Reports
{
	public abstract class IncludedInstance : IncludedInstanceBase
	{
		protected IncludedInstance([NotNull] string title,
		                           [NotNull] Assembly assembly,
		                           [NotNull] IInstanceInfo instanceInfo,
		                           bool obsolete,
		                           bool internallyUsed)
			: base(title, assembly, obsolete, internallyUsed, instanceInfo.TestCategories)
		{
			Assert.ArgumentNotNull(instanceInfo, nameof(instanceInfo));

			InstanceInfo = instanceInfo;
		}

		[NotNull]
		public IInstanceInfo InstanceInfo { get; }

		public override string Description => InstanceInfo.TestDescription;

		public override string IndexTooltip => InstanceInfo.TestDescription;
	}
}
