namespace CrmAtlas.ApplicationCore.IA;

public interface IContextRetriever
{
    Task<string> RetrieveAsync(string question, CancellationToken cancellationToken = default);
}
