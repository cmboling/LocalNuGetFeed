using System;
using System.Net;
using Newtonsoft.Json;

namespace LocalNugetFeed.Core.Models
{
	public class ResponseModel<T> : ResponseModel
	{
		public T Data { get; }

		public ResponseModel(HttpStatusCode statusCode, string message = null) : base(statusCode, message)
		{
		}

		public ResponseModel(T data) : base(HttpStatusCode.OK)
		{
			Data = data;
		}
	}

	[Serializable]
	public class ResponseModel
	{
		public string Message { get; protected set; }
		public HttpStatusCode StatusCode { get; protected set; }
		public bool Success => StatusCode == HttpStatusCode.OK;

		public ResponseModel(HttpStatusCode statusCode, string message = null)
		{
			Message = message;
			StatusCode = statusCode;
		}
	}
}