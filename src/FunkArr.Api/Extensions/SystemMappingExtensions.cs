using FunkArr.Core;
using ApiModels = FunkArr.Api.Models;

namespace FunkArr.Api.Extensions;

internal static class SystemMappingExtensions
{
    internal static ApiModels.VariantsResponse ToApi(this VariantOptions options) =>
        new(
        [
            new ApiModels.VariantPolicyEntry(ApiModels.MediathekVariant.AudioDescription, options.AudioDescription.ToApi()),
            new ApiModels.VariantPolicyEntry(ApiModels.MediathekVariant.SignLanguage, options.SignLanguage.ToApi()),
            new ApiModels.VariantPolicyEntry(ApiModels.MediathekVariant.PlainLanguage, options.PlainLanguage.ToApi()),
            new ApiModels.VariantPolicyEntry(ApiModels.MediathekVariant.OriginalVersion, options.OriginalVersion.ToApi())
        ]);

    internal static ApiModels.VariantPolicy ToApi(this VariantPolicy policy) =>
        policy == VariantPolicy.Allow ? ApiModels.VariantPolicy.Allow : ApiModels.VariantPolicy.Exclude;
}
