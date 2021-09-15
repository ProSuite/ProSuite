using System;
using System.ComponentModel;
using System.Resources;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Globalization
{
	/// <summary>
	/// Base class for localizable description attributes.
	/// </summary>
	/// <remarks>The CurrentUICulture of the current thread is used for looking up localized resources.</remarks>
	[CLSCompliant(false)]
	public abstract class LocalizedDescriptionAttribute : DescriptionAttribute
	{
		[NotNull] private readonly ResourceManager _resourceManager;
		[NotNull] private readonly string _resourceName;
		private bool _localized;

		/// <summary>
		/// Initializes a new instance of the <see cref="LocalizedDescriptionAttribute"/> class.
		/// </summary>
		/// <param name="resourceManager">The resource manager.</param>
		/// <param name="resourceName">Name of the resource.</param>
		protected LocalizedDescriptionAttribute([NotNull] ResourceManager resourceManager,
		                                        [NotNull] string resourceName)
		{
			Assert.ArgumentNotNull(resourceManager, nameof(resourceManager));
			Assert.ArgumentNotNullOrEmpty(resourceName, nameof(resourceName));

			_resourceManager = resourceManager;
			_resourceName = resourceName;
		}

		public override string Description
		{
			get
			{
				if (! _localized)
				{
					DescriptionValue = _resourceManager.GetString(_resourceName);
					_localized = true;
				}

				return DescriptionValue;
			}
		}

		public string ResourceName => _resourceName;
	}
}
