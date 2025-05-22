using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Robust.Client.WebView.Cef;

internal sealed partial class WebViewManagerCef
{
    private const string BaseCacheName = "cef_cache";
    private const string LockFileName = "robust.lock";
    private FileStream? _lockFileStream;
    // This probably shouldn't be a cvar because the only reason you'd need it change for legit just botting the game
    private const int MaxAttempts = 100;

    private string FindAndLockCacheDirectory(WritableDirProvider userData)
    {
        var finalAbsoluteCachePath = "";

        try
        {
            List<string> existingCacheDirs = GetExistingCacheDirectories(userData);
            existingCacheDirs.Sort();

            foreach (var relativeDirName in existingCacheDirs)
            {
                var absoluteDirPath = userData.GetFullPath(new ResPath($"/{relativeDirName}"));

                if (!Directory.Exists(absoluteDirPath)
                    || !TryAcquireDirectoryLock(absoluteDirPath, out FileStream? lockStream)) continue;

                _lockFileStream = lockStream;
                finalAbsoluteCachePath = absoluteDirPath;
                _sawmill.Debug($"Found and locked existing cache directory: {finalAbsoluteCachePath}");
                break;
            }

            if (string.IsNullOrEmpty(finalAbsoluteCachePath))
                finalAbsoluteCachePath = CreateLockNewCacheDir(userData);

            return finalAbsoluteCachePath;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to find or create cache directory", ex);
        }
    }

    private List<string> GetExistingCacheDirectories(WritableDirProvider userData)
    {
        List<string> existingCacheDirs = new();

        try
        {
            // shut
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var entryName in userData.DirectoryEntries(new ResPath("/")))
                if (entryName.StartsWith(BaseCacheName)
                    && userData.IsDir(new ResPath($"/{entryName}")))
                    existingCacheDirs.Add(entryName);
        }
        catch (IOException ex)
        {
            _sawmill.Warning($"Failed to enumerate cache directories: {ex.Message}");
            // Oh well, we'll make a new directory then
        }

        return existingCacheDirs;
    }

    private bool TryAcquireDirectoryLock(string directoryPath, [NotNullWhen(true)] out FileStream? lockStream)
    {
        lockStream = null;
        var lockFilePath = Path.Combine(directoryPath, LockFileName);

        try
        {
            if (File.Exists(lockFilePath))
            {
                if (IsLockFileValid(lockFilePath))
                {
                    _sawmill.Debug($"Cache directory {directoryPath} is locked by active process");
                    return false;
                }

                _sawmill.Debug($"Removing stale lock file: {lockFilePath}");
                File.Delete(lockFilePath);
            }

            lockStream = new FileStream(lockFilePath,
                FileMode.CreateNew,
                FileAccess.ReadWrite,
                FileShare.None,
                4096,
                FileOptions.DeleteOnClose);

            return true;
        }
        catch (IOException)
        {
            lockStream?.Dispose();
            lockStream = null;
            return false;
        }
    }

    // Check if this file is actually locked
    // This should except because we are using none fileshare
    private bool IsLockFileValid(string lockFilePath)
    {
        try
        {
            using FileStream testStream = new(lockFilePath, FileMode.Open, FileAccess.Read, FileShare.None);
            return false;
        }
        // I'd love to expose what process is using the file, but it's all like P/Invoke stuff therefore windows only
        // Passing the whole exception over is also meh.
        catch (IOException)
        {
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return true;
        }
    }

    private string CreateLockNewCacheDir(WritableDirProvider userData)
    {
        for (var attempts = 0; attempts < MaxAttempts; attempts++)
        {
            string newRelativeCacheDir = $"{BaseCacheName}{attempts}";
            string absolutePath = userData.GetFullPath(new ResPath($"/{newRelativeCacheDir}"));

            try
            {
                if (!TryCreateLockDir(absolutePath, out FileStream? lockStream))
                    continue;

                _lockFileStream = lockStream;
                _sawmill.Debug($"Created and locked new cache directory: {absolutePath}");
                return absolutePath;
            }
            catch (Exception ex)
            {
                _sawmill.Warning($"Failed to create directory {absolutePath}: {ex.Message}");
            }
        }

        throw new InvalidOperationException($"Failed to create any cache directory after {MaxAttempts} attempts");
    }

    private bool TryCreateLockDir(string directoryPath, [NotNullWhen(true)] out FileStream? lockStream)
    {
        lockStream = null;
        string lockFilePath = Path.Combine(directoryPath, LockFileName);

        try
        {
            if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

            lockStream = new FileStream(lockFilePath,
                FileMode.CreateNew,
                FileAccess.ReadWrite,
                FileShare.None,
                4096,
                FileOptions.DeleteOnClose);

            return true;
        }
        catch (IOException)
        {
            lockStream?.Dispose();
            lockStream = null;

            try
            {
                if (Directory.Exists(directoryPath) && !Directory.EnumerateFileSystemEntries(directoryPath).Any())
                    Directory.Delete(directoryPath);
            }
            catch
            {
                // ignored
            }

            return false;
        }
    }
}
