namespace AudioPress.Services;

public interface IDialogService
{
    IReadOnlyList<string> PickMediaFiles(bool includeVideo);

    string? PickFolder(string? initialDirectory = null);
}

