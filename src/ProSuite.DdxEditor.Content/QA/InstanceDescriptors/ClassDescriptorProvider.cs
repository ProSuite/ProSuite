using System.Windows.Forms;
using ProSuite.DomainModel.Core;

namespace ProSuite.DdxEditor.Content.QA.InstanceDescriptors
{
	internal delegate ClassDescriptor ClassDescriptorProvider(
		IWin32Window owner, ClassDescriptor orig);
}
