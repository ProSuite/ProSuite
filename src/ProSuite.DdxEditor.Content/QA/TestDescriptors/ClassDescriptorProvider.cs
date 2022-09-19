using System.Windows.Forms;
using ProSuite.DomainModel.Core;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors
{
	internal delegate ClassDescriptor ClassDescriptorProvider(
		IWin32Window owner, ClassDescriptor orig);
}
