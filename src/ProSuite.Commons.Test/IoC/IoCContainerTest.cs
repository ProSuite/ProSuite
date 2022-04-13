using NUnit.Framework;
using ProSuite.Commons.IoC;
using ProSuite.Commons.Notifications;

namespace ProSuite.Commons.Test.IoC
{
	[TestFixture]
	public class IoCContainerTest
	{
		[Test]
		public void CanRegisterComponent()
		{
			IoCContainer container = new IoCContainer();

			var component = new Notification("bla");
			container.Register<INotification>(component);

			INotification resolved = container.Resolve<INotification>();

			Assert.NotNull(resolved);
			Assert.IsTrue(resolved == component);
		}

		[Test]
		public void CanRegisterNamedComponent()
		{
			IoCContainer container = new IoCContainer();

			// Register another, un-named component
			var bliComponent = new Notification("bli");
			container.Register<INotification>(bliComponent);

			// Register named
			var blaComponent = new Notification("bla");
			container.Register<INotification>(blaComponent, "bla_component");

			INotification resolved = container.Resolve<INotification>("bla_component");

			Assert.NotNull(resolved);
			Assert.IsTrue(resolved == blaComponent);
		}
	}
}
