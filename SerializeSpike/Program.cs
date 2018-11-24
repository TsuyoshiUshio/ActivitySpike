using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SerializeSpike
{

    class Context
    {
        public Stack<string> OrchestrationState { get; set; }

        public Context()
        {
            OrchestrationState = new Stack<string>();
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            var context = new Context();
            // context.OrchestrationState = new Stack<string>();

            // context.OrchestrationState.Push("foo");
           // context.OrchestrationState.Push("bar");

            var json = JsonConvert.SerializeObject(context);
            var context2 = JsonConvert.DeserializeObject<Context>(json);
            // Console.WriteLine($"Element 1: {context2.OrchestrationState.Pop()} Element 2: {context2.OrchestrationState.Pop()}");
            Console.ReadLine();
        }
    }
}
