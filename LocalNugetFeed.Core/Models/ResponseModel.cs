using System.Net;
using LocalNugetFeed.Core.Interfaces;
using Newtonsoft.Json;

namespace LocalNugetFeed.Core.Models
{
	public class ResponseModel<T>
	{
		public string Message { get; }
		public HttpStatusCode StatusCode { get; }
		public T Data { get; }
		public bool Success => StatusCode == HttpStatusCode.OK || StatusCode == HttpStatusCode.NoContent;

		[JsonConstructor]
		public ResponseModel(HttpStatusCode statusCode, T data, string message = null)
		{
			Data = data;
			Message = message;
			StatusCode = statusCode;
		}

		public ResponseModel(HttpStatusCode statusCode, string message = null)
		{
			Message = message;
			StatusCode = statusCode;
		}
	}

	public class ResponseModel
	{
		public string Message { get; }
		public HttpStatusCode StatusCode { get; }
		public bool Success => StatusCode == HttpStatusCode.OK;

		public ResponseModel(HttpStatusCode statusCode, string message = null)
		{
			Message = message;
			StatusCode = statusCode;
		}
	}
}