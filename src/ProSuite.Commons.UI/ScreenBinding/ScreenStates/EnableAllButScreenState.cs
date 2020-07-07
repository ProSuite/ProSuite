namespace ProSuite.Commons.UI.ScreenBinding.ScreenStates
{
	public class EnableAllButScreenState : IScreenState
	{
		private readonly ControlSet _internalSet = new ControlSet();

		#region IScreenState Members

		public bool IsControlEnabled(object control)
		{
			return ! _internalSet.Contains(control);
		}

		#endregion

		public void Disable(object[] controls)
		{
			_internalSet.AddRange(controls);
		}
	}
}
