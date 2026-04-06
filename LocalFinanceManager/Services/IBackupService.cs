using LocalFinanceManager.DTOs;

namespace LocalFinanceManager.Services;

public interface IBackupService
{
    Task<BackupData> CreateBackupAsync(Guid userId);
    Task<BackupValidationResultDto> ValidateBackupAsync(BackupData backup, Guid userId);
    Task<BackupRestoreResultDto> RestoreBackupAsync(Guid userId, BackupData backup, ConflictResolutionStrategy strategy);
}
