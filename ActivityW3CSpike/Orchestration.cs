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
using Microsoft.ApplicationInsights.W3C;

#pragma warning disable 618

namespace ActivityW3CSpike
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
            [Queue("control-queue-ai", Connection = "ConnectionString")] IAsyncCollector<TraceContext> contexts,
            ILogger log)
        {
            log.LogInformation("Accept the request. Incoming request doesn't have correlation info.");
            var requestActivity = new Activity("W3CActivitySpike: HttpTrigger Request");
            requestActivity.GenerateW3CContext();
            // No parent from the start
            requestActivity.Start();
            var requestTelemetry = new RequestTelemetry { Name = "W3CActivitySpike: HttpTrigger Request" };
            requestTelemetry.SetActivity(requestActivity);

            requestTelemetry.Start();

            requestTelemetry.Stop();
            client.Track(requestTelemetry);

            var dependencyActivity = new Activity("HttpTrigger Dependency Queue output");
            dependencyActivity.SetTraceparent(requestActivity.GetTraceparent());
            dependencyActivity.SetTracestate(requestActivity.GetTracestate());
            // dependencyActivity.SetParentId(requestActivity.Id); // maybe not necessary
            dependencyActivity.Start();
            var context = dependencyActivity.CreateTraceContext();

            var dependencyTelemetry = new DependencyTelemetry { Name = "W3CActivitySpike:: Enqueue" };
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
        public static async Task Orchestrator([QueueTrigger("control-queue-ai", Connection = "ConnectionString")]
            TraceContext traceContext,
            [Queue("work-item-queue-ai", Connection = "ConnectionString")]
            IAsyncCollector<TraceContext> contexts,
            ILogger log)
        {
            log.LogInformation($"Orchestration Started.");
            Activity requestActivity = null;
            var isReplay = traceContext.OrchestrationContexts.Count != 0;

            if (!isReplay)
            {

            requestActivity = new Activity("W3CActivity Spike: Orchestration Request");
            requestActivity.SetParentAndStart(traceContext);

            var requestTelemetry = new RequestTelemetry {Name = "W3CActivity Spike: Orchestration Request"};
            requestTelemetry.SetActivity(requestActivity);

            requestTelemetry.Start();

             requestActivity.Stop();

             requestTelemetry.Stop();
             client.Track(requestTelemetry);
            }

        TraceContext c = null;

            if (!isReplay)
            {
                c = requestActivity.CreateTraceContext();
                c.OrchestrationContexts = traceContext.OrchestrationContexts;
                c.OrchestrationContexts.Push(requestActivity.CreateTraceContext());
            }
            else
            {
                c = traceContext.OrchestrationContexts.Peek(); // if necessary. This program doesn't need it.
            }



            if (traceContext.Completed)
            {
                var dependencyActivity = new Activity("W3CActivity Spike: Orchestration Dependency");
                dependencyActivity.SetParentAndStart(traceContext.OrchestrationContexts.Peek());
                var dependencyTelemetry = new DependencyTelemetry { Name = "W3CActivity Spike: Orchestration Dependency" };
                dependencyTelemetry.SetActivity(dependencyActivity);

                dependencyTelemetry.Start();
                dependencyActivity.Stop();
                dependencyTelemetry.Stop();

                client.Track(dependencyTelemetry);
            }
            else
            {
                await contexts.AddAsync(c);
                // We don't need to emit telemetry for intermediate execution.
            }

        }

        [FunctionName("Activity")]
        public static async Task ActivityFunction([QueueTrigger("work-item-queue-ai", Connection = "ConnectionString")]TraceContext traceContext,
            [Queue("control-queue-ai", Connection = "ConnectionString")] IAsyncCollector<TraceContext> contexts,
            ILogger log)
        {
            log.LogInformation($"W3CActivity Functions Started.");
            var requestActivity = new Activity("W3CActivity Spike: Activity Function Request");
            requestActivity.SetParentAndStart(traceContext);

            var requestTelemetry = new RequestTelemetry { Name = "W3CActivity Spike: Activity Function Request" };
            requestTelemetry.SetActivity(requestActivity);

            requestTelemetry.Start();

            var dependencyActivity = new Activity("W3CActivity FUnction Dependency");
            dependencyActivity.SetParentAndStart(requestActivity);

            var dependencyTelemetry = new DependencyTelemetry { Name = "W3CActivity Spike: Activity Function Dependency" };
            dependencyTelemetry.SetActivity(dependencyActivity);
            dependencyTelemetry.Start();
            var c = dependencyActivity.CreateTraceContext();
            c.OrchestrationContexts = traceContext.OrchestrationContexts;
            c.Completed = true;
            
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
