using System;
using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Keyboard;

namespace ProSuite.DomainModel.Core.Commands
{
	public interface ICommandDescriptorRepository : IRepository<CommandDescriptor>
	{
		CommandDescriptor Get(Guid clsid, int? subtype);

		IList<CommandDescriptor> GetSubTypeCommands(Guid clsid);

		IList<CommandDescriptor> Get(KeyboardShortcut shortcut);
	}
}
