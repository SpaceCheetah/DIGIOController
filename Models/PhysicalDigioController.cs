using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace DIGIOController.Models; 

public class PhysicalDigioController : IDigioController {
    readonly BehaviorSubject<string> _currentPort = new("");

    public IObservable<string> CurrentPort => _currentPort;
    readonly BehaviorSubject<bool> _isConnected = new(false);
    public IObservable<bool> IsConnected => _isConnected;
    readonly ReplaySubject<Bit[]> _inputs = new(1);

    public IObservable<Bit[]> Inputs => _inputs;
    readonly ReplaySubject<Bit[]> _outputs = new(1);
    public IObservable<Bit[]> Outputs => _outputs;
    public int InputBits => 8;
    public int OutputBits => 8;
    public IEnumerable<string> GetComPorts() => SerialPort.GetPortNames();

    SerialPort? _serialPort = null;
    readonly object _readLock = new();
    readonly Subject<string> _messages = new();
    IEnumerable<string> DataSequence() {
        if (!Monitor.TryEnter(_readLock)) {
            throw new InvalidOperationException("Cannot read on multiple threads");
        }
        try {
            while (true) {
                string result;
                try {
                    _serialPort?.ReadTo("<");
                    result = _serialPort?.ReadTo(">") ?? string.Empty;
                }
                catch (Exception e) when (e is TimeoutException or InvalidOperationException or OperationCanceledException) {
                    break;
                }
                yield return result;
            }
        }
        finally {
            Monitor.Exit(_readLock);
        }
    }

    async Task<string> GetNextOutput() {
        int output = 0;
        foreach (Bit bit in (await Outputs.FirstAsync()).Where(b => b.Set)) {
            output |= 1 << bit.Position;
        }
        return $"P{output:X2}";
    }
    
    public async Task<bool> TryConnect(string port) {
        if (_isConnected.Value) {
            Disconnect();
        }
        _serialPort = new();
        _serialPort.PortName = port;
        _serialPort.BaudRate = 9600;
        _serialPort.Parity = Parity.None;
        _serialPort.DataBits = 8;
        _serialPort.StopBits = StopBits.One;
        _serialPort.Handshake = Handshake.None;
        _serialPort.ReadTimeout = 500;
        _serialPort.WriteTimeout = 500;
        _serialPort.Open();
        Observable.Interval(TimeSpan.FromMilliseconds(10))
            .TakeUntil(_isConnected.Skip(1).Where(connected => !connected))
            .SelectMany(_ => GetNextOutput())
            .Merge(_messages.Throttle(TimeSpan.FromMilliseconds(10))) //10ms delay in clock updates, to avoid spamming controller
            .SubscribeOn(RxApp.MainThreadScheduler)
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Subscribe(message => {
                try {
                    _serialPort?.Write($"<{message}>");
                }
                catch (Exception e) when (e is TimeoutException or InvalidOperationException or OperationCanceledException) {
                    Disconnect();
                }
            });
        _messages.OnNext("XX");
        if (await DataSequence().ToObservable().FirstAsync() == "YY") {
            _currentPort.OnNext(port);
            _inputs.OnNext(Enumerable.Range(0, 8).Select(i => new Bit(i)).ToArray());
            _outputs.OnNext(Enumerable.Range(0, 8).Select(i => new Bit(i)).ToArray());
            _isConnected.OnNext(true);
            DataSequence().ToObservable()
                .Where(response => response.Length != 0)
                .Select(response => int.TryParse(response, NumberStyles.HexNumber, null, out int result) ? result : -1)
                .Where(result => result != -1)
                .TakeUntil(_isConnected.Where(connected => !connected))
                .SubscribeOn(RxApp.TaskpoolScheduler)
                .ObserveOn(RxApp.MainThreadScheduler)
                .SelectMany(async output => {
                    foreach (Bit input in await Inputs.FirstAsync()) {
                        input.Set = (output & 1) == 1;
                        output >>= 1;
                    }
                    return Unit.Default;
                })
                .Subscribe(_ => { }, Disconnect);
            return true;
        } else {
            Disconnect();
            return false;
        }
    }
    public void Disconnect() {
        _serialPort?.Close();
        _serialPort = null;
        if (_isConnected.Value) {
            _isConnected.OnNext(false);
            _currentPort.OnNext("");
        }
    }
    public void SetClock(int frequencyHz) {
        if (!_isConnected.Value) {
            throw new InvalidOperationException("Cannot set clock when not connected");
        }
        int clockRegister = frequencyHz == 0 ? 0 : 0x7A12 / frequencyHz;
        //This (and only this) value are sent in decimal rather than hex
        _messages.OnNext($"C{clockRegister:D5}");
    }
}