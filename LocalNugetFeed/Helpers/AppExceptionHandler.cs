using System;
using System.Net;
using System.Threading.Tasks;
using LocalNugetFeed.Core.BLL.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace LocalNugetFeed.Web.Helpers
{
	public class AppExceptionHandler
	{
		private readonly RequestDelegate _next;
		private readonly ILogger _logger;

		public AppExceptionHandler(RequestDelegate next, ILogger<AppExceptionHandler> logger)
		{
			_logger = logger;
			_next = next;
		}

		public async Task Invoke(HttpContext httpContext)
		{
			try
			{
				await _next(httpContext);
			}
			catch (Exception exception)
			{
				_logger.LogError($"Application error: {exception}");
				
				httpContext.Response.ContentType = "application/json";
				httpContext.Response.StatusCode = (int) HttpStatusCode.InternalServerError;

				await httpContext.Response.WriteAsync(JsonConvert.SerializeObject(new ResponseDTO(
					HttpStatusCode.InternalServerError,
					exception.Message ?? "An error has occured during request. Please try again later."
				)));
			}
		}
	}
}