using ReactiveUI;

namespace DIGIOController.Models; 

public class Bit : ReactiveObject {
    bool _set = false;

    public bool Set {
        get => _set;
        set => this.RaiseAndSetIfChanged(ref _set, value);
    }
    
    public int Position { get; }

    public Bit(int position) => Position = position;
}