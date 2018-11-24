using Microsoft.ApplicationInsights.DataContracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.ApplicationInsights.W3C;


#pragma warning disable 618

namespace ActivityW3CSpike
{
    public static class RequestTelemetryExtensions
    {
        public static void SetActivity(this RequestTelemetry telemetry, Activity activity)
        {
            telemetry.Id = $"|{activity.GetTraceId()}.{activity.GetSpanId()}";
            telemetry.Context.Operation.Id = activity.GetTraceId();
            telemetry.Context.Operation.ParentId = $"|{activity.GetTraceId()}.{activity.GetParentSpanId()}";
        }

    }

    public static class DependencyTelemetryExtensions
    {
        public static void SetActivity(this DependencyTelemetry telemetry, Activity activity)
        {
            telemetry.Id = $"|{activity.GetTraceId()}.{activity.GetSpanId()}";
            telemetry.Context.Operation.Id = activity.GetTraceId();
            telemetry.Context.Operation.ParentId = $"|{activity.GetTraceId()}.{activity.GetParentSpanId()}";
        }
    }

    public static class ActivityExtensions
    {
        public static TraceContext CreateTraceContext(this Activity activity)
        {
            var context = new TraceContext()
            {
                Traceparent = activity.GetTraceparent(),
                Tracestate = activity.GetTracestate(),
                ParentSpanId = activity.GetParentSpanId()
            };
            return context;
        }


        public static Activity SetParentAndStart(this Activity activity, TraceContext context)
        {
            activity.SetTraceparent(context.Traceparent);
            activity.SetTracestate(context.Tracestate);
            activity.Start();
            return activity;
        }

        public static Activity SetParentAndStart(this Activity activity, Activity parent)
        {
            activity.SetTraceparent(parent.GetTraceparent());
            activity.SetTracestate(parent.GetTracestate());
            activity.Start();
            return activity;
        }

        private static void CallPrivateMethodWithValue(Activity activity, string methodName, string value)
        {
            var method = typeof(W3CActivityExtensions).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(activity, new object[] {activity, value});
        }
    }
}
