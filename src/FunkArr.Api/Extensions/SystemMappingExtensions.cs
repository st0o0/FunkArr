using FunkArr.Api.Models.Shared;
using FunkArr.Api.Models.System;
using FunkArr.Core;
using VariantPolicy = FunkArr.Core.VariantPolicy;

namespace FunkArr.Api.Extensions;

internal static class SystemMappingExtensions
{
    internal static VariantsResponse ToApi(this VariantOptions options) =>
        new(
        [
            new VariantPolicyEntry(MediathekVariant.AudioDescription, options.AudioDescription.ToApi()),
            new VariantPolicyEntry(MediathekVariant.SignLanguage, options.SignLanguage.ToApi()),
            new VariantPolicyEntry(MediathekVariant.PlainLanguage, options.PlainLanguage.ToApi()),
            new VariantPolicyEntry(MediathekVariant.OriginalVersion, options.OriginalVersion.ToApi())
        ]);

    internal static Models.Shared.VariantPolicy ToApi(this VariantPolicy policy) =>
        policy == VariantPolicy.Allow ? Models.Shared.VariantPolicy.Allow : Models.Shared.VariantPolicy.Exclude;
}
