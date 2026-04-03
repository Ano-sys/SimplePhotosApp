using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Photos.Models;

public partial class Photo : ObservableObject, IDisposable
{
    [ObservableProperty] private string _displayName;
    [ObservableProperty] private string _fullPath;
    [ObservableProperty] private string _displayPath;

    [ObservableProperty] private Bitmap? _previewImage;
    [ObservableProperty] private Bitmap? _fullImage;

    public Photo(string path, string relativeTo)
    {
        DisplayName = System.IO.Path.GetFileName(path);
        FullPath = path;
        DisplayPath = path.Remove(0, relativeTo.Length + 1);
    }

    private async Task<Bitmap?> LoadImageAsync(bool scaled = false)
    {
        var image = await Task.Run(() =>
        {
            Bitmap? i = null;

            using (var stream = File.OpenRead(FullPath))
            {
                i = scaled ? Bitmap.DecodeToWidth(stream, 200) : new Bitmap(stream);
            }

            return i;
        });

        return image;
    }

    public async Task LoadPreviewImage()
    {
        var image = await LoadImageAsync(true);
        Dispatcher.UIThread.Post(() =>
        {
            PreviewImage?.Dispose();
            PreviewImage = image;
        });
    }

    public async Task LoadFullImage()
    {
        var image = await LoadImageAsync();
        Dispatcher.UIThread.Post(() =>
        {
            FullImage?.Dispose();
            FullImage = image;
        });
    }

    public void Dispose()
    {
        FullImage?.Dispose();
        PreviewImage?.Dispose();
        GC.SuppressFinalize(this);
    }
}

public static partial class PhotoExtension
{
    private static readonly string[] SupportedFileExtensions = [".png", ".jpg", ".jpeg"];

    public static List<string>? GetPhotoPathsFromDirectory(string path)
    {
        if (!Directory.Exists(path)) return null;
        var files = Directory.EnumerateFiles(path);

        return files.Where(f => SupportedFileExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase)).ToList();
    }

    public static List<Photo>? GetPhotosFromDirectory(string path)
    {
        if (!Directory.Exists(path)) return null;
        var files = Directory.EnumerateFiles(path);

        return files.Where(f => SupportedFileExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                    .Select(x => new Photo(x, path))
                    .ToList();
    }

    public static void CreateAndAddPhotosToList(List<string> photos, string rootPath, ObservableCollection<Photo> toAddTo)
    {
        var bag = new ConcurrentBag<Photo>();

        Parallel.ForEach(photos, p =>
        {
            bag.Add(new Photo(p, rootPath));
        });

        Dispatcher.UIThread.Invoke(() =>
        {
            foreach (var photo in bag)
            {
                toAddTo.Add(photo);
            }
        });
    }
}