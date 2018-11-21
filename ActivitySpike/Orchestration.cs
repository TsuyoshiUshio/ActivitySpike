using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
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
            var requestActivity = new Activity("ActivitySpike: HttpTrigger Request");
            // No parent from the start
            requestActivity.Start();
            var requestTelemetry = new RequestTelemetry {Name = "ActivitySpike: HttpTrigger Request"};
            requestTelemetry.SetActivity(requestActivity);

            requestTelemetry.Start();

            requestTelemetry.Stop();
            client.Track(requestTelemetry);

            var dependencyActivity = new Activity("HttpTrigger Dependency Queue output");
            dependencyActivity.SetParentId(requestActivity.Id);
            dependencyActivity.Start();
            var context = new Context()
            {
                ActivityId = dependencyActivity.Id,
                ParentId = dependencyActivity.ParentId,
            };
            var dependencyTelemetry = new DependencyTelemetry { Name = "ActivitySpike:: Enqueue" };
            dependencyTelemetry.SetActivity(dependencyActivity);
            dependencyTelemetry.Start();
            
           
            await contexts.AddAsync(context);
            dependencyActivity.Stop();

            requestActivity.Stop();

            dependencyTelemetry.Stop();
            client.Track(dependencyTelemetry);

            return new OkObjectResult($"Orchestration has been started.");

        }
        [FunctionName("Orchestrator")]
        public static async Task Orchestrator([QueueTrigger("control-queue-ai", Connection = "ConnectionString")]Context context,
            [Queue("work-item-queue-ai", Connection = "ConnectionString")] IAsyncCollector<Context> contexts,            
            ILogger log)
        {
            log.LogInformation($"Orchestration Started.");
            Activity requestActivity = null;

            if (context.OrchestrationActivity == null) // In case of the initial execution.
            {
                requestActivity = new Activity("Activity Spike: Orchestration Request");
                requestActivity.SetParentId(context.ActivityId);
            }
            else
            {
                requestActivity = new Activity("Activity Spike: Orchestration Request");
                // After Activity.SetParentId then Start the actvitiy, it will create a new Id. However, it is not Identical as the last execution. 
                // This is necessary. Or directly set from SubsetActivity. 
                var property = typeof(Activity).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
                property.SetValue(requestActivity, context.OrchestrationActivity.ActivityId);
                requestActivity.SetParentId(context.OrchestrationActivity.ParentId);
            }

            requestActivity.Start();
            var subsetActivity = new SubsetActivity()
            {
                ActivityId = requestActivity.Id,
                ParentId = requestActivity.ParentId,
                RootId = requestActivity.RootId
            };
            var requestTelemetry = new RequestTelemetry { Name = "Activity Spike: Ochestration Result" };

            if (context.OrchestrationActivity == null)
            {
                requestTelemetry.SetActivity(requestActivity);
            }
            else
            {
                requestTelemetry.Id = context.OrchestrationActivity.ActivityId;
                requestTelemetry.Context.Operation.Id = context.OrchestrationActivity.RootId;
                requestTelemetry.Context.Operation.ParentId = context.OrchestrationActivity.ParentId;
            }

            requestTelemetry.Start();
            // Only the last execution, we track it. 
            var dependencyActivity = new Activity("Activity Spike: Orchestration Dependency");
            dependencyActivity.SetParentId(requestActivity.Id);
            dependencyActivity.Start();

            var dependencyTelemetry = new DependencyTelemetry { Name = "Activity Spike: Orchestration Dependency" };
            dependencyTelemetry.SetActivity(dependencyActivity);

            dependencyTelemetry.Start();

            var c = new Context()
            {
                ActivityId = requestActivity.Id,
                ParentId = requestActivity.ParentId,
                OrchestrationActivity = subsetActivity
            };


            dependencyActivity.Stop();
            dependencyTelemetry.Stop();

            if (context.Completed)
            {
                client.Track(dependencyTelemetry);
            }
            else
            {
                await contexts.AddAsync(c);
                // We don't need to emit telemetry for intermediate execution.
            }

            requestActivity.Stop();

            requestTelemetry.Stop();
            if (context.OrchestrationActivity == null) // In case of the initial execution.
                client.Track(requestTelemetry);
        }

        [FunctionName("Activity")]
        public static async Task ActivityFunction([QueueTrigger("work-item-queue-ai", Connection = "ConnectionString")]Context context,
            [Queue("control-queue-ai", Connection = "ConnectionString")] IAsyncCollector<Context> contexts,
            ILogger log)
        {
            log.LogInformation($"Activity Functions Started.");
            var requestActivity = new Activity("Activity Spike: Activity Function Request");
            requestActivity.SetParentId(context.ActivityId);
            requestActivity.Start();

            var requestTelemetry = new RequestTelemetry { Name = "Activity Spike: Activity Function Request" };
            requestTelemetry.SetActivity(requestActivity);

            requestTelemetry.Start();

            var dependencyActivity = new Activity("Activity FUnction Dependency");
            dependencyActivity.SetParentId(requestActivity.Id);
            dependencyActivity.Start();

            var dependencyTelemetry = new DependencyTelemetry { Name = "Activity Spike: Activity Function Dependency" };
            dependencyTelemetry.SetActivity(dependencyActivity);
            dependencyTelemetry.Start();

            var c = new Context()
              {
                  ActivityId = dependencyActivity.Id,
                  ParentId = dependencyActivity.ParentId,
                  OrchestrationActivity = context.OrchestrationActivity , // I skip the code for stack for the activity.
                  Completed = true
              };
            await contexts.AddAsync(c);
            dependencyActivity.Stop();
            dependencyTelemetry.Stop();
            client.Track(dependencyTelemetry);
            requestActivity.Stop();
            requestTelemetry.Stop();
            client.Track(requestTelemetry);

        }


    }
    
}
