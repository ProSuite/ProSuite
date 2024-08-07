using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	/// <summary>
	/// Used for caching properties that require loading the test factory 
	/// </summary>
	internal class TestProperties
	{
		public TestProperties([NotNull] TestDescriptor testDescriptor)
		{
			Assert.ArgumentNotNull(testDescriptor, nameof(testDescriptor));

			try
			{
				IInstanceInfo instanceInfo =
					InstanceDescriptorUtils.GetInstanceInfo(testDescriptor);

				Signature = InstanceUtils.GetTestSignature(instanceInfo);
				Description = instanceInfo.TestDescription ?? string.Empty;
			}
			catch (TypeLoadException e)
			{
				TestLoadException = e;

				Signature = string.Format("Error: {0}", e.Message);
				Description = string.Format("Error: {0}", e.Message);
			}
		}

		[NotNull]
		public string Signature { get; }

		[NotNull]
		public string Description { get; }

		[CanBeNull]
		public Exception TestLoadException { get; }
	}
}
