using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Microsoft.VisualBasic.FileIO;
using Photos.Models;

namespace Photos.Views;

public class FolderPickerService(Window window) : IFolderPickerService
{
    private Window _window = window;

    public void SetDependencyWindow(Window window) => _window = window;

    public async Task<string?> PickFolderAsync()
    {
        var suggestedFolder = await _window.StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Pictures);

        var folders = await _window.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Select Folder",
                AllowMultiple = false,
                SuggestedStartLocation = suggestedFolder
            }
        );

        return folders.FirstOrDefault()?.Path.LocalPath;
    }
}