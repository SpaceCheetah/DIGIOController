using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DIGIOController.Models;
using DIGIOController.ViewModels;
using DIGIOController.Views;
using Splat;

namespace DIGIOController;

public partial class App : Application {
    public override void Initialize() { AvaloniaXamlLoader.Load(this); }

    public override void OnFrameworkInitializationCompleted() {
        Locator.CurrentMutable.RegisterConstant(new PhysicalDigioController(), typeof(IDigioController));
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            desktop.MainWindow = new MainWindow {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}