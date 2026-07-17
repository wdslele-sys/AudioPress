namespace AudioPress.Services;

public interface IDialogService
{
    IReadOnlyList<string> PickAudioFiles();

    string? PickFolder(string? initialDirectory = null);
}

