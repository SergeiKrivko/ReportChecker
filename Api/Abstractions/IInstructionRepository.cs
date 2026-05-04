using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IInstructionRepository
{
    public Task<Instruction?> GetInstructionByIdAsync(Guid id, CancellationToken ct = default);
    public Task<IEnumerable<Instruction>> GetInstructionsAsync(Guid reportId, CancellationToken ct = default);

    public Task<Guid> CreateInstructionAsync(Guid reportId, string content, Guid userId, Guid? commentId = null,
        CancellationToken ct = default);

    public Task<bool> UpdateInstructionAsync(Guid id, string content, Guid userId, CancellationToken ct = default);
    public Task<bool> DeleteInstructionAsync(Guid id, CancellationToken ct = default);
}