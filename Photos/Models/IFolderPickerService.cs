using System.Threading.Tasks;

namespace Photos.Models;

public interface IFolderPickerService
{
    Task<string?> PickFolderAsync();
}