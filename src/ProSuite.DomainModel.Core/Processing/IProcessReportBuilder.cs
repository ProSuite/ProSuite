using System;
using System.IO;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.Processing
{
	public interface IProcessReportBuilder
	{
		bool IncludeAssemblyInfo { get; set; }
		bool IncludeObsolete { get; set; }

		void AddHeaderItem([NotNull] string name, string value = null);

		void AddProcessType([NotNull] Type processType,
		                    string registeredName = null,
		                    string registeredDescription = null);

		void WriteReport([NotNull] Stream stream);
	}
}
