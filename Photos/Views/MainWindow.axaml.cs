using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactions.DragAndDrop;
using Photos.Animations;
using Photos.Models;
using Photos.ViewModels;

namespace Photos.Views;

public partial class MainWindow : Window
{
    private enum NavigationInputMode
    {
        Mouse,
        Keyboard
    }

    private NavigationInputMode _navigationInputMode = NavigationInputMode.Mouse;

    private IFolderPickerService _folderPickerService;
    private int _currentImageIndex = 0;
    private Control? _currentHoveredCard;

    public MainWindow()
    {
        InitializeComponent();
        _folderPickerService = new FolderPickerService(this);
        DataContext = new MainWindowViewModel(_folderPickerService);
    }

    public void DropHandler(object sender, DragEventArgs e)
    {
        (DataContext as MainWindowViewModel)?.WindowDropHandler(e);
    }

    private async void OnKeyDownHandler(object sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        // input state changer
        switch (e.Key)
        {
            case Key.Enter:
            case Key.Escape:
            case Key.Left:
            case Key.Right:
            case Key.Up:
            case Key.Down:
                _navigationInputMode = NavigationInputMode.Keyboard;
                UpdatePointerHoverSuppression();
                UpdateKeyboardHover();
                break;
        }

        async Task HandleEnter()
        {
            if (vm.IsPhotoFocused()) vm.UnfocusPhoto();
            else await vm.TryFocusImage(_currentImageIndex);
        }

        void HandleEscape()
        {
            vm.UnfocusPhoto();
        }

        async Task HandleArrowKey()
        {
            if(vm.IsPhotoFocused()) await vm.TryFocusImage(_currentImageIndex);
            // TODO: move basic selection
        }

        int CalculateCurrentIndexHop()
        {
            // Values of UniformGridLayout
            var minItemWidth = 220;
            var minColumnSpacing = 18;

            var repeaterWidth = Repeater.Bounds.Width;

            var abstractedColumnCount = Math.Floor(repeaterWidth / (minItemWidth + minColumnSpacing));
            return Math.Max(1, (int)abstractedColumnCount);
        }

        void HandleArrowKeyDown()
        {
            var nextIdx = CalculateCurrentIndexHop();
            if (_currentImageIndex + nextIdx >= vm.Photos.Count) _currentImageIndex = vm.Photos.Count - 1;
            else _currentImageIndex += nextIdx;
        }

        void HandleArrowKeyUp()
        {
            var nextIdx = CalculateCurrentIndexHop();
            if (_currentImageIndex - nextIdx < 0) _currentImageIndex = 0;
            else _currentImageIndex -= nextIdx;
        }

        Control? FindCardControlForCurrentIndex()
        {
            if (_currentImageIndex < 0 || _currentImageIndex >= vm.Photos.Count)
                return null;

            var currentPhoto = vm.Photos[_currentImageIndex];

            return Repeater.GetVisualDescendants()
                .OfType<Control>()
                .FirstOrDefault(c =>
                    ReferenceEquals(c.DataContext, currentPhoto) &&
                    CardHoverEffectBehavior.GetIsEnabled(c));
        }

        void UpdateKeyboardHover()
        {
            if (DataContext is not MainWindowViewModel vm)
                return;

            if (_currentHoveredCard is not null)
            {
                CardHoverEffectBehavior.ResetHover(_currentHoveredCard);
                _currentHoveredCard = null;
            }

            var newCard = FindCardControlForCurrentIndex();
            if (newCard is null) return;

            CardHoverEffectBehavior.ApplyHover(newCard);
            _currentHoveredCard = newCard;
            _currentHoveredCard.BringIntoView();
        }

        switch (e.Key)
        {
            case Key.Enter: case Key.Space:
                if (!vm.PhotosLoaded) await vm.OpenDirectory();
                else await HandleEnter();
                break;
            case Key.Escape:
                HandleEscape();
                break;
            case Key.Down:
                HandleArrowKeyDown();
                UpdateKeyboardHover();
                await HandleArrowKey();
                break;
            case Key.Up:
                HandleArrowKeyUp();
                UpdateKeyboardHover();
                await HandleArrowKey();
                break;
            case Key.Left:
                if (_currentImageIndex > 0)
                {
                    _currentImageIndex--;
                    UpdateKeyboardHover();
                    await HandleArrowKey();
                }
                break;
            case Key.Right:
                if (_currentImageIndex < vm.Photos.Count - 1)
                {
                    _currentImageIndex++;
                    UpdateKeyboardHover();
                    await HandleArrowKey();
                }
                break;
        }
    }

    private void PointerEnteredPhotoHandler(object sender, PointerEventArgs e)
    {
        if (sender is not Image image || DataContext is not MainWindowViewModel vm) return;

        var photo = image.DataContext as Photo;
        if (photo is null) return;

        var idx = vm.Photos.IndexOf(photo);
        if (idx < 0) return;

        _navigationInputMode = NavigationInputMode.Mouse;
        UpdatePointerHoverSuppression();
        _currentImageIndex = idx;

        if (_currentHoveredCard is null) return;

        CardHoverEffectBehavior.ResetHover(_currentHoveredCard);
        _currentHoveredCard = null;
    }

    private void UpdatePointerHoverSuppression()
    {
        foreach (var control in Repeater.GetVisualDescendants().OfType<Control>())
        {
            if (CardHoverEffectBehavior.GetIsEnabled(control))
            {
                CardHoverEffectBehavior.SetSuppressPointerHover(
                    control,
                    _navigationInputMode == NavigationInputMode.Keyboard);
            }
        }
    }
}