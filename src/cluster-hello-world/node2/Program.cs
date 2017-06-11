using System;
using System.Runtime.Loader;
using System.Threading;
using Messages;
using Proto;
using Proto.Cluster;
using Proto.Cluster.Consul;
using Proto.Remote;
using ProtosReflection = Messages.ProtosReflection;

namespace node1
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

            Serialization.RegisterFileDescriptor(ProtosReflection.Descriptor);
            var props = Actor.FromFunc(ctx =>
            {
                switch (ctx.Message)
                {
                    case HelloRequest _:
                        ctx.Respond(new HelloResponse
                        {
                            Message = "Hello from node 2"
                        });
                        break;
                }
                return Actor.Done;
            });

            Remote.RegisterKnownKind("HelloKind", props);
            Remote.Start("node2", 12000);
            Cluster.Start("MyCluster", new ConsulProvider(new ConsulProviderOptions(), config =>
            {
                config.Address = new Uri("http://consul1:8500");
            }));

            Console.WriteLine("node2 started");

            endsignal.Wait();
        }
    }
}
