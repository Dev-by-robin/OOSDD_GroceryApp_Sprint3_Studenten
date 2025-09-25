
namespace Grocery.Core.Interfaces.Services
{
    public interface IFileSaverService
    {
        Task SaveFileAsync(string fileName, string content, CancellationToken cancellationToken);
        string FilePath { get; }
    }
}
