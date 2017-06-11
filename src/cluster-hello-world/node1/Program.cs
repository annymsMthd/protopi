using System;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Messages;
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
            Thread.Sleep(10000);
            var endsignal = new ManualResetEventSlim();

            AssemblyLoadContext.Default.Unloading += ctx =>
            {
                System.Console.WriteLine("Unloading fired");
                endsignal.Set();
            };

            Serialization.RegisterFileDescriptor(ProtosReflection.Descriptor);

            Remote.Start("node1", 12001);

            Cluster.Start("MyCluster", new ConsulProvider(new ConsulProviderOptions(), config =>
            {
                config.Address = new Uri("http://consul1:8500");
            }));

            Console.WriteLine("node1 started");

            Task.Run(async () =>
            {
                var pid = await Cluster.GetAsync("TheName", "HelloKind");

                var res = await pid.RequestAsync<HelloResponse>(new HelloRequest());

                Console.WriteLine(res.Message);
            });

            endsignal.Wait();
        }
    }
}
