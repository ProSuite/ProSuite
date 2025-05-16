using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DomainModel.Core.Geodatabase;

namespace ProSuite.DdxEditor.Content.Connections
{
	internal class AddConnectionProviderCommand<E> :
		AddItemCommandBase<ConnectionProvidersItem>
		where E : ConnectionProvider, new()
	{
		private readonly string _shortText;
		private readonly string _text;

		public AddConnectionProviderCommand(
			[NotNull] ConnectionProvidersItem parentItem,
			[NotNull] IApplicationController applicationController,
			[NotNull] string text)
			: this(parentItem, applicationController, text, text) { }

		public AddConnectionProviderCommand(
			[NotNull] ConnectionProvidersItem parentItem,
			[NotNull] IApplicationController applicationController,
			[NotNull] string text,
			[NotNull] string shortText)
			: base(parentItem, applicationController)
		{
			Assert.ArgumentNotNullOrEmpty(text, nameof(text));
			Assert.ArgumentNotNullOrEmpty(shortText, nameof(shortText));

			_text = text;
			_shortText = shortText;
		}

		public override string Text => _text;

		public override string ShortText => _shortText;

		protected override void ExecuteCore()
		{
			Item.AddConnectionProvider<E>();
		}
	}
}
