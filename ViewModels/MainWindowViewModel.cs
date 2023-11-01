using DIGIOController.Models;
using ReactiveUI;
using System;
using System.Linq;

namespace DIGIOController.ViewModels;

public class MainWindowViewModel : ViewModelBase {
    public Bit[] Inputs { get; } = Enumerable.Range(0, 8).Select(i => 7 - i).Select(i => new Bit(i)).ToArray();
    public Bit[] Outputs { get; } = Enumerable.Range(0, 8).Select(i => 7 - i).Select(i => new Bit(i)).ToArray();

    static int Normalize(int value) => value & 0xFF;
    
    int _outputCombined = 0;
    public int? OutputCombined {
        get => _outputCombined;
        set => this.RaiseAndSetIfChanged(ref _outputCombined, Normalize(value ?? 0));
    }

    public MainWindowViewModel() {
        this.WhenAnyValue(x => x.OutputCombined).Subscribe(output => {
            for (int i = Outputs.Length - 1; i >= 0; i--) {
                Outputs[i].Set = (output & 1) == 1;
                output >>= 1;
            }
        });
        foreach (Bit b in Outputs) {
            int mask = 1 << b.Position;
            b.WhenAnyValue(x => x.Set).Subscribe(bit => {
                if (bit) {
                    OutputCombined |= mask;
                } else {
                    OutputCombined &= ~mask;
                }
            });
        }
    }
}