using System;
using System.Net;

namespace LocalNugetFeed.Core.BLL.DTO
{
	public class ResponseDTO<T> : ResponseDTO
	{
		public T Data { get; }

		public ResponseDTO(HttpStatusCode statusCode, string message = null) : base(statusCode, message)
		{
		}

		public ResponseDTO(T data) : base(HttpStatusCode.OK)
		{
			Data = data;
		}
	}

	[Serializable]
	public class ResponseDTO
	{
		public string Message { get; protected set; }
		public HttpStatusCode StatusCode { get; protected set; }
		public bool Success => StatusCode == HttpStatusCode.OK;

		public ResponseDTO(HttpStatusCode statusCode, string message = null)
		{
			Message = message;
			StatusCode = statusCode;
		}
	}
}