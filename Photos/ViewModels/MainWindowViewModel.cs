using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using Avalonia.Controls;
using Avalonia.Input;
using Photos.Models;
using Avalonia.Platform.Storage;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Photos.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private IFolderPickerService _folderPickerService;

    [ObservableProperty] private ObservableCollection<string> _directories = [];
    [ObservableProperty] private ObservableCollection<Photo> _photos = [];

    [ObservableProperty] private Photo? _selectedPhoto;
    [ObservableProperty] private bool _photosLoaded = false;

    [ObservableProperty] private string _libraryPath = string.Empty;

    public string HelpDisclaimer { get; } =
        "Help\n\nPhotos lets you browse image folders, open pictures in a larger view, and move through your library quickly with either the mouse or the keyboard.\n\nGetting started\nChoose Open Library to select a folder, or drag an image file into the window to open its containing folder. Subfolders appear as cards at the top of the gallery. Click a folder card to enter it.\n\nBrowsing photos\nClick any thumbnail to open it in a larger view. Click the image again, click outside it, or press Escape to close it.\n\nKeyboard shortcuts\nUse the arrow keys to move through the photo grid.\nLeft and Right move one item at a time.\nUp and Down move one row at a time.\nPress Enter or Space to open the currently selected photo.\nPress Enter again or Escape to close the focused photo.\nPress Backspace to go to the parent folder.\n\nExtra notes\nClick the current path in the title bar to go up one level.\nSupported image formats: PNG, JPG, and JPEG.\n";
    [ObservableProperty] private bool _isHelpDisclaimerVisible = false;

    public MainWindowViewModel(IFolderPickerService folderPickerService)
    {
        _folderPickerService = folderPickerService;
    }

    private bool HasAccessRights(string folder)
    {
        try { Directory.GetFiles(folder); }
        catch { return false; }
        return true;
    }

    private async Task LoadDirectory(string? folder)
    {
        if (folder is null) return;
        if (!HasAccessRights(folder)) return;

        Photos.Clear();

        LibraryPath = folder != "/" ? folder.TrimEnd('/') : folder;
        Directories = new (Directory.EnumerateDirectories(folder).Select(x => x[LibraryPath.Length..].Trim('/')));

        await AddPhotosAsync(folder);
        PhotosLoaded = true;
    }

    public async Task OpenDirectory()
    {
        var folder = await _folderPickerService.PickFolderAsync();
        if (folder is null) return;

        await LoadDirectory(folder);
    }

    public async Task OpenDirectory(string folder) => await LoadDirectory(folder);

    public async Task OpenAboveDirectory() => await LoadDirectory(Path.GetDirectoryName(LibraryPath));

    public void CloseDirectory()
    {
        LibraryPath = string.Empty;
        Photos.Clear();
        PhotosLoaded = false;
    }

    private void SanitizeFiles(List<string> files, IEnumerable<string> extensions)
    => files.RemoveAll(x => !extensions.Any(ex => x.EndsWith(ex, StringComparison.OrdinalIgnoreCase)));

    private async Task AddPhotosAsync(string path)
    {
        try
        {
            var photoPaths = PhotoExtension.GetPhotoPathsFromDirectory(path);
            if (photoPaths is null) return;

            await Task.Run(() => PhotoExtension.CreateAndAddPhotosToList(photoPaths, path, Photos));
            foreach (var p in Photos) _ = p.LoadPreviewImage();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    public async void WindowDropHandler(DragEventArgs e)
    {
        var files = e.DataTransfer.TryGetFiles();
        if (files is null || files.Length == 0) return;

        var localPath = Path.GetDirectoryName(files[0].TryGetLocalPath());
        if (string.IsNullOrWhiteSpace(localPath)) return;

        LibraryPath = localPath;
        Photos.Clear();

        await AddPhotosAsync(localPath);
    }

    public async Task PhotoClicked(Image img)
    {
        SelectedPhoto = Photos.FirstOrDefault(x => x.PreviewImage == img.Source);
        if (SelectedPhoto is null) return;
        await SelectedPhoto.LoadFullImage();
    }

    public async Task DirectoryClicked(string folder) => await OpenDirectory(folder);

    public void ResetSelectedPhoto()
    {
        if (SelectedPhoto is null) return;
        SelectedPhoto.FullImage = null;
        SelectedPhoto = null;
    }

    public async Task TryFocusImage(int index)
    {
        if (index < 0 || index >= Photos.Count ) return;
        SelectedPhoto = Photos[index];
        await SelectedPhoto.LoadFullImage();
    }

    public void UnfocusPhoto() => ResetSelectedPhoto();

    public bool IsPhotoFocused() => SelectedPhoto != null;

    public int GetImageIndex(Image i)
    {
        var p = Photos.FirstOrDefault(x => x.PreviewImage == i.Source);
        if (p is null) return -1;
        return Photos.IndexOf(p);
    }

    public string? EvaluateFolderPath(string? folder)
    {
        if (folder is null) return null;
        var newPath = !folder.StartsWith(LibraryPath) ? Path.Combine(LibraryPath, folder) : folder;
        return Directory.Exists(newPath) ? newPath : null;
    }

    public void FlipHelpDisclaimerVisibility() => IsHelpDisclaimerVisible = !IsHelpDisclaimerVisible;
}