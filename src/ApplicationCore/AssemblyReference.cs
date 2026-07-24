namespace CrmAtlas.ApplicationCore;

/// <summary>
/// Provides a stable reference to the Application Core assembly for discovery
/// and architecture tests.
/// </summary>
public static class AssemblyReference
{
    public static readonly System.Reflection.Assembly Assembly = typeof(AssemblyReference).Assembly;
}
