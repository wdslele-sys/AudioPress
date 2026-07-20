using AudioPress.Core.Compression;
using AudioPress.Core.Presets;

namespace AudioPress.Core.Utilities;

public static class OutputPathResolver
{
    public static OutputPathResolution Resolve(
        string inputPath,
        string? outputDirectory,
        bool useSourceCompressedFolder,
        AudioFormat format,
        SameNamePolicy sameNamePolicy,
        string sourceFolderName = "Compressed")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);

        var sourceDirectory = Path.GetDirectoryName(Path.GetFullPath(inputPath)) ?? Directory.GetCurrentDirectory();
        var targetDirectory = useSourceCompressedFolder || string.IsNullOrWhiteSpace(outputDirectory)
            ? Path.Combine(sourceDirectory, sourceFolderName)
            : Path.GetFullPath(outputDirectory);

        var baseName = Path.GetFileNameWithoutExtension(inputPath);
        var extension = format.GetExtension();
        var candidate = Path.Combine(targetDirectory, baseName + extension);

        if (!File.Exists(candidate))
        {
            return new OutputPathResolution(candidate, false, null);
        }

        return sameNamePolicy switch
        {
            SameNamePolicy.Skip => new OutputPathResolution(null, true, "目标文件已存在，已跳过。"),
            SameNamePolicy.Overwrite => new OutputPathResolution(candidate, false, "目标文件已存在，将覆盖。"),
            SameNamePolicy.AutoNumber => new OutputPathResolution(ResolveNumberedPath(targetDirectory, baseName, extension), false, null),
            _ => throw new ArgumentOutOfRangeException(nameof(sameNamePolicy), sameNamePolicy, null)
        };
    }

    private static string ResolveNumberedPath(string directory, string baseName, string extension)
    {
        for (var index = 1; index < 10_000; index++)
        {
            var candidate = Path.Combine(directory, $"{baseName} ({index}){extension}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("无法生成可用的自动编号文件名。");
    }
}

