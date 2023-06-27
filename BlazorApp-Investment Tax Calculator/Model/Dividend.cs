using Enum;
using System.Globalization;

namespace Model;

public record Dividend : TaxEvent
{
    public required DividendType DividendType { get; set; }
    public RegionInfo CompanyLocation { get; set; } = RegionInfo.CurrentRegion;
    public required DescribedMoney Proceed { get; set; }
}
