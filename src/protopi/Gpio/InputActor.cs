using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Proto;
using Proto.Schedulers.SimpleScheduler;

namespace protopi
{
    public class InputActor : IActor
    {
        private readonly int _inputNumber;

        private SimpleScheduler _scheduler = new SimpleScheduler();
        private CancellationTokenSource _cts;
        private bool? _currentValue;

        public InputActor(int inputNumber)
        {
            _inputNumber = inputNumber;
        }

        public Task ReceiveAsync(IContext context)
        {
            switch (context.Message)
            {
                case Started _:
                    InitializeInput(context);
                    break;
                case InputPinStatus status:
                    Console.WriteLine($"Input pin changed to {status.IsEnabled}");
                    break;
                case Tick _:
                    CheckPinStatus();
                    break;
                case Stopped _:
                    _cts?.Cancel();
                    break;
            }
            return Actor.Done;
        }

        private void InitializeInput(IContext context)
        {
            Console.WriteLine($"Initializing input pin {_inputNumber}");

            if (!Directory.Exists($"/sys/class/gpio/gpio{_inputNumber}"))
            {
                Console.WriteLine($"...about to open pin {_inputNumber}");
                File.WriteAllText("/sys/class/gpio/export", $"{_inputNumber}");
            }
            else
            {
                Console.WriteLine("...pin is already open");
            }

            Console.WriteLine($"...specifying direction of Pin {_inputNumber} as IN");
            File.WriteAllText($"/sys/class/gpio/gpio{_inputNumber}/direction", "in");

            _scheduler.ScheduleTellRepeatedly(TimeSpan.Zero, TimeSpan.FromMilliseconds(50), context.Self, new Tick(), out _cts);
        }

        private void CheckPinStatus()
        {
            var valueString = File.ReadAllText($"/sys/class/gpio/gpio{_inputNumber}/value");
            var value = valueString.Contains("1");

            if (!_currentValue.HasValue || value != _currentValue.Value)
            {
                _currentValue = value;
                Console.WriteLine($"Input pin changed to {_currentValue}");
            }
        }
    }

    public class InputPinStatus
    {
        public bool IsEnabled { get; }

        public InputPinStatus(bool isEnabled)
        {
            IsEnabled = isEnabled;
        }
    }

    public class Tick { }
}
