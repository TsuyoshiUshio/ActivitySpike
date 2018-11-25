using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics;
using Microsoft.ApplicationInsights.DataContracts;

namespace CorrelationBasic
{
    public static class Function1
    {

        private static TelemetryClient telemetryClient;

        static Function1()
        {
            DependencyTrackingTelemetryModule module = new DependencyTrackingTelemetryModule();
            module.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("core.windows.net");
            module.Initialize(TelemetryConfiguration.Active);

            var config = new TelemetryConfiguration();
            // Set the instrumentKey
            config.InstrumentationKey = Environment.GetEnvironmentVariable("InstrumentKey");

            telemetryClient = new TelemetryClient(config);

        }

        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [Queue("sample-items", Connection = "ConnectionString")]
            IAsyncCollector<string> parentIds,
            ILogger log)
        {
            try
            {
                var current = Activity.Current;
                log.LogInformation("C# HTTP trigger function processed a request.");
                var requestActivity = new Activity("Sample: Function 1 HttpRequest");
                requestActivity.Start(); // You can omit this code.

                var requestOperation = telemetryClient.StartOperation<RequestTelemetry>(requestActivity);

                telemetryClient.StopOperation(requestOperation);
         
                var dependencyActivity = new Activity("Sample: Function 1 Enqueue");
                dependencyActivity.SetParentId(requestActivity.Id); // You can comit this code. 
                dependencyActivity.Start(); // You can omit this code. 
                var dependencyOperation = telemetryClient.StartOperation<DependencyTelemetry>(dependencyActivity);
                await parentIds.AddAsync(dependencyActivity.Id);

                telemetryClient.StopOperation(dependencyOperation);

                return (ActionResult) new OkObjectResult($"Done");
            }
            catch (Exception e)
            {
                telemetryClient.TrackException(e);
                throw e;
            }
        }

        [FunctionName("Function2")]
        public static void Queue([QueueTrigger("sample-items", Connection = "ConnectionString")]string parentId, ILogger log)
        {
            var requestActivity = new Activity("Sample: Function 2 Queue Request");
            requestActivity.SetParentId(parentId);
            requestActivity.Start(); // You can omit this code.

            var requestOperation = telemetryClient.StartOperation<RequestTelemetry>(requestActivity);

            telemetryClient.StopOperation(requestOperation);
            log.LogInformation($"C# Queue trigger function processed: {parentId}");
        }
    }
}
