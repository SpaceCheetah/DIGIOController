using DynamicData;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text.RegularExpressions;

namespace DIGIOController.ViewModels; 

public class TruthTableDialogViewModel : ViewModelBase {
    readonly int _outputBits;
    readonly int _inputBits;

    bool _confirmed = false;
    public bool Confirmed {
        get => _confirmed;
        set => this.RaiseAndSetIfChanged(ref _confirmed, value);
    }

    int _delayMilliseconds = 30;
    public int DelayMilliseconds {
        get => _delayMilliseconds;
        set => this.RaiseAndSetIfChanged(ref _delayMilliseconds, value);
    }

    public ReactiveCommand<Unit, Unit> CancelCommand { get; }
    
    public ReactiveCommand<Unit, Unit> ConfirmCommand { get; }

    public ObservableCollection<TruthTableColumn> Outputs { get; } = new();
    public ObservableCollection<TruthTableColumn> Inputs { get; } = new();

    public TruthTableDialogViewModel(int outputBits, int inputBits) {
        _outputBits = outputBits;
        _inputBits = inputBits;
        Outputs.AddRange(
            Enumerable.Range(0, _outputBits)
                .Select(i => new TruthTableColumn(i) {Label = $"OUT{i}"})
                .Reverse());
        Inputs.AddRange(
            Enumerable.Range(0, _inputBits)
                .Select(i => new TruthTableColumn(i) {Label = $"IN{i}"})
                .Reverse());
        ConfirmCommand = ReactiveCommand.Create(() => {
            Confirmed = true;
        });
        CancelCommand = ReactiveCommand.Create(() => { });
    }
}

public class TruthTableColumn : ViewModelBase {
    public int BitPosition { get; }
    string _label = "";
    public string Label {
        get => _label;
        set {
            Regex regex = new Regex(@"^[a-zA-Z0-9\s]*$");
            if (!regex.IsMatch(value)) {
                throw new ArgumentException("Label may not contain special characters");
            }
            this.RaiseAndSetIfChanged(ref _label, value);
        }
    }
    bool _enabled = false;
    public bool Enabled {
        get => _enabled;
        set => this.RaiseAndSetIfChanged(ref _enabled, value);
    }

    public TruthTableColumn(int bitPosition) {
        BitPosition = bitPosition;
    }
}