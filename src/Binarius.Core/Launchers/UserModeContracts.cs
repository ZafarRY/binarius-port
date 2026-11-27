namespace Binarius.Core.Launchers;

public interface IUserModePrompter
{
    Task ShowInfoAsync(string message);
    Task<bool> AskCheckboxAsync(string message);
    Task<int> AskChoiceAsync(string message, IReadOnlyList<string> options); // -1 = batal
    Task<bool> ConfirmSaveAsync(string summary);
}

public sealed class UserModeOutcome
{
    public bool Applied { get; set; }
    public bool Saved { get; set; }
    public string? Failure { get; private set; }
    public List<string> ScriptErrors { get; } = new();
    public List<string> SaveErrors { get; } = new();

    public void Fail(string message) => Failure = message;
}