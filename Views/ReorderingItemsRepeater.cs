using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using System.Collections;

namespace DIGIOController.Views; 

public class ReorderingItemsRepeater : ItemsRepeater {
    public readonly static StyledProperty<IList> ItemsListProperty =
        AvaloniaProperty.Register<ReorderingItemsRepeater, IList>(nameof(ItemsList));
    bool drag = false;
    int dragChildIndex = 0;

    public ReorderingItemsRepeater() {
        Bind(ItemsSourceProperty, this.GetObservable(ItemsListProperty));
    }

    public IList ItemsList {
        get => GetValue(ItemsListProperty);
        set => SetValue(ItemsListProperty, value);
    }
    
    protected override void OnPointerMoved(PointerEventArgs e) {
        base.OnPointerMoved(e);
        if (!drag) return;
        Control? child = TryGetElement(dragChildIndex);
        if (child is null) {
            drag = false;
            return;
        }
        Point point = e.GetCurrentPoint(child).Position;
        object? elem = ItemsList[dragChildIndex];
        if (point.X < -10 && dragChildIndex > 0) {
            ItemsList.RemoveAt(dragChildIndex);
            dragChildIndex--;
            ItemsList.Insert(dragChildIndex, elem);
        } else if (point.X > child.Bounds.Width + 10 && dragChildIndex < ItemsList.Count - 1) {
            ItemsList.RemoveAt(dragChildIndex);
            dragChildIndex++;
            ItemsList.Insert(dragChildIndex, elem);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e) {
        base.OnPointerPressed(e);
        foreach (var child in Children) {
            double relativeX = e.GetCurrentPoint(child).Position.X;
            if (relativeX >= 0 && relativeX < child.Bounds.Width) {
                dragChildIndex = GetElementIndex(child);
                drag = true;
                return;
            }
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e) {
        base.OnPointerReleased(e);
        drag = false;
    }
}