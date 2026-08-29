namespace AlpacaAgent.Domain.Policies;

public sealed record CoveredCallLimits(int MinimumDte, int MaximumDte, decimal MinimumDelta, decimal MaximumDelta, decimal MaximumSpreadRatio, int MaximumContractsPerShareBlock, decimal MaximumPortfolioExposure, int RetryLimit);
public sealed record CoveredCallProfile(string Name, int MinimumDte, int MaximumDte, decimal MinimumDelta, decimal MaximumDelta, decimal MaximumSpreadRatio, int MaximumContractsPerShareBlock, decimal MaximumPortfolioExposure, int RetryLimit);
public sealed record ProfileValidation(bool IsValid, IReadOnlyList<string> Violations);
public static class CoveredCallProfileValidator
{
    public static ProfileValidation Validate(CoveredCallLimits outer, CoveredCallProfile inner)
    {
        var failures = new List<string>();
        if (inner.MinimumDte < outer.MinimumDte) failures.Add(nameof(inner.MinimumDte));
        if (inner.MaximumDte > outer.MaximumDte) failures.Add(nameof(inner.MaximumDte));
        if (inner.MinimumDelta < outer.MinimumDelta) failures.Add(nameof(inner.MinimumDelta));
        if (inner.MaximumDelta > outer.MaximumDelta) failures.Add(nameof(inner.MaximumDelta));
        if (inner.MaximumSpreadRatio > outer.MaximumSpreadRatio) failures.Add(nameof(inner.MaximumSpreadRatio));
        if (inner.MaximumContractsPerShareBlock > outer.MaximumContractsPerShareBlock) failures.Add(nameof(inner.MaximumContractsPerShareBlock));
        if (inner.MaximumPortfolioExposure > outer.MaximumPortfolioExposure) failures.Add(nameof(inner.MaximumPortfolioExposure));
        if (inner.RetryLimit > outer.RetryLimit) failures.Add(nameof(inner.RetryLimit));
        return new(failures.Count == 0, failures.Order(StringComparer.Ordinal).ToArray());
    }
}
