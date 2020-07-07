namespace ProSuite.Commons.UI.ScreenBinding
{
	public interface IBoundPart
	{
		IScreenBinder Binder { set; }

		string FieldName { get; }

		void Bind(object model);

		bool ApplyChanges();

		void Reset();

		void Update();

		void StopBinding();

		void SetDefaults();

		bool IsDirty();
	}
}
