using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using CashApp.Models;
using Microsoft.Extensions.Logging;
using Serilog;

namespace CashApp.Services
{
    public class BackupService
    {
        private readonly DatabaseService _databaseService;
        private readonly ILogger<BackupService> _logger;
        private readonly string _backupDirectory;

        public BackupService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _logger = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog();
            }).CreateLogger<BackupService>();

            _backupDirectory = Path.Combine(AppContext.BaseDirectory, "Backups");
            EnsureBackupDirectoryExists();
        }

        public async Task<string> CreateBackupAsync(string? customPath = null)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupName = $"CashApp_Backup_{timestamp}.zip";
                var backupPath = customPath ?? Path.Combine(_backupDirectory, backupName);

                await Task.Run(() => CreateZipBackup(backupPath));

                await _databaseService.LogActivityAsync(0, AuditAction.BackupCreated,
                    $"Full backup created: {backupPath}", AuditLogLevel.Info);

                _logger.LogInformation("Backup created successfully: {BackupPath}", backupPath);
                return backupPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create backup");
                throw;
            }
        }

        public async Task<bool> RestoreBackupAsync(string backupPath)
        {
            try
            {
                if (!File.Exists(backupPath))
                {
                    throw new FileNotFoundException("Backup file not found", backupPath);
                }

                // Extract backup to temporary location
                var tempRestorePath = Path.Combine(Path.GetTempPath(), "CashApp_Restore");
                if (Directory.Exists(tempRestorePath))
                {
                    Directory.Delete(tempRestorePath, true);
                }

                await Task.Run(() => ZipFile.ExtractToDirectory(backupPath, tempRestorePath));

                // Restore database
                var databasePath = Path.Combine(tempRestorePath, "Database", "CashApp.db");
                if (File.Exists(databasePath))
                {
                    await _databaseService.RestoreDatabaseAsync(databasePath);
                }

                // Restore settings and other files
                await RestoreApplicationDataAsync(tempRestorePath);

                await _databaseService.LogActivityAsync(0, AuditAction.BackupRestored,
                    $"Backup restored from: {backupPath}", AuditLogLevel.Info);

                _logger.LogInformation("Backup restored successfully from: {BackupPath}", backupPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore backup from: {BackupPath}", backupPath);
                return false;
            }
        }

        public async Task<IEnumerable<BackupInfo>> GetAvailableBackupsAsync()
        {
            try
            {
                var backupFiles = Directory.GetFiles(_backupDirectory, "*.zip")
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .ToList();

                var backups = new List<BackupInfo>();

                foreach (var file in backupFiles)
                {
                    var fileInfo = new FileInfo(file);
                    backups.Add(new BackupInfo
                    {
                        FilePath = file,
                        FileName = Path.GetFileName(file),
                        CreatedDate = fileInfo.LastWriteTime,
                        Size = fileInfo.Length
                    });
                }

                return backups;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get available backups");
                return new List<BackupInfo>();
            }
        }

        public async Task<bool> DeleteBackupAsync(string backupPath)
        {
            try
            {
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);

                    await _databaseService.LogActivityAsync(0, AuditAction.BackupCreated,
                        $"Backup deleted: {backupPath}", AuditLogLevel.Info);

                    _logger.LogInformation("Backup deleted: {BackupPath}", backupPath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete backup: {BackupPath}", backupPath);
                return false;
            }
        }

        public async Task<BackupInfo> GetBackupInfoAsync(string backupPath)
        {
            try
            {
                var fileInfo = new FileInfo(backupPath);
                return new BackupInfo
                {
                    FilePath = backupPath,
                    FileName = Path.GetFileName(backupPath),
                    CreatedDate = fileInfo.LastWriteTime,
                    Size = fileInfo.Length
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get backup info for: {BackupPath}", backupPath);
                return new BackupInfo();
            }
        }

        public async Task<bool> ScheduleAutomaticBackupAsync(string scheduleTime)
        {
            try
            {
                // In a real application, you would implement a proper scheduler
                // For now, we'll just log the scheduling
                await _databaseService.LogActivityAsync(0, AuditAction.SettingsChanged,
                    $"Automatic backup scheduled for: {scheduleTime}", AuditLogLevel.Info);

                _logger.LogInformation("Automatic backup scheduled for: {ScheduleTime}", scheduleTime);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to schedule automatic backup");
                return false;
            }
        }

        private void CreateZipBackup(string backupPath)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "CashApp_Backup");
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }

                Directory.CreateDirectory(tempDir);
                Directory.CreateDirectory(Path.Combine(tempDir, "Database"));
                Directory.CreateDirectory(Path.Combine(tempDir, "Logs"));
                Directory.CreateDirectory(Path.Combine(tempDir, "Settings"));

                // Copy database
                var databasePath = "CashApp.db";
                if (File.Exists(databasePath))
                {
                    File.Copy(databasePath, Path.Combine(tempDir, "Database", "CashApp.db"));
                }

                // Copy logs
                var logFiles = Directory.GetFiles("logs", "*.txt");
                foreach (var logFile in logFiles)
                {
                    File.Copy(logFile, Path.Combine(tempDir, "Logs", Path.GetFileName(logFile)));
                }

                // Copy settings
                var settingsFiles = Directory.GetFiles("Config", "*.*");
                foreach (var settingsFile in settingsFiles)
                {
                    File.Copy(settingsFile, Path.Combine(tempDir, "Settings", Path.GetFileName(settingsFile)));
                }

                // Create zip file
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }

                ZipFile.CreateFromDirectory(tempDir, backupPath);
            }
            finally
            {
                // Clean up temporary directory
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        private async Task RestoreApplicationDataAsync(string restorePath)
        {
            try
            {
                // Restore logs
                var logSourcePath = Path.Combine(restorePath, "Logs");
                if (Directory.Exists(logSourcePath))
                {
                    var logDestPath = "logs";
                    if (Directory.Exists(logDestPath))
                    {
                        Directory.Delete(logDestPath, true);
                    }

                    CopyDirectory(logSourcePath, logDestPath);
                }

                // Restore settings
                var settingsSourcePath = Path.Combine(restorePath, "Settings");
                if (Directory.Exists(settingsSourcePath))
                {
                    var settingsDestPath = "Config";
                    CopyDirectory(settingsSourcePath, settingsDestPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore application data");
            }
        }

        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
                CopyDirectory(subDir, destSubDir);
            }
        }

        private void EnsureBackupDirectoryExists()
        {
            if (!Directory.Exists(_backupDirectory))
            {
                Directory.CreateDirectory(_backupDirectory);
            }
        }
    }

    public class BackupInfo
    {
        public string FilePath { get; set; } = "";
        public string FileName { get; set; } = "";
        public DateTime CreatedDate { get; set; }
        public long Size { get; set; }

        public string FormattedSize
        {
            get
            {
                const long KB = 1024;
                const long MB = KB * 1024;
                const long GB = MB * 1024;

                return Size switch
                {
                    >= GB => $"{Size / (double)GB:F2} GB",
                    >= MB => $"{Size / (double)MB:F2} MB",
                    >= KB => $"{Size / (double)KB:F2} KB",
                    _ => $"{Size} Bytes"
                };
            }
        }
    }
}
