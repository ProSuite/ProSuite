using System.Drawing;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Dialogs;
using ProSuite.Commons.UI.Env;
using ProSuite.DdxEditor.Framework.EntityProperties;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.Properties;

namespace ProSuite.DdxEditor.Framework.Commands
{
	public class ShowEntityPropertiesCommand<E, BASE> :
		ItemCommandBase<EntityItem<E, BASE>>
		where BASE : Entity
		where E : BASE
	{
		private static readonly Image _image;

		static ShowEntityPropertiesCommand()
		{
			_image = Resources.Properties;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ShowEntityPropertiesCommand&lt;E, BASE&gt;"/> class.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="applicationController">The application controller.</param>
		public ShowEntityPropertiesCommand(
			[NotNull] EntityItem<E, BASE> item,
			[NotNull] IApplicationController applicationController)
			: base(item, applicationController) { }

		#region Overrides of CommandBase

		public override string Text => "Properties";

		public override Image Image => _image;

		protected override void ExecuteCore()
		{
			E entity = ApplicationController.ReadInTransaction(() => Item.GetEntity());

			if (entity == null)
			{
				Dialog.Warning(ApplicationController.Window, Text,
				               "The entity no longer exists");
				return;
			}

			using (var form = new EntityPropertiesForm(entity))
			{
				UIEnvironment.ShowDialog(form, ApplicationController.Window);
			}
		}

		#endregion
	}
}
