using System;
using System.Threading;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.SearchModule.Core.Extensions;

[Obsolete("Conversion helper for the legacy ICancellationToken overloads. Use CancellationToken.", DiagnosticId = "VC0014", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
public static class LegacyCancellationTokenExtensions
{
    /// <summary>
    /// Converts a legacy <see cref="ICancellationToken"/> to a <see cref="CancellationToken"/>.
    /// A <see cref="CancellationTokenWrapper"/> yields its underlying token. Any other implementation cannot be observed
    /// as a <see cref="CancellationToken"/>, so it is checked once here (throwing if already cancelled) and
    /// <see cref="CancellationToken.None"/> is returned; later cancellation of such a token is not observed.
    /// </summary>
    public static CancellationToken ToCancellationToken(this ICancellationToken cancellationToken)
    {
        if (cancellationToken is null)
        {
            return CancellationToken.None;
        }

        cancellationToken.ThrowIfCancellationRequested();

        return cancellationToken is CancellationTokenWrapper wrapper ? wrapper.CancellationToken : CancellationToken.None;
    }
}
