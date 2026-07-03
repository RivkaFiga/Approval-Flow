using ApprovalFlow.ConfigPolicy.Application.Dtos;
using ApprovalFlow.ConfigPolicy.Application.Ports;
using ApprovalFlow.ConfigPolicy.Domain.Entities;
using ApprovalFlow.ConfigPolicy.Domain.Exceptions;

namespace ApprovalFlow.ConfigPolicy.Application.Services;

public sealed class PolicyService
{
    private readonly IPolicyRepository _repo;
    private readonly IPolicyEventPublisher _publisher;

    public PolicyService(IPolicyRepository repo, IPolicyEventPublisher publisher)
    {
        _repo = repo;
        _publisher = publisher;
    }

    public async Task<PolicyDocumentDto> CreateAsync(CreatePolicyRequest request, CancellationToken ct = default)
    {
        var doc = PolicyDocument.Create(
            request.Name,
            request.Markdown,
            request.AutonomyCeilingUsd,
            request.AutonomyMinConfidence,
            request.BaseCurrency,
            request.FxRates.Select(kv => new FxRateEntry(kv.Key, kv.Value)),
            request.KnownVendors.Select(v => new KnownVendorEntry(v)));

        await _repo.AddAsync(doc, ct);
        await _repo.SaveChangesAsync(ct);
        await _publisher.PublishPolicyChangedAsync(doc.Id, doc.Version, ct);

        return ToDto(doc);
    }

    public async Task<PolicyDocumentDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var doc = await _repo.GetByIdAsync(id, ct) ?? throw new PolicyNotFoundException(id);
        return ToDto(doc);
    }

    public async Task<PolicyDocumentDto> GetActiveSnapshotAsync(CancellationToken ct = default)
    {
        var doc = await _repo.GetActiveAsync(ct)
            ?? throw new InvalidOperationException("No active policy document exists.");
        return ToDto(doc);
    }

    public async Task<IReadOnlyList<PolicyDocumentDto>> GetAllAsync(CancellationToken ct = default)
    {
        var docs = await _repo.GetAllAsync(ct);
        return docs.Select(ToDto).ToList();
    }

    public async Task<PolicyDocumentDto> UpdateAsync(Guid id, UpdatePolicyRequest request, CancellationToken ct = default)
    {
        var doc = await _repo.GetByIdAsync(id, ct) ?? throw new PolicyNotFoundException(id);

        if (doc.Version != request.ExpectedVersion)
            throw new ConcurrencyConflictException(id);

        doc.Update(
            request.Name,
            request.Markdown,
            request.AutonomyCeilingUsd,
            request.AutonomyMinConfidence,
            request.BaseCurrency,
            request.FxRates.Select(kv => new FxRateEntry(kv.Key, kv.Value)),
            request.KnownVendors.Select(v => new KnownVendorEntry(v)));

        await _repo.SaveChangesAsync(ct);
        await _publisher.PublishPolicyChangedAsync(doc.Id, doc.Version, ct);

        return ToDto(doc);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var doc = await _repo.GetByIdAsync(id, ct) ?? throw new PolicyNotFoundException(id);
        doc.Deactivate();
        await _repo.SaveChangesAsync(ct);
        await _publisher.PublishPolicyChangedAsync(doc.Id, doc.Version, ct);
    }

    private static PolicyDocumentDto ToDto(PolicyDocument doc) => new()
    {
        Id = doc.Id,
        Name = doc.Name,
        Markdown = doc.Markdown,
        AutonomyCeilingUsd = doc.AutonomyCeilingUsd,
        AutonomyMinConfidence = doc.AutonomyMinConfidence,
        BaseCurrency = doc.BaseCurrency,
        Version = doc.Version,
        CreatedAt = doc.CreatedAt,
        UpdatedAt = doc.UpdatedAt,
        IsActive = doc.IsActive,
        FxRates = doc.FxRates.ToDictionary(fx => fx.CurrencyCode, fx => fx.RateToBaseCurrency),
        KnownVendors = doc.KnownVendors.Select(v => v.VendorName).ToList()
    };
}
