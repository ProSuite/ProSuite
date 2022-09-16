using System;

namespace ProSuite.DdxEditor.Framework.Items
{
	public interface IEntityTypeItem
	{
		bool IsBasedOn(Type type);
	}
}
