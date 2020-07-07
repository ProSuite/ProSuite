using ProSuite.Commons.UI.Persistence.WinForms;

namespace ProSuite.Commons.UI.Finder
{
	public class TypeFinderFormState : FormState
	{
		private string _assemblyPath;

		public string AssemblyPath
		{
			get { return _assemblyPath; }
			set { _assemblyPath = value; }
		}
	}
}
