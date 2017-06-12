using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Proto;
using Proto.Schedulers.SimpleScheduler;

namespace protopi
{
    public class OutputActor : IActor
    {
        private readonly int _outputNumber;

        public OutputActor(int outputNumber)
        {
            _outputNumber = outputNumber;
        }

        public Task ReceiveAsync(IContext context)
        {
            switch (context.Message)
            {
                case Started _:
                    InitializeOutput(context);
                    break;
                case InputPinStatus status:
                    SetOutputPin(status.IsEnabled);
                    break;
            }
            return Actor.Done;
        }

        private void InitializeOutput(IContext context)
        {
            Console.WriteLine($"Initializing output pin {_outputNumber}");

            if (!Directory.Exists($"/sys/class/gpio/gpio{_outputNumber}"))
            {
                Console.WriteLine($"...about to open pin {_outputNumber}");
                File.WriteAllText("/sys/class/gpio/export", $"{_outputNumber}");
            }
            else
            {
                Console.WriteLine("...pin is already open");
            }

            Console.WriteLine($"...specifying direction of Pin {_outputNumber} as OUT");
            File.WriteAllText($"/sys/class/gpio/gpio{_outputNumber}/direction", "out");

            File.WriteAllText($"/sys/class/gpio/gpio{_outputNumber}/value", "0");
        }

        private void SetOutputPin(bool isEnabled)
        {
            File.WriteAllText($"/sys/class/gpio/gpio{_outputNumber}/value", isEnabled ? "1" : "0");
        }
    }
}
