using System.IO;

namespace MhRenamerWpf.Services;

public sealed record RenameRequest(string OldPath, string NewPath);

public sealed record RenameResult(bool Succeeded, string? ErrorMessage = null)
{
    public static RenameResult Success() => new(true);
    public static RenameResult Failure(string message) => new(false, message);
}

public static class RenameService
{
    private sealed record StagedRename(RenameRequest Request, string TemporaryPath);

    public static RenameResult Execute(IReadOnlyList<RenameRequest> requests)
    {
        var effectiveRequests = requests
            .Where(request => !string.Equals(
                request.OldPath,
                request.NewPath,
                StringComparison.OrdinalIgnoreCase))
            .ToList();

        var duplicates = effectiveRequests
            .GroupBy(request => request.NewPath, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => Path.GetFileName(group.Key))
            .ToList();

        if (duplicates.Count > 0)
        {
            return RenameResult.Failure(
                $"リネーム後に名前が重複する項目があります。\n\n{string.Join("\n", duplicates)}");
        }

        var sourcePaths = new HashSet<string>(
            effectiveRequests.Select(request => request.OldPath),
            StringComparer.OrdinalIgnoreCase);

        var conflicts = effectiveRequests
            .Where(request => PathExists(request.NewPath) && !sourcePaths.Contains(request.NewPath))
            .Select(request => Path.GetFileName(request.NewPath))
            .ToList();

        if (conflicts.Count > 0)
        {
            return RenameResult.Failure(
                $"以下の名前は既に存在するためリネームできません。\n\n{string.Join("\n", conflicts)}");
        }

        var stagedRenames = new List<StagedRename>();
        var completedRenames = new List<StagedRename>();

        try
        {
            foreach (var request in effectiveRequests)
            {
                var temporaryPath = CreateTemporaryPath(request.OldPath);
                MovePath(request.OldPath, temporaryPath);
                stagedRenames.Add(new StagedRename(request, temporaryPath));
            }

            foreach (var stagedRename in stagedRenames)
            {
                MovePath(stagedRename.TemporaryPath, stagedRename.Request.NewPath);
                completedRenames.Add(stagedRename);
            }

            return RenameResult.Success();
        }
        catch (Exception exception)
        {
            RollBack(stagedRenames, completedRenames);
            return RenameResult.Failure(
                $"リネームに失敗しました。変更を可能な範囲で元に戻しました。\n\n{exception.Message}");
        }
    }

    private static void RollBack(
        IReadOnlyList<StagedRename> stagedRenames,
        IReadOnlyCollection<StagedRename> completedRenames)
    {
        foreach (var stagedRename in stagedRenames.Reverse())
        {
            try
            {
                var currentPath = completedRenames.Contains(stagedRename)
                    ? stagedRename.Request.NewPath
                    : stagedRename.TemporaryPath;

                if (PathExists(currentPath) && !PathExists(stagedRename.Request.OldPath))
                {
                    MovePath(currentPath, stagedRename.Request.OldPath);
                }
            }
            catch
            {
                // 復元処理は可能な項目だけ継続する。
            }
        }
    }

    private static string CreateTemporaryPath(string sourcePath)
    {
        var directory = Path.GetDirectoryName(sourcePath) ?? "";

        string temporaryPath;
        do
        {
            temporaryPath = Path.Combine(directory, $".mh-renamer-{Guid.NewGuid():N}.tmp");
        }
        while (PathExists(temporaryPath));

        return temporaryPath;
    }

    private static bool PathExists(string path) => File.Exists(path) || Directory.Exists(path);

    private static void MovePath(string sourcePath, string destinationPath)
    {
        if (Directory.Exists(sourcePath))
        {
            Directory.Move(sourcePath, destinationPath);
        }
        else
        {
            File.Move(sourcePath, destinationPath);
        }
    }
}
