using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using ReactiveUI;
using System.Collections;

namespace DIGIOController.Views; 

public class ReorderingItemsRepeater : ItemsRepeater {
    public readonly static StyledProperty<IList> ItemsListProperty =
        AvaloniaProperty.Register<ReorderingItemsRepeater, IList>(nameof(ItemsList));
    bool _drag = false;
    int _dragChildIndex = 0;
    bool _addClass = false;

    public ReorderingItemsRepeater() {
        Bind(ItemsSourceProperty, this.GetObservable(ItemsListProperty));
        LayoutUpdated += (sender, args) => {
            if (_addClass) {
                TryGetElement(_dragChildIndex)?.Classes.Add("dragged");
                _addClass = false;
            }
        };
    }

    public IList ItemsList {
        get => GetValue(ItemsListProperty);
        set => SetValue(ItemsListProperty, value);
    }
    
    protected override void OnPointerMoved(PointerEventArgs e) {
        base.OnPointerMoved(e);
        if (!_drag) return;
        Control? child = TryGetElement(_dragChildIndex);
        if (child is null) {
            StopDrag();
            return;
        }
        Point point = e.GetCurrentPoint(child).Position;
        object? elem = ItemsList[_dragChildIndex];
        int newIndex = _dragChildIndex;
        if (point.X < -10 && _dragChildIndex > 0) {
            newIndex--;
        } else if (point.X > child.Bounds.Width + 10 && _dragChildIndex < ItemsList.Count - 1) {
            newIndex++;
        }
        if (newIndex == _dragChildIndex) return;
        child.Classes.Remove("dragged");
        ItemsList.RemoveAt(_dragChildIndex);
        _dragChildIndex = newIndex;
        ItemsList.Insert(_dragChildIndex, elem);
        _addClass = true;
    }
    
    

    protected override void OnPointerPressed(PointerPressedEventArgs e) {
        base.OnPointerPressed(e);
        foreach (var child in Children) {
            double relativeX = e.GetCurrentPoint(child).Position.X;
            if (relativeX >= 0 && relativeX < child.Bounds.Width) {
                _dragChildIndex = GetElementIndex(child);
                child.Classes.Add("dragged");
                _drag = true;
                Classes.Add("dragging");
                return;
            }
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e) {
        base.OnPointerReleased(e);
        StopDrag();
    }

    void StopDrag() {
        _drag = false;
        Classes.Remove("dragging");
        TryGetElement(_dragChildIndex)?.Classes.Remove("dragged");
    }
}