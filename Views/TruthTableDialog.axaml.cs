using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DIGIOController.ViewModels;
using ReactiveUI;
using System;
using System.Reactive.Disposables;

namespace DIGIOController.Views; 

public partial class TruthTableDialog : ReactiveWindow<TruthTableDialogViewModel> {
    public TruthTableDialog() {
        InitializeComponent();
        this.WhenActivated(d => {
            ViewModel!.CancelCommand.Subscribe(_ => Close()).DisposeWith(d);
            ViewModel!.ConfirmCommand.Subscribe(_ => Close()).DisposeWith(d);
        });
    }
}