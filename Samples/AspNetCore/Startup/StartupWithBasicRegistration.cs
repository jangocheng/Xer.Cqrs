using System;
using System.IO;
using Domain.Commands;
using Domain.DomainEvents;
using Domain.Repositories;
using Infrastructure.DomainEventHandlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReadSide.Products.Queries;
using ReadSide.Products.Repositories;
using Swashbuckle.AspNetCore.Swagger;
using Xer.Cqrs.CommandStack;
using Xer.Cqrs.EventStack;
using Xer.Cqrs.QueryStack;
using Xer.Cqrs.QueryStack.Dispatchers;
using Xer.Cqrs.QueryStack.Registrations;
using Xer.Delegator.Registrations;

namespace AspNetCore
{
    class StartupWithBasicRegistration
    {
        private static readonly string AspNetCoreAppXmlDocPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                                                                    $"{typeof(StartupWithBasicRegistration).Assembly.GetName().Name}.xml");
                                                                    
        public StartupWithBasicRegistration(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {            
            // Swagger.
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "AspNetCore Basic Registration Sample", Version = "v1" });
                c.IncludeXmlComments(AspNetCoreAppXmlDocPath);
            });

            // Write-side repository.
            services.AddSingleton<IProductRepository>((serviceProvider) => 
                new PublishingProductRepository(new InMemoryProductRepository(), serviceProvider.GetRequiredService<EventDelegator>())
            );

            // Read-side repository.
            services.AddSingleton<IProductReadSideRepository, InMemoryProductReadSideRepository>();

            // Register command delegator.
            services.AddSingleton<CommandDelegator>(serviceProvider => 
            {
                 // Register command handlers.
                var commandHandlerRegistration = new SingleMessageHandlerRegistration();
                commandHandlerRegistration.RegisterCommandHandler(() => new RegisterProductCommandHandler(serviceProvider.GetRequiredService<IProductRepository>()));
                commandHandlerRegistration.RegisterCommandHandler(() => new ActivateProductCommandHandler(serviceProvider.GetRequiredService<IProductRepository>()));
                commandHandlerRegistration.RegisterCommandHandler(() => new DeactivateProductCommandHandler(serviceProvider.GetRequiredService<IProductRepository>()));

                return new CommandDelegator(commandHandlerRegistration.BuildMessageHandlerResolver());
            });

            // Register event delegator.
            services.AddSingleton<EventDelegator>((serviceProvider) =>
            {
                // Register event handlers.
                var eventHandlerRegistration = new MultiMessageHandlerRegistration();
                eventHandlerRegistration.RegisterEventHandler<ProductRegisteredEvent>(() => new ProductDomainEventsHandler(serviceProvider.GetRequiredService<IProductReadSideRepository>()));
                eventHandlerRegistration.RegisterEventHandler<ProductActivatedEvent>(() => new ProductDomainEventsHandler(serviceProvider.GetRequiredService<IProductReadSideRepository>()));
                eventHandlerRegistration.RegisterEventHandler<ProductDeactivatedEvent>(() => new ProductDomainEventsHandler(serviceProvider.GetRequiredService<IProductReadSideRepository>()));

                return new EventDelegator(eventHandlerRegistration.BuildMessageHandlerResolver());
            });

            // Register query dispatcher.
            services.AddSingleton<IQueryAsyncDispatcher>(serviceProvider =>
            {
                // Register query handlers.
                var registration = new QueryHandlerRegistration();
                registration.Register(() => new QueryAllProductsHandler(serviceProvider.GetRequiredService<IProductReadSideRepository>()));
                registration.Register(() => new QueryProductByIdHandler(serviceProvider.GetRequiredService<IProductReadSideRepository>()));

                return new QueryDispatcher(registration);
            });

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "AspNetCore Basic Registration Sample V1");
            });

            app.UseMvc();
        }
    }
}
