using System;
using System.ComponentModel;
using System.Resources;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Globalization
{
	[CLSCompliant(false)]
	public abstract class LocalizedCategoryAttribute : CategoryAttribute
	{
		[NotNull] private readonly ResourceManager _resourceManager;
		[NotNull] private readonly string _resourceName;

		/// <summary>
		/// Initializes a new instance of the <see cref="LocalizedCategoryAttribute"/> class.
		/// </summary>
		/// <param name="resourceManager">The resource manager.</param>
		/// <param name="resourceName">Name of the resource.</param>
		protected LocalizedCategoryAttribute([NotNull] ResourceManager resourceManager,
		                                     [NotNull] string resourceName)
		{
			Assert.ArgumentNotNull(resourceManager, nameof(resourceManager));
			Assert.ArgumentNotNullOrEmpty(resourceName, nameof(resourceName));

			_resourceManager = resourceManager;
			_resourceName = resourceName;
		}

		protected sealed override string GetLocalizedString(string value)
		{
			return _resourceManager.GetString(_resourceName);
		}
	}
}
