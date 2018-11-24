using System;
using System.Collections.Generic;
using System.Text;

namespace ActivityW3CSpike
{
    public class TraceContext
    {

        public string Tracestate { get; set; }

        public string Traceparent { get; set; }

        // This is only used for recover the state.
        public string ParentSpanId { get; set; }

        public bool Completed { get; set; }

        public Stack<TraceContext> OrchestrationContexts { get; set; }
        public TraceContext()
        {
            OrchestrationContexts = new Stack<TraceContext>();
        }

    }
}
