using Azure.Communication.CallAutomation;
using CallAutomation.Playground.Interfaces;
using CallAutomation.Playground.Tones;

namespace CallAutomation.Playground.Extensions;

internal static class DtmfToneExtensions
{
    /// <summary>
    /// Converts a <see cref="DtmfTone"/> into a <see cref="IDtmfTone"/> type which can be used as a generic type parameter constraint.
    /// </summary>
    /// <param name="tone"></param>
    /// <returns></returns>
    /// <exception cref="ApplicationException"></exception>
    public static IDtmfTone Convert(this DtmfTone tone)
    {
        if (tone == DtmfTone.One) return default(One);
        if (tone == DtmfTone.Two) return default(Two);
        if (tone == DtmfTone.Three) return default(Three);
        if (tone == DtmfTone.Four) return default(Four);
        if (tone == DtmfTone.Five) return default(Five);
        if (tone == DtmfTone.Six) return default(Six);
        if (tone == DtmfTone.Seven) return default(Seven);
        if (tone == DtmfTone.Eight) return default(Eight);
        if (tone == DtmfTone.Nine) return default(Nine);
        if (tone == DtmfTone.Zero) return default(Zero);

        if (tone == DtmfTone.Pound) return default(Pound);
        if (tone == DtmfTone.Asterisk) return default(Asterisk);

        throw new ApplicationException($"Unable to convert DtmfTone: {tone}");
    }

    /// <summary>
    /// Gets the first tone from the collection and returns a <see cref="IDtmfTone"/>.
    /// </summary>
    /// <param name="tones"></param>
    /// <returns></returns>
    /// <inheritdoc cref="Convert"/>
    public static IDtmfTone GetSingleTone(this IReadOnlyList<DtmfTone> tones) => tones.ToList().First().Convert();
}