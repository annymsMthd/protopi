using System;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Proto;

namespace pi_hello_world
{
    class Program
    {
        static void Main(string[] args)
        {
            var endsignal = new ManualResetEventSlim();

            AssemblyLoadContext.Default.Unloading += ctx =>
            {
                System.Console.WriteLine("Unloading fired");
                endsignal.Set();
            };

            var props = Actor.FromProducer(() => new HelloActor());
            var pid = Actor.Spawn(props);
            pid.Tell(new Hello
            {
                Who = "ProtoActor"
            });

            Console.WriteLine("actor system started");
            endsignal.Wait();
        }

        internal class Hello
        {
            public string Who;
        }

        internal class HelloActor : IActor
        {
            public Task ReceiveAsync(IContext context)
            {
                var msg = context.Message;
                if (msg is Hello r)
                {
                    Console.WriteLine($"Hello {r.Who}");
                }
                return Actor.Done;
            }
        }
    }
}
