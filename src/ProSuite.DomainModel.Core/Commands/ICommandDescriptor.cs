using ProSuite.Commons.Keyboard;

namespace ProSuite.DomainModel.Core.Commands
{
	public interface ICommandDescriptor
	{
		string Identifier { get; }

		string Name { get; set; }

		CommandType CommandType { get; set; }

		KeyboardShortcut KeyboardShortcut { get; set; }
	}
}
