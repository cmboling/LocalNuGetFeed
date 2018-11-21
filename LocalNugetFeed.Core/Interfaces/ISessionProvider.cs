namespace LocalNugetFeed.Core.Interfaces
{
	public interface ISessionProvider<out T>
	{
		T Session { get; }
	}
}