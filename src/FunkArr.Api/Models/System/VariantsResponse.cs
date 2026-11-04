using FunkArr.Api.Models.Shared;

namespace FunkArr.Api.Models.System;

public sealed record VariantsResponse(VariantPolicyEntry[] Variants);

public sealed record VariantPolicyEntry(MediathekVariant Variant, VariantPolicy Policy);
