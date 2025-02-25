﻿namespace InvestmentTaxCalculator.Services.PdfExport;

using PdfSharp.Fonts;

using System.Net.Http;
using System.Threading;

public class CustomFontResolver(HttpClient httpClient) : IFontResolver
{
    private byte[]? _openSans;
    private byte[]? _openSansBold;
    private byte[]? _openSansBoldItalic;
    private byte[]? _openSansItalic;

    public async Task InitializeFontsAsync()
    {
        _openSans = await GetFontData("OpenSans-Regular.ttf", httpClient);
        _openSansBold = await GetFontData("OpenSans-Bold.ttf", httpClient);
        _openSansBoldItalic = await GetFontData("OpenSans-BoldItalic.ttf", httpClient);
        _openSansItalic = await GetFontData("OpenSans-Italic.ttf", httpClient);
    }

    private static async Task<byte[]> GetFontData(string name, HttpClient httpClient, CancellationToken cancellationToken = default)
    {
        await using var sourceStream = await httpClient.GetStreamAsync($"fonts/{name}", cancellationToken);
        using var memoryStream = new MemoryStream();
        await sourceStream.CopyToAsync(memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }

    public byte[] GetFont(string faceName)
    {
        InvalidOperationException notInitialised = new("Font is not initialised with InitializeFontsAsync");
        return faceName switch
        {
            "OpenSans-Bold.ttf" => _openSansBold ?? throw notInitialised,
            "OpenSans-BoldItalic.ttf" => _openSansBoldItalic ?? throw notInitialised,
            "OpenSans-Italic.ttf" => _openSansItalic ?? throw notInitialised,
            _ => _openSans ?? throw notInitialised,
        };
    }

    public FontResolverInfo ResolveTypeface(string familyName, bool bold, bool italic)
    {
        FontResolverInfo fontResolverInfo = (bold, italic) switch
        {
            (true, true) => new FontResolverInfo("OpenSans-BoldItalic.ttf"),
            (true, false) => new FontResolverInfo("OpenSans-Bold.ttf"),
            (false, true) => new FontResolverInfo("OpenSans-Italic.ttf"),
            (false, false) => new FontResolverInfo("OpenSans-Regular.ttf")
        };
        return fontResolverInfo;
    }
}
