using System.Text;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace LocalNugetFeed.Core.Extensions
{
	public static class SessionExtensions
	{
		public static void Set<T>(this ISession session, string key, T value)
		{
			session.Set(key, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value)));
		}

		public static T Get<T>(this ISession session, string key)
		{
			session.TryGetValue(key, out byte[] value);

			string data = value != null ? Encoding.UTF8.GetString(value) : null;

			return data == null ? default(T) : JsonConvert.DeserializeObject<T>(data);
		}
	}
}