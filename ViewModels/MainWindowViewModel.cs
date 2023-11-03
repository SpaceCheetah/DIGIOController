using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using DIGIOController.Models;
using DIGIOController.Views;
using ReactiveUI;
using Splat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace DIGIOController.ViewModels;

public class MainWindowViewModel : ViewModelBase {
    readonly IDigioController _controller = Locator.Current.GetService<IDigioController>() ?? throw new NullReferenceException("No controller");
    public IObservable<Bit[]> Inputs { get; }
    public IObservable<Bit[]> Outputs { get; }
    public IObservable<bool> IsConnected => _controller.IsConnected;
    public IObservable<string> ConnectedPort => _controller.CurrentPort;

    readonly TruthTableDialogViewModel _truthTableDialogViewModel;
    int Normalize(int value) => value & ~(~0 << _controller.OutputBits);

    string _error = "";
    public string Error {
        get => _error;
        set => this.RaiseAndSetIfChanged(ref _error, value);
    }

    public async Task AutoConnect() {
        if (await _controller.TryAutoconnect()) {
            Error = "";
        }
        else {
            Error = "Automatic connection failed";
        }
    }

    public async Task Connect() {
        if (SelectedPort == "") {
            Error = "No port selected";
        } else if (await _controller.TryConnect(SelectedPort)) {
            Error = "";
        }
        else {
            Error = "Connection failed";
        }
    }

    decimal? _clock = 0;
    public decimal? Clock {
        get => _clock;
        set => this.RaiseAndSetIfChanged(ref _clock, value);
    }

    public async Task GenerateTruthTable() {
        _truthTableDialogViewModel.Confirmed = false;
        Window mainWindow = ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow!;
        Window dialog = new TruthTableDialog { DataContext = _truthTableDialogViewModel };
        await dialog.ShowDialog(mainWindow);
        if (!_truthTableDialogViewModel.Confirmed) return;
        List<int> outputOrder = new();
        List<int> inputOrder = new();
        List<string> labels = new();
        foreach (var column in _truthTableDialogViewModel.Outputs.Where(column => column.Enabled)) {
            outputOrder.Add(column.BitPosition);
            labels.Add(column.Label);
        }
        foreach (var column in _truthTableDialogViewModel.Inputs.Where(column => column.Enabled)) {
            inputOrder.Add(column.BitPosition);
            labels.Add(column.Label);
        }
        TruthTable.TruthTableSettings settings = new() {
            Delay = TimeSpan.FromMilliseconds(_truthTableDialogViewModel.DelayMilliseconds),
            OutputOrder = outputOrder,
            InputOrder = inputOrder,
            Labels = labels
        };
        List<List<bool>> table = await TruthTable.GenerateTruthTable(_controller, settings);
        string filePath = Path.Combine(Path.GetTempPath(), Path.ChangeExtension(Guid.NewGuid().ToString(), ".csv"));
        await using (var writer = new StreamWriter(filePath)) {
            await writer.WriteAsync(TruthTable.ConvertToCsv(table, settings));
        }
        ProcessStartInfo psInfo = new ProcessStartInfo {
            FileName = filePath,
            UseShellExecute = true
        };
        Process.Start(psInfo);
    }

    public void RefreshComPorts() { 
        ComPorts = _controller.GetComPorts().ToList();
        if (SelectedPort == "" && ComPorts.Count > 0) {
            SelectedPort = ComPorts[0];
        }
    }

    public void Disconnect() {
        _controller.Disconnect();
    }
    
    int _outputCombined = 0;
    //int? to work with some controls, which occasionally use null. Can never actually be null.
    public int? OutputCombined {
        get => _outputCombined;
        set => this.RaiseAndSetIfChanged(ref _outputCombined, Normalize(value ?? 0));
    }

    List<string> _comPorts = new();
    public List<string> ComPorts {
        get => _comPorts; 
        private set => this.RaiseAndSetIfChanged(ref _comPorts, value);
    }

    string _selectedPort = "";

    public string SelectedPort {
        get => _selectedPort;
        set => this.RaiseAndSetIfChanged(ref _selectedPort, value);
    }

    public MainWindowViewModel() {
        _truthTableDialogViewModel = new(_controller.OutputBits, _controller.InputBits);
        Func<Bit[],Bit[]> arraySorter = array => {
            array = (Bit[])array.Clone();
            Array.Sort(array, (bit1, bit2) => bit2.Position.CompareTo(bit1.Position));
            return array;
        };
        Inputs = _controller.Inputs.Select(arraySorter);
        Outputs = _controller.Outputs.Select(arraySorter);
        this.WhenAnyValue(x => x.OutputCombined).CombineLatest(Outputs)
            .Subscribe(zipped => {
                int outputCombined = zipped.First!.Value;
                for (int i = _controller.OutputBits - 1; i >= 0; i--) {
                    zipped.Second[i].Set = (outputCombined & 1) == 1;
                    outputCombined >>= 1;
                }
            });
        Outputs.SelectMany(bits => bits.ToObservable())
            .Select(bit => bit.WhenAnyValue(x => x.Set).Select(b => (b, bit))).Merge()
            .Subscribe(zipped => {
                int mask = 1 << zipped.Item2.Position;
                if (zipped.Item1) {
                    OutputCombined |= mask;
                } else {
                    OutputCombined &= ~mask;
                }
            });
        ComPorts = _controller.GetComPorts().ToList();
        if (ComPorts.Count > 0) {
            SelectedPort = ComPorts.First();
        }
        _controller.IsConnected.Where(connected => !connected)
            .Subscribe(_ => {
                OutputCombined = 0;
            });
        this.WhenAnyValue(x => x.Clock)
            .Merge(_controller.IsConnected.Where(connected => connected).Select(_ => Clock))
            .Skip(2)
            .Subscribe(clock => {
                int hz = 0;
                try {
                    if (clock is not null) {
                        hz = decimal.ToInt32(clock.Value);
                    }
                }
                catch (OverflowException) { }
                _controller.SetClock(decimal.ToInt32(hz));
            });
    }
}