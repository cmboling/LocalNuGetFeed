using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;
using LocalNugetFeed.Controllers;
using LocalNugetFeed.Core.ConfigurationOptions;
using LocalNugetFeed.Core.Interfaces;
using LocalNugetFeed.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
			services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
			
			services.AddTransient<IPackageFileStorageService, PackageFileStorageService>();
			services.AddTransient<IPackageService, PackageService>();
			services.AddTransient<IPackageDatabaseService, PackageDatabaseService>();
			
			InitPackageFileStorage(services);
		}

		/// <summary>
		/// init file storage options
		/// </summary>
		private void InitPackageFileStorage(IServiceCollection services)
		{
			var fileStorageSection = Configuration.GetSection("PackagesFileStorage");

			services.Configure<PackagesFileStorageOptions>(fileStorageSection);
			services.AddScoped(sp =>
			{
				var options = sp
					.GetService<IOptionsSnapshot<PackagesFileStorageOptions>>()
					.Value;

				options.Path = string.IsNullOrEmpty(options.Path)
					? Path.Combine(Directory.GetCurrentDirectory(), "Packages")
					: options.Path;

				Directory.CreateDirectory(options.Path);

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
			}
			
			app.UseStatusCodePages();
			app.UseHttpsRedirection();
			app.UseMvc(routes =>
			{
				// Service index
				routes.MapRoute("index", "v3/index.json", defaults: new { controller = "Index", action = nameof(IndexController.Get)});

				// Package Publish
				routes.MapRoute(
					"upload",
					"v2/package",
					defaults: new { controller = "Package", action = nameof(PackageController.Push) });
			});

		}
	}
}
