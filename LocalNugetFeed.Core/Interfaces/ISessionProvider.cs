namespace LocalNugetFeed.Core.Interfaces
{
	/// <summary>
	/// Generic session provider interface 
	/// </summary>
	/// <typeparam name="T">type of session provider</typeparam>
	public interface ISessionProvider<out T>
	{
		T Session { get; }
	}
}