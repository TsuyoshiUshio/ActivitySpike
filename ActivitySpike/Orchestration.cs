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
            requestTelemetry.Id = requestActivity.Id;
            requestTelemetry.Context.Operation.Id = requestActivity.RootId;
            requestTelemetry.Context.Operation.ParentId = requestActivity.ParentId;

            requestTelemetry.Start();

            requestTelemetry.Stop();
            client.Track(requestTelemetry);
            var subsetActivity = new SubsetActivity()
            {
                ActivityId = requestActivity.Id,
                ParentId =  requestActivity.ParentId,
                RootId = requestActivity.RootId
            };

            var dependencyActivity = new Activity("HttpTrigger Dependency Queue output");
            dependencyActivity.SetParentId(requestActivity.Id);
            dependencyActivity.Start();
            var list = new List<SubsetActivity>();
            list.Add(subsetActivity);
            var context = new Context()
            {
                ActivityId = dependencyActivity.Id,
                ParentId = dependencyActivity.ParentId,
                Stack = list,

            };
            var dependencyTelemetry = new DependencyTelemetry { Name = "ActivitySpike:: Enqueue" };
            dependencyTelemetry.Id = dependencyActivity.Id;
            dependencyTelemetry.Context.Operation.Id = dependencyActivity.RootId;
            dependencyTelemetry.Context.Operation.ParentId = dependencyActivity.ParentId;
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
            var count = context.Stack.Count;
            if (count == 1) // In case of the initial execution.
            {
                requestActivity = new Activity("Activity Spike: Orchestration Request");
                requestActivity.SetParentId(context.ActivityId);
            }
            else
            {
                requestActivity = new Activity("Activity Spike: Orchestration Request");
                var current = context.Stack.LastOrDefault();
                var property = typeof(Activity).GetProperty("Id", BindingFlags.Public);
                property.SetValue(requestActivity, current.ActivityId);
                requestActivity.SetParentId(current.ParentId);
            }

            requestActivity.Start();
            var subsetActivity = new SubsetActivity()
            {
                ActivityId = requestActivity.Id,
                ParentId = requestActivity.ParentId,
                RootId = requestActivity.RootId
            };
            var requestTelemetry = new RequestTelemetry { Name = "Activity Spike: Ochestration Result" };
            requestTelemetry.Id = requestActivity.Id;
            requestTelemetry.Context.Operation.Id = requestActivity.RootId;
            requestTelemetry.Context.Operation.ParentId = requestActivity.ParentId;

            requestTelemetry.Start();

            if (context.Completed)
            {
                // Finish. Do nothing. 
            }
            else
            {
                var dependencyActivity = new Activity("Activity Spike: Orchestration Dependency");
                dependencyActivity.SetParentId(requestActivity.Id);
                dependencyActivity.Start();

                var dependencyTelemetry = new DependencyTelemetry { Name = "Activity Spike: Orchestration Dependency" };
                dependencyTelemetry.Id = dependencyActivity.Id;
                dependencyTelemetry.Context.Operation.Id = dependencyActivity.RootId;
                dependencyTelemetry.Context.Operation.ParentId = dependencyActivity.ParentId;

                dependencyTelemetry.Start();

                var c = new Context()
                {
                    ActivityId = dependencyActivity.Id,
                    ParentId = dependencyActivity.ParentId,
                    Stack = new List<SubsetActivity>()
                };
                if (count == 1)
                {
                    c.Stack.Add(subsetActivity);
                }
                else
                {
                    c.Stack = context.Stack;
                }

                await contexts.AddAsync(c);
                dependencyActivity.Stop();
                dependencyTelemetry.Stop();
                client.Track(dependencyTelemetry);

            }

            requestActivity.Stop();

            requestTelemetry.Stop();
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
            requestTelemetry.Id = requestActivity.Id;
            requestTelemetry.Context.Operation.Id = requestActivity.RootId;
            requestTelemetry.Context.Operation.ParentId = requestActivity.ParentId;

            requestTelemetry.Start();

            var dependencyActivity = new Activity("Activity FUnction Dependency");
            dependencyActivity.SetParentId(requestActivity.Id);
            dependencyActivity.Start();

            var dependencyTelemetry = new DependencyTelemetry { Name = "Activity Spike: Activity Function Dependency" };
            dependencyTelemetry.Id = dependencyActivity.Id;
            dependencyTelemetry.Context.Operation.Id = dependencyActivity.RootId;
            dependencyTelemetry.Context.Operation.ParentId = dependencyActivity.ParentId;
            dependencyTelemetry.Start();

            var c = new Context()
              {
                  ActivityId = dependencyActivity.Id,
                  ParentId = dependencyActivity.ParentId,
                  Stack = context.Stack , // I skip the code for stack for the activity.
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
