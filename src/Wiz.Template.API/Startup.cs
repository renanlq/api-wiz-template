﻿using AutoMapper;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using NSwag;
using NSwag.SwaggerGeneration.Processors.Security;
using Polly;
using System;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using Wiz.Template.API.Extensions;
using Wiz.Template.API.Filters;
using Wiz.Template.API.Handler;
using Wiz.Template.API.Middlewares;
using Wiz.Template.API.Services;
using Wiz.Template.API.Services.Interfaces;
using Wiz.Template.API.Settings;
using Wiz.Template.API.Swagger;
using Wiz.Template.Domain.Interfaces.Notifications;
using Wiz.Template.Domain.Interfaces.Repository;
using Wiz.Template.Domain.Interfaces.Services;
using Wiz.Template.Domain.Interfaces.UoW;
using Wiz.Template.Domain.Notifications;
using Wiz.Template.Infra.Context;
using Wiz.Template.Infra.Repository;
using Wiz.Template.Infra.Services;
using Wiz.Template.Infra.UoW;

[assembly: ApiConventionType(typeof(MyApiConventions))]
namespace Wiz.Template.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            Configuration = configuration;
            HostingEnvironment = hostingEnvironment;
        }

        public IConfiguration Configuration { get; }
        public IHostingEnvironment HostingEnvironment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(options =>
            {
                options.Filters.Add<DomainNotificationFilter>();
                options.EnableEndpointRouting = false;
            }).SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
            .AddJsonOptions(options =>
             {
                 options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
             });
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = TokenAuthenticationOptions.Bearer;
            }).AddBearerToken(null);
            services.AddApiVersioning(options =>
            {
                options.ApiVersionReader = new HeaderApiVersionReader("api-version");
            });
            services.Configure<GzipCompressionProviderOptions>(x => x.Level = CompressionLevel.Optimal);
            services.AddResponseCompression(x =>
            {
                x.Providers.Add<GzipCompressionProvider>();
            });

            services.AddHttpClient<IViaCEPService, ViaCEPService>((s, c) =>
            {
                c.BaseAddress = new Uri(Configuration["API:ViaCEP"]);
                c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }).AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.OrResult(response =>
                   (int)response.StatusCode != (int)HttpStatusCode.OK)
              .WaitAndRetryAsync(3, retry =>
                   TimeSpan.FromSeconds(Math.Pow(2, retry)) +
                   TimeSpan.FromMilliseconds(new Random().Next(0, 100))))
              .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.CircuitBreakerAsync(
                   handledEventsAllowedBeforeBreaking: 3,
                   durationOfBreak: TimeSpan.FromSeconds(30)
            ));

            if (PlatformServices.Default.Application.ApplicationName != "testhost")
            {
                services.AddHealthChecksUI()
                    .AddHealthChecks()
                    .AddSqlServer(Configuration["ConnectionStrings:CustomerDB"])
                    .AddApplicationInsightsPublisher();
            }

            if (!HostingEnvironment.IsProduction())
            {
                services.AddOpenApiDocument(document =>
                {
                    document.DocumentName = "v1";
                    document.Version = "v1";
                    document.Title = "Template API";
                    document.Description = "API de Template";
                    document.OperationProcessors.Add(new OperationSecurityScopeProcessor("JWT"));
                    document.DocumentProcessors.Add(new SecurityDefinitionAppender("JWT", new SwaggerSecurityScheme
                    {
                        Type = SwaggerSecuritySchemeType.ApiKey,
                        Name = HeaderNames.Authorization,
                        Description = "Token de autenticação via SSO",
                        In = SwaggerSecurityApiKeyLocation.Header
                    }));
                });
            }

            services.AddAutoMapper(typeof(Startup));
            services.AddHttpContextAccessor();

            RegisterServices(services);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IOptions<ApplicationInsightsSettings> options)
        {
            if (!env.IsProduction())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseResponseCompression();

            if (PlatformServices.Default.Application.ApplicationName != "testhost")
            {
                app.UseHealthChecks("/health", new HealthCheckOptions()
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
                app.UseHealthChecksUI(setup =>
                {
                    setup.UIPath = "/health-ui";
                });
            }

            if (!env.IsProduction())
            {
                app.UseSwagger();
                app.UseSwaggerUi3();
            }

            app.UseExceptionHandler(new ExceptionHandlerOptions
            {
                ExceptionHandler = new ErrorHandlerMiddleware(options, env).Invoke
            });

            app.UseMvc();
        }

        private void RegisterServices(IServiceCollection services)
        {
            services.Configure<ApplicationInsightsSettings>(Configuration.GetSection("ApplicationInsights"));

            #region Service

            services.AddTransient<ICustomerService, CustomerService>();

            #endregion

            #region Domain

            services.AddScoped<IDomainNotification, DomainNotification>();

            #endregion

            #region Infra

            services.AddDbContext<EntityContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("CustomerDB")));
            services.AddScoped<DapperContext>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddTransient<ICustomerRepository, CustomerRepository>();

            #endregion
        }
    }
}
