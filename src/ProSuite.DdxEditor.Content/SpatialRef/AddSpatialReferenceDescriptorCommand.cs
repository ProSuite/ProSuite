using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;

namespace ProSuite.DdxEditor.Content.SpatialRef
{
	public class AddSpatialReferenceDescriptorCommand :
		AddItemCommandBase<SpatialReferenceDescriptorsItem>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AddSpatialReferenceDescriptorCommand"/> class.
		/// </summary>
		/// <param name="spatialReferenceDescriptorsItem">The spatial reference descriptors item.</param>
		/// <param name="applicationController">The application controller.</param>
		public AddSpatialReferenceDescriptorCommand(
			[NotNull] SpatialReferenceDescriptorsItem spatialReferenceDescriptorsItem,
			[NotNull] IApplicationController applicationController)
			: base(spatialReferenceDescriptorsItem, applicationController) { }

		public override string Text => "Add Spatial Reference Descriptor";

		protected override void ExecuteCore()
		{
			Item.AddSpatialReferenceDescriptorItem();
		}
	}
}
