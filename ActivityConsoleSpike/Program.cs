using System;
using System.Diagnostics;
using Newtonsoft.Json;

namespace ActivityConsoleSpike
{
    class Program
    {
        static void Main(string[] args)
        {
            var activity = new Activity("hello");
            activity.Start();
            var option = new JsonSerializerSettings();
            option.TypeNameHandling = TypeNameHandling.Auto;
            
            var json = JsonConvert.SerializeObject(activity, option);
            Console.WriteLine(json);

            var obj = JsonConvert.DeserializeObject<Activity>(json, option); // Activity is not serializable.
            Console.ReadLine();

        }
    }
}
