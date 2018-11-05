using System;
using System.IO;
using LocalNugetFeed.Controllers;
using LocalNugetFeed.Core.Common;
using LocalNugetFeed.Core.ConfigurationOptions;
using LocalNugetFeed.Core.Interfaces;
using LocalNugetFeed.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SpaServices.AngularCli;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LocalNugetFeed
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddDistributedMemoryCache();
			services.AddHttpContextAccessor();
			services.AddSession(options =>
			{
				options.Cookie.Name = Constants.PackagesSessionCookieKey;
				options.IdleTimeout = TimeSpan.FromMinutes(5);
				options.Cookie.HttpOnly = true;
			});
			
			services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
			
			services.AddSpaStaticFiles(c =>
			{
				c.RootPath = "wwwroot";
			});
			services.AddTransient<IPackageFileStorageService, PackageFileStorageService>();
			services.AddTransient<IPackageService, PackageService>();
			services.AddSingleton<IPackageSessionService, PackageSessionService>();
			
			InitPackageFileStorage(services);
		}

		/// <summary>
		/// init file storage options
		/// </summary>
		private void InitPackageFileStorage(IServiceCollection services)
		{
			var fileStorageSection = Configuration.GetSection(Constants.PackagesFileStorage);

			services.Configure<PackagesFileStorageOptions>(fileStorageSection);
			services.AddScoped(sp =>
			{
				var options = sp
					.GetService<IOptionsSnapshot<PackagesFileStorageOptions>>()
					.Value;

				try
				{
					options.Path = PackagesFileHelper.GetPackagesFolderPath(options.Path);
				}
				catch (Exception)
				{
					options.Path = PackagesFileHelper.GetDefaultPackagesFolderFullPath();
				}

				return options;
			});
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseHsts();
				app.UseHttpsRedirection();
			}
			
			// todo: register api key for nuget push operation
			
			app.UseSession();
			app.UseDefaultFiles();
			app.UseStaticFiles();
			app.UseSpaStaticFiles();
			app.UseStatusCodePages();
			app.UseMvc(routes =>
			{
				
				// Service index
				routes.MapRoute("index", "v3/index.json", defaults: new { controller = "Index", action = nameof(IndexController.Get)});

				// Package Publish
				routes.MapRoute(
					Constants.NuGetPushActionName,
					Constants.NuGetPushRelativeUrl,
					defaults: new { controller = "Package", action = nameof(PackageController.Push) });
			});

			app.UseSpa(spa =>
			{
				// refs https://docs.microsoft.com/en-us/aspnet/core/client-side/spa/angular?view=aspnetcore-2.1
				spa.Options.SourcePath = "ClientApp";
 
				if (env.IsDevelopment())
				{
					spa.UseAngularCliServer(npmScript: "start");
				}
			});
		}
	}
}
