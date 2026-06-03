namespace Schiism.WPF.Models.Helpers
{
    using System.Windows;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using Microsoft.Xaml.Behaviors;

    public class ThumbDragBehavior : Behavior<Thumb>
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
                nameof(Command),
                typeof(ICommand),
                typeof(ThumbDragBehavior));

        public ICommand? Command
        {
            get => (ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        protected override void OnAttached()
        {
            AssociatedObject.DragDelta += OnDragDelta;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.DragDelta -= OnDragDelta;
        }

        private void OnDragDelta(
            object? sender,
            DragDeltaEventArgs e)
        {
            Command?.Execute(e.HorizontalChange);
        }
    }
}
