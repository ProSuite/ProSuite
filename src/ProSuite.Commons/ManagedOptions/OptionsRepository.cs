using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.ManagedOptions
{
	/// <summary>
	/// Repository class that provides and maintains the (single) instance of
	/// the respective options class.
	/// </summary>
	/// <typeparam name="TOptions">The options type or interface used by clients.
	/// It can be an interface but the options class created by the provided
	/// factory method must derive from <see cref="OptionsBase{TPartialOptions}"/>.</typeparam>
	/// <typeparam name="TPartialOptions">The partial options type used to
	/// construct the TOptions./></typeparam>
	public class OptionsRepository<TOptions, TPartialOptions> : IOptionsRepository<TOptions>
		where TOptions : class
		where TPartialOptions : PartialOptionsBase
	{
		private TOptions _options;
		private readonly OverridableSettingsProvider<TPartialOptions> _settingsProvider;

		private readonly Func<TPartialOptions, TPartialOptions, TOptions> _factoryMethod;

		/// <summary>
		/// Initializes a new instance of the <see cref="OptionsRepository&lt;TOptions, TPartialOptions&gt;"/> class.
		/// </summary>
		/// <param name="settingsProvider">The settings provider used for serialization / deserialization.</param>
		/// <param name="factoryMethod">The method to be used to create the TOptions object.
		/// The created object must derive from <see cref="OptionsBase{TPartialOptions}"/>.
		/// </param>
		public OptionsRepository(
			[NotNull] OverridableSettingsProvider<TPartialOptions> settingsProvider,
			[NotNull] Func<TPartialOptions, TPartialOptions, TOptions> factoryMethod)
		{
			// TODO: To get rid of the factory method paramter:
			//       Add abstract Initialize(TPartialOptions, TPartialOptions) to base class
			//       --> add parameter-less constructor to TOptions (and ... where TOptions : OptionsBase<TPartialOptions, new())
			//       and change *all* implementation

			_settingsProvider = settingsProvider;
			_factoryMethod = factoryMethod;
		}

		[NotNull]
		public TOptions GetOptions()
		{
			if (_options == null)
			{
				TPartialOptions central, local;

				_settingsProvider.GetConfigurations(out local, out central);

				_options = _factoryMethod(central, local);
			}

			return _options;
		}

		public void Update([NotNull] TOptions options)
		{
			_options = options;

			var overridableOptions = options as OptionsBase<TPartialOptions>;

			TPartialOptions localOptions =
				Assert.NotNull(overridableOptions).LocalOptions;

			_settingsProvider.StoreLocalConfiguration(localOptions);
		}

		public string GetStorageLocationMessage()
		{
			return _settingsProvider.GetXmlLocationLogMessage();
		}
	}
}