using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.QA.TestReport
{
	public abstract class IncludedInstance : IncludedInstanceBase
	{
		protected IncludedInstance([NotNull] string title,
		                           [NotNull] Assembly assembly,
		                           [NotNull] InstanceFactory instanceFactory,
		                           bool obsolete,
		                           bool internallyUsed)
			: base(title, assembly, obsolete, internallyUsed, instanceFactory.TestCategories)
		{
			Assert.ArgumentNotNull(instanceFactory, nameof(instanceFactory));

			InstanceFactory = instanceFactory;
		}

		[NotNull]
		public InstanceFactory InstanceFactory { get; }

		public override string Description => InstanceFactory.GetTestDescription();
	}
}
