using System;
using System.Net;
using System.Runtime.Loader;
using System.Threading;
using Proto;
using Proto.Cluster;
using Proto.Cluster.Consul;
using Proto.Remote;

namespace protopi
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

            var consulHostAddress = Environment.GetEnvironmentVariable("CONSUL_HOST");
            if (consulHostAddress == null)
            {
                Console.WriteLine("You must set the CONSUL_HOST environment variable to your consul host.");
                endsignal.Wait();
                return;
            }

            var ip = Environment.GetEnvironmentVariable("IP_ADDRESS");
            Console.WriteLine($"Node is at address {ip}");

            Remote.Start(ip.ToString(), 12001);

            Cluster.Start("protopi", new ConsulProvider(new ConsulProviderOptions(), config =>
            {
                config.Address = new Uri(consulHostAddress);
            }));

            var deviceType = Environment.GetEnvironmentVariable("DEVICE_TYPE");
            switch (deviceType)
            {
                case "INPUT":
                    var inputProps = Actor.FromProducer(() => new InputActor(21));
                    var inputPid = Actor.Spawn(inputProps);
                    Console.WriteLine("Input actor started...");
                    break;
                case "OUTPUT":
                    var outputProps = Actor.FromProducer(() => new OutputActor(21));
                    var outputPid = Actor.Spawn(outputProps);
                    outputPid.Tell(new InputPinStatus(true));
                    Console.WriteLine("Output actor started...");
                    break;
                case null:
                    Console.WriteLine("DEVICE_TYPE environment variable is not defined. please define and restart");
                    break;
                default:
                    Console.WriteLine($"DEVICE_TYPE '{deviceType}' is not reconized. use 'INPUT' or 'OUTPUT'");
                    break;
            }

            endsignal.Wait();
        }
    }
}
