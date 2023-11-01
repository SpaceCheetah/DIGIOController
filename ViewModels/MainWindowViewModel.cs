using DIGIOController.Models;
using ReactiveUI;
using Splat;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace DIGIOController.ViewModels;

public class MainWindowViewModel : ViewModelBase {
    readonly IDigioController _controller = Locator.Current.GetService<IDigioController>() ?? throw new NullReferenceException("No controller");
    public IObservable<Bit[]> Inputs { get; }
    public IObservable<Bit[]> Outputs { get; }
    int Normalize(int value) => value & ~(~0 << _controller.OutputBits);
    
    int _outputCombined = 0;
    //int? to work with some controls, which occasionally use null. Can never actually be null.
    public int? OutputCombined {
        get => _outputCombined;
        set => this.RaiseAndSetIfChanged(ref _outputCombined, Normalize(value ?? 0));
    }

    public MainWindowViewModel() {
        _controller.TryConnect("TEST1").Wait();
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
    }
}