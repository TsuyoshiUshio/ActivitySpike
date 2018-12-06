using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reactive;

namespace DiagnosticSourceSpikeForFramework
{

        class Program
        {

            private static DiagnosticSource commandlineLogger = new DiagnosticListener("System.Console");

            private static IDisposable networkSubscription = null;

            private static IDisposable listenerSubscription = DiagnosticListener.AllListeners.Subscribe(
                delegate (DiagnosticListener listener)
                {
                    if (listener.Name == "System.Console")
                    {
                        if (networkSubscription != null)
                            networkSubscription.Dispose();

                        networkSubscription = listener.Subscribe((KeyValuePair<string, object> evnt) =>
                        {
                            Console.WriteLine("From Listener {0} Received Evnet {1} with Payload {2}",
                                listener.Name, evnt.Key, evnt.Value.ToString());
                        });
                    }
                });
            static void Main(string[] args)
            {
                commandlineLogger.IsEnabled("Hello");
                commandlineLogger.Write("Request", new { Name = "hi" });
                Console.ReadLine();
            }
        }
}
