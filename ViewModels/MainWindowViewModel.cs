using DIGIOController.Models;
using System.Linq;

namespace DIGIOController.ViewModels;

public class MainWindowViewModel : ViewModelBase {
    public Bit[] Inputs { get; } = Enumerable.Range(0, 8).Select(i => 7 - i).Select(i => new Bit(i)).ToArray();
    public Bit[] Outputs { get; } = Enumerable.Range(0, 8).Select(i => 7 - i).Select(i => new Bit(i)).ToArray();
}