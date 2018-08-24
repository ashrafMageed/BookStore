using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Books.API.Controllers;
using Books.API.Controllers.Messaging;
using Books.API.ShippingService;
using Microsoft.AspNetCore.Authentication.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;

namespace Books.API
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
            var dataBaseRoot = new InMemoryDatabaseRoot();
            services.AddDbContext<ApiContext>(opt => opt.UseInMemoryDatabase("BooksDb"));
            services.AddDbContext<OrderContext>(opt => opt.UseInMemoryDatabase("OrdersDb"));
            services.AddDbContext<ShippingContext>(opt => opt.UseInMemoryDatabase("ShippingDb", dataBaseRoot));
            services.AddTransient<PurchaseOrderReceivedHandler>();
            var sp = services.BuildServiceProvider();
            var bus = new InMemoryMessageBus(); 

            var optionsBuilder = new DbContextOptionsBuilder<ShippingContext>();
            optionsBuilder.UseInMemoryDatabase("ShippingDb", dataBaseRoot);
            var context = new ShippingContext(optionsBuilder.Options);
            var shippingOrderHandler = new PurchaseOrderReceivedHandler(bus, context);
            bus.RegisterHandler<PurchaseOrderReceived>(e => shippingOrderHandler.Handle(e));
            
            services.AddSingleton<InMemoryMessageBus>(bus);

            // services.AddAuthentication(options =>
            //   {
            //     options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            //   })
            //     .AddJwtBearer(jwtOptions =>
            //     {
            //       jwtOptions.Authority = $"https://login.microsoftonline.com/tfp/{Configuration["AzureAdB2C:Tenant"]}/{Configuration["AzureAdB2C:Policy"]}/v2.0/";
            //       jwtOptions.Audience = Configuration["AzureAdB2C:ClientId"];
            //       jwtOptions.Events = new JwtBearerEvents
            //       {
            //         OnAuthenticationFailed = AuthenticationFailed
            //       };
            //     });

                services.AddAzureAdB2CAuthentication();

            services.AddMvc(setupAction => {
                
                var inputFormatter = setupAction.InputFormatters.OfType<JsonInputFormatter>().FirstOrDefault();

                if(inputFormatter != null)
                    inputFormatter.SupportedMediaTypes.Add("application/json-patch+json");
            });
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "Books Store", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            
            // // bus.RegisterHandler<PurchaseOrderReceived>(e => shippingOrderHandler.Handle(e));
            // var bus = app.ApplicationServices.GetService<InMemoryMessageBus>();
            // var handler = new PurchaseOrderReceivedHandler(bus, app.ApplicationServices.GetService<ShippingContext>());
            // bus.RegisterHandler<PurchaseOrderReceived>(e => handler.Handle(e));

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Book Store");
                c.RoutePrefix = String.Empty;
            });

            app.UseAuthentication();

                        app.UseRewriter(new RewriteOptions().AddIISUrlRewrite(env.ContentRootFileProvider, "urlRewrite.config"));

            app.UseMvc();
        }

        private Task AuthenticationFailed(AuthenticationFailedContext arg)
        {
            // For debugging purposes only!
            var s = $"AuthenticationFailed: {arg.Exception.Message}";
            arg.Response.ContentLength = s.Length;
            arg.Response.Body.Write(Encoding.UTF8.GetBytes(s), 0, s.Length);
            return Task.FromResult(0);
        }
    }
}
