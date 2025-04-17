namespace ProSuite.GIS.Geodatabase.API
{
	public interface ISet
	{
		void Add(object unk);

		void Remove(object unk);

		void RemoveAll();

		object Find(object unk);

		object Next();

		void Reset();

		int Count { get; }
	}
}
