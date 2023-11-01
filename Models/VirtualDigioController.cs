using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace DIGIOController.Models; 

public class VirtualDigioController : IDigioController {
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

    public Task<IEnumerable<string>> GetComPorts() {
        return Task.FromResult<IEnumerable<string>>(new[] { "TEST0", "TEST1" });
    }
    public Task<bool> TryConnect(string port) {
        if (_isConnected.Value) {
            Disconnect();
        }
        if (port != "TEST1") return Task.FromResult(false);
        _inputs.OnNext(Enumerable.Range(0, 8).Select(i => new Bit(i)).ToArray());
        _outputs.OnNext(Enumerable.Range(0, 8).Select(i => new Bit(i)).ToArray());
        _isConnected.OnNext(true);
        _currentPort.OnNext("TEST1");
        return Task.FromResult(true);
    }
    public void Disconnect() {
        if (_isConnected.Value) return;
        _isConnected.OnNext(false);
        _currentPort.OnNext("");
    }
}