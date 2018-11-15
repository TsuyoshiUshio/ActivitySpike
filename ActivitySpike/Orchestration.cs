using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.DependencyCollector;

namespace ActivitySpike
{
    public static class Orchestration
    {
        private static TelemetryClient client;

        static Orchestration()
        {
            DependencyTrackingTelemetryModule module = new DependencyTrackingTelemetryModule();
            module.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("core.windows.net");
            module.Initialize(TelemetryConfiguration.Active);

            var config = new TelemetryConfiguration();
            // Set the instrumentKey
            config.InstrumentationKey = Environment.GetEnvironmentVariable("InstrumentKey");

            client = new TelemetryClient(config);

        }


        [FunctionName("HttpRequest")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            [Queue("control-queue-ai", Connection = "ConnectionString")] IAsyncCollector<Context> contexts,
            ILogger log)
        {
            log.LogInformation("Accept the request. Incoming request doesn't have correlation info.");
            var requestActivity = new Activity("HttpTrigger Request");
            // No parent from the start
            requestActivity.Start();

            var dependencyActivity = new Activity("HttpTrigger Dependency Queue output");
            dependencyActivity.SetParentId(requestActivity.Id);
            dependencyActivity.Start();
            var context = new Context()
            {
                ActivityId = dependencyActivity.Id,
                ParentId = dependencyActivity.ParentId
            };
            await contexts.AddAsync(context);
            dependencyActivity.Stop();

            requestActivity.Stop();

            return new OkObjectResult($"Orchestration has been started.");

        }
        [FunctionName("Orchestrator")]
        public static async Task Orchestrator([QueueTrigger("control-queue-ai", Connection = "ConnectionString")]Context context,
            [Queue("work-item-queue-ai", Connection = "ConnectionString")] IAsyncCollector<Context> contexts,            
            ILogger log)
        {
            log.LogInformation($"Orchestration Started.");
            var requestActivity = new Activity("Orchestration Request");
            requestActivity.SetParentId(context.ActivityId);
            requestActivity.Start();

            if (context.Completed)
            {
                // Finish. Do nothing. 
            }
            else
            {
                var dependencyActivity = new Activity("Orchestration Denepency");
                dependencyActivity.SetParentId(requestActivity.Id);
                dependencyActivity.Start();
                var c = new Context()
                {
                    ActivityId = dependencyActivity.Id,
                    ParentId = dependencyActivity.ParentId
                };
                await contexts.AddAsync(c);
                dependencyActivity.Stop();
  
            }

            requestActivity.Stop();
        }

        [FunctionName("Activity")]
        public static async Task ActivityFunction([QueueTrigger("work-item-queue-ai", Connection = "ConnectionString")]Context context,
            [Queue("control-queue-ai", Connection = "ConnectionString")] IAsyncCollector<Context> contexts,
            ILogger log)
        {
            log.LogInformation($"Activity Functions Started.");
            var requestActivity = new Activity("Activity Function Request");
            requestActivity.SetParentId(context.ActivityId);
            requestActivity.Start();

            var dependencyActivity = new Activity("Activity FUnction Dependency");
            dependencyActivity.SetParentId(requestActivity.Id);
            dependencyActivity.Start();
            var c = new Context()
              {
                  ActivityId = dependencyActivity.Id,
                  ParentId = dependencyActivity.ParentId,
                  Completed = true
              };
            await contexts.AddAsync(c);
            dependencyActivity.Stop();
            requestActivity.Stop();

        }

    }
    
}
