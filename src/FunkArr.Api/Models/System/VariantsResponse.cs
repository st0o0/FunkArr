namespace FunkArr.Api.Models;

public sealed record VariantsResponse(VariantPolicyEntry[] Variants);

public sealed record VariantPolicyEntry(MediathekVariant Variant, VariantPolicy Policy);
