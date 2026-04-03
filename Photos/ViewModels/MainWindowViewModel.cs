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

    [ObservableProperty] private ObservableCollection<Photo> _photos = [];

    [ObservableProperty] private Photo? _selectedPhoto;
    [ObservableProperty] private bool _photosLoaded = false;

    public string LibraryPath { get; private set; } = string.Empty;
    
    public MainWindowViewModel(IFolderPickerService folderPickerService)
    {
        _folderPickerService = folderPickerService;
    }

    public async Task OpenDirectory()
    {
        var folder = await _folderPickerService.PickFolderAsync();
        if (folder is null) return;

        // foreach(var p in Photos) p.Dispose();
        Photos.Clear();

        LibraryPath = folder;
        await AddPhotosAsync(folder);
        PhotosLoaded = true;
    }

    public void CloseDirectory()
    {
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

    public async Task PhotoClicked(PointerReleasedEventArgs e)
    {
        if(e.Source is not Image img) return;
        SelectedPhoto = Photos.FirstOrDefault(x => x.PreviewImage == img.Source);
        if (SelectedPhoto is null) return;
        await SelectedPhoto.LoadFullImage();
    }

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
}