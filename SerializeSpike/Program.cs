using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SerializeSpike
{

    class Context
    {
        public Stack<Context> OrchestrationState { get; set; }
        public string Name { get; set; }
        public Context()
        {
            OrchestrationState = new Stack<Context>();
        }
    }

    class TraceContext
    {
        public Stack<string> OrchestrationTraceContexts { get; set; }
        public string Name { get; set; }

        private static JsonSerializerSettings DefaultSerializerSettings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Objects
        };


        public TraceContext()
        {
            OrchestrationTraceContexts = new Stack<string>();
        }



        public void OrchestrationTraceContextPush(TraceContext context)
        {
            OrchestrationTraceContexts.Push(JsonConvert.SerializeObject(context, DefaultSerializerSettings));
        }

        public TraceContext OrchestrationTraceContextPeek()
        {
            var context = OrchestrationTraceContexts.Peek();
            return JsonConvert.DeserializeObject<TraceContext>(context, DefaultSerializerSettings);
        }

        public TraceContext OrchestrationTraceContextPop()
        {
            var context = OrchestrationTraceContexts.Pop();
            return JsonConvert.DeserializeObject<TraceContext>(context, DefaultSerializerSettings);
        }
    }


    class Context2
    {
        public Stack<Context2> OrchestrationState { get; set; }
        public string Name { get; set; }
        public Context2()
        {
            OrchestrationState = new Stack<Context2>();
        }
    }

    class Context2Store
    {
        public string Context2Json { get; set; }

    
        public Context2 Restore()
        {
            return JsonConvert.DeserializeObject<Context2>(Context2Json);
                
                //, new JsonSerializerSettings()
            //{
            //    TypeNameHandling = TypeNameHandling.Objects,
              
            //    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            //    ReferenceLoopHandling = ReferenceLoopHandling.Serialize
            //});
        }

        public static Context2Store Create(Context2 context)
        {
            var store = new Context2Store();

            var json = JsonConvert.SerializeObject(context, new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Objects,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize
            });
            store.Context2Json = json;
            return store;
        }

    }


    class Program
    {
        static void Main(string[] args)
        {
            var context = new Context2();
            context.OrchestrationState = new Stack<Context2>();

            context.OrchestrationState.Push(new Context2() { Name = "foo" });
            context.OrchestrationState.Push(context);
            var store = Context2Store.Create(context);

            var json = JsonConvert.SerializeObject(store);
        
            var context2Store = JsonConvert.DeserializeObject<Context2Store>(json);
            var context2 = context2Store.Restore();
            var elm1 = context2.OrchestrationState.Pop();
            var elm2 = context2.OrchestrationState.Pop();
            Console.WriteLine($"Element 1: {elm1.Name} Element 2: {elm2.Name}");
            Console.ReadLine();
        }

        private static void ThisStrategyDoesnotwork()
        {
            var context = new TraceContext();


            context.OrchestrationTraceContextPush(new TraceContext() { Name = "foo" });
            context.OrchestrationTraceContextPush(context);

            var json = JsonConvert.SerializeObject(context);
            var context2 = JsonConvert.DeserializeObject<TraceContext>(json);
            var elm1 = context2.OrchestrationTraceContextPop();
            var elm2 = context2.OrchestrationTraceContextPop();
            Console.WriteLine($"Element 1: {elm1.Name} Element 2: {elm2.Name}");
            Console.ReadLine();
        }

        private static void ReproduceIssue()
        {
            var context = new Context();
            context.OrchestrationState = new Stack<Context>();

            context.OrchestrationState.Push(new Context() { Name = "foo" });
            context.OrchestrationState.Push(context);

            var json = JsonConvert.SerializeObject(context);
            var context2 = JsonConvert.DeserializeObject<Context>(json);
            Console.WriteLine($"Element 1: {context2.OrchestrationState.Pop().Name} Element 2: {context2.OrchestrationState.Pop().Name}");

        }
    }
}
