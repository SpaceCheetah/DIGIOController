using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DIGIOController.Models; 

public interface IDigioController {
    IObservable<string> CurrentPort { get; }
    IObservable<bool> IsConnected { get; }
    IObservable<Bit[]> Inputs { get; }
    IObservable<Bit[]> Outputs { get; }
    int InputBits { get; }
    int OutputBits { get; }
    /**
     * List of COM ports on the system
     */
    IEnumerable<string> GetComPorts();
    /**
     * Attempt connecting to all COM ports in sequence, until one succeeds
     */
    async Task<bool> TryAutoconnect() {
        IEnumerable<string> ports = GetComPorts();
        foreach (string port in ports) {
            if (await TryConnect(port)) {
                return true;
            }
        }
        return false;
    }
    Task<bool> TryConnect(string port);
    void Disconnect();
}