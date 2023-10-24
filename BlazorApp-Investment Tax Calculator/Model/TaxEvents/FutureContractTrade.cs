using Model;
using Model.TaxEvents;

namespace TaxEvents;

public record FutureContractTrade : Trade
{
    public required DescribedMoney ContractValue { get; set; }
}
