using System.Windows.Forms;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors
{
	internal delegate ClassDescriptor ClassDescriptorProvider(
		IWin32Window owner, ClassDescriptor orig);
}
