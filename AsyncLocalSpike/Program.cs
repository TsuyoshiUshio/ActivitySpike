using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncLocalSpike
{
    class Context
    {
        public string Value { get; set; }
    }
    class Program
    {

        private static AsyncLocal<string> asyncLocal = new AsyncLocal<string>();
        private static AsyncLocal<Context> asyncLocalContext = new AsyncLocal<Context>();
        private static ThreadLocal<string> threadLocal = new ThreadLocal<string>();
        static void Main(string[] args)
        {
            var program = new Program();
            program.ExecuteAsync().GetAwaiter().GetResult();
            Console.ReadLine();
        }

        private async Task ExecuteAsync()
        {
            asyncLocal.Value = "Value 1";
            threadLocal.Value = "Value 1";
            asyncLocalContext.Value = new Context();
            asyncLocalContext.Value.Value = "Context Value 1";
            var t1 = Level1("1");
            asyncLocal.Value = "Value 2";
            threadLocal.Value = "Value 2";
            asyncLocalContext.Value = new Context();
            asyncLocalContext.Value.Value = "Context Value 2";
            var t2 = Level1("2");

            await t1;
            await t2;
            Console.WriteLine($"Level0: AsyncLocal: {asyncLocal.Value} AsyncLocalContext: {asyncLocalContext.Value.Value} Thread: {Thread.CurrentThread.ManagedThreadId}");
            Console.WriteLine($"Level0: ThreadLocal: {threadLocal.Value} Thread: {Thread.CurrentThread.ManagedThreadId}");
        }

        private async Task Level1(string value)
        {
            Console.WriteLine($"Level1:Start: {value} AsyncLocal: {asyncLocal.Value} ThreadLocal: {threadLocal.Value} AsyncLocalContext: {asyncLocalContext.Value.Value}  Thread: {Thread.CurrentThread.ManagedThreadId}");
            asyncLocal.Value = $"{asyncLocal.Value}.{value}";
            asyncLocalContext.Value.Value = $"{asyncLocal.Value}.{value}";
            threadLocal.Value = $"{threadLocal.Value}.{value}";
            // await Task.Delay(100);  // If you add wait, then, the it switch the Thread.
            await Level2(value);
            Console.WriteLine($"Level1:End: {value} AsyncLocal: {asyncLocal.Value} ThreadLocal: {threadLocal.Value} AsyncLocalContext: {asyncLocalContext.Value.Value}  Thread: {Thread.CurrentThread.ManagedThreadId}");

        }

        private async Task Level2(string value)
        {
            Console.WriteLine($"Level2:Start: {value} AsyncLocal: {asyncLocal.Value} ThreadLocal: {threadLocal.Value} AsyncLocalContext: {asyncLocalContext.Value.Value}  Thread: {Thread.CurrentThread.ManagedThreadId}");
            asyncLocal.Value = $"{asyncLocal.Value}.{value}";
            threadLocal.Value = $"{threadLocal.Value}.{value}";
            asyncLocalContext.Value.Value = $"{asyncLocal.Value}.{value}";
            Console.WriteLine($"Level2:End: {value} AsyncLocal: {asyncLocal.Value} ThreadLocal: {threadLocal.Value} AsyncLocalContext: {asyncLocalContext.Value.Value}  Thread: {Thread.CurrentThread.ManagedThreadId}");

        }
    }
}
