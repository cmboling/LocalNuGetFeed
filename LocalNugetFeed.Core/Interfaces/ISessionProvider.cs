namespace LocalNugetFeed.Core.Interfaces
{
	public interface ISessionProvider<T>
	{
		T Session { get; set; }
	}
}