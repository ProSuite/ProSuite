using System.IO;
using System.Reflection;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Content.QA.InstanceDescriptors
{
	internal static class TestAssemblyUtils
	{
		[CanBeNull]
		public static string ChooseAssemblyFileName()
		{
			using (FileDialog dialog = new OpenFileDialog())
			{
				var assemblyPath = new FileInfo(Assembly.GetExecutingAssembly().Location);
				string directoryName = assemblyPath.DirectoryName;

				if (Directory.Exists(directoryName))
				{
					dialog.RestoreDirectory = true;
					dialog.InitialDirectory = directoryName;
				}

				dialog.Filter = @"Assembly (*.dll)|*.dll|Executable (*.exe)|*.exe";

				return dialog.ShowDialog() != DialogResult.OK
					       ? null
					       : dialog.FileName;
			}
		}
	}
}
