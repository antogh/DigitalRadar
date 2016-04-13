using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Fabric;

using Common;
using System.Text;

namespace Web1 {
   public class Startup {
      public Startup(IHostingEnvironment env) {
         // Set up configuration sources.
         var builder = new ConfigurationBuilder()
             .AddJsonFile("appsettings.json")
             .AddEnvironmentVariables();
         Configuration = builder.Build();
      }

      public IConfigurationRoot Configuration { get; set; }

      // This method gets called by the runtime. Use this method to add services to the container.
      public void ConfigureServices(IServiceCollection services) {
         // Add framework services.
         services.AddMvc();
         services.AddSingleton<ConcurrentBag<WebSocket>, ConcurrentBag<WebSocket>>();
      }

      // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
      public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory) {
         loggerFactory.AddConsole(Configuration.GetSection("Logging"));
         loggerFactory.AddDebug();
         app.UseIISPlatformHandler();
         app.UseStaticFiles(new StaticFileOptions { ServeUnknownFileTypes = true });
         app.UseWebSockets();
         app.UseMiddleware<MyWebSocketMiddleware>();
         app.UseMvc();
      }

      // Entry point for the application.
      public static void Main(string[] args) => WebApplication.Run<Startup>(args);
   }

   /*
   public class WebSocketCollection  {
      public ConcurrentBag<WebSocket> _collection;

      public WebSocketCollection() {
         _collection = new ConcurrentBag<WebSocket>();
      }
   }
   */

   public class MyWebSocketMiddleware {
      private readonly RequestDelegate _next;
      private ConcurrentBag<WebSocket> _ws_collection;

      public MyWebSocketMiddleware(RequestDelegate next, ConcurrentBag<WebSocket> ws_collection) {
         _next = next;
         _ws_collection = ws_collection;
      }

      public async Task Invoke(Microsoft.AspNet.Http.HttpContext context) {
         if (context.WebSockets.IsWebSocketRequest) {
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            if ((webSocket != null) && (webSocket.State == WebSocketState.Open)) {
               _ws_collection.Add(webSocket);
               while (webSocket.State == WebSocketState.Open) {
                  Thread.Sleep(100);
                  /*
                  var token = CancellationToken.None;
                  var type = WebSocketMessageType.Text;
                  var data = Encoding.UTF8.GetBytes("prova 0 32 localhost");
                  var buffer = new ArraySegment<Byte>(data);
                  await webSocket.SendAsync(buffer, type, true, token);
                  */
               }
            }
         }
         else {
            // Nothing to do here, pass downstream.  
            await _next.Invoke(context);
         }
      }


   }


}





















