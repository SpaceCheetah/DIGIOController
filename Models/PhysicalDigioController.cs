using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Linq;
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
    readonly object _serialLock = new();
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
        if (await WriteAndReceive("XX") == "YY") {
            _currentPort.OnNext(port);
            _inputs.OnNext(Enumerable.Range(0, 8).Select(i => new Bit(i)).ToArray());
            _outputs.OnNext(Enumerable.Range(0, 8).Select(i => new Bit(i)).ToArray());
            _isConnected.OnNext(true);
            Observable.Interval(TimeSpan.FromMilliseconds(10))
                .TakeUntil(_isConnected.Where(connected => !connected))
                .SelectMany(_ => Observable.FromAsync(SendUpdate))
                .Where(response => response != String.Empty)
                .SelectMany(response => Observable.FromAsync(() => ParseUpdate(response)))
                .Subscribe();
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

    async Task<string> SendUpdate() {
        Bit[] outputs = await Outputs.FirstAsync();
        int result = 0;
        foreach (Bit output in outputs.Where(output => output.Set)) {
            result |= 1 << output.Position;
        }
        return await WriteAndReceive("P" + result.ToString("X2"));
    }

    async Task ParseUpdate(string update) {
        int responseInt = int.Parse(update, NumberStyles.HexNumber);
        foreach (Bit input in await Inputs.FirstAsync()) {
            input.Set = (responseInt & 1) == 1;
            responseInt >>= 1;
        }
    }

    async Task<string> WriteAndReceive(string toWrite) {
        if (_serialPort is null || !_serialPort.IsOpen) {
            Disconnect();
        }
        return await Task.Run(() => {
            string result;
            lock (_serialLock) {
                _serialPort?.Write($"<{toWrite}>");
                _serialPort?.ReadTo("<");
                result = _serialPort?.ReadTo(">") ?? string.Empty;
            }
            return result;
        });
    }
}