using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model.Interfaces;

using System.Collections.Immutable;
using System.Text;

namespace InvestmentTaxCalculator.Model.TaxEvents;

public record Trade : TaxEvent, ITextFilePrintable
{
    public virtual AssetCategoryType AssetType { get; set; } = AssetCategoryType.STOCK;
    public virtual required TradeType AcquisitionDisposal { get; set; }
    private decimal _quantity;
    /// <summary>
    /// Greater than 0 regardless of acquisition or disposal
    /// </summary>
    public virtual required decimal Quantity
    {
        get { return _quantity; }
        set
        {
            if (value < 0) throw new ArgumentException("Quantity must be greater than 0");
            _quantity = value;
        }
    }
#pragma warning disable CS8618 // backfield already set in required property GrossProceed
    private DescribedMoney _grossProceed;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    /// <summary>
    /// Greater than 0 regardless of acquisition or disposal
    /// </summary>
    public virtual required DescribedMoney GrossProceed
    {
        get { return _grossProceed; }
        set
        {
            if (value.Amount.Amount < 0) throw new ArgumentException("Gross Proceed must be greater than 0");
            _grossProceed = value;
        }
    }
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// <para> positive = charge: take money from you. </para>
    /// <para> negative = rebate: give you money </para>
    /// </summary>
    public ImmutableList<DescribedMoney> Expenses { get; init; } = [];
    public TradeReason TradeReason { get; set; } = TradeReason.OrderedTrade;
    /// <summary>
    /// indicate if the cost of the option is added to this trade already or not.
    /// </summary>
    public bool OptionAttached { get; set; } = false;
    /// <summary>
    /// If a trade is a result of an option, the cost or premium received is rolled to the trade. 
    /// </summary>
    /// <param name="cost"></param>
    /// <param name="description"></param>
    public void AttachOptionTrade(WrappedMoney cost, string description)
    {
        if (!OptionAttached)
        {
            GrossProceed = GrossProceed with
            {
                Amount = GrossProceed.Amount + cost.Convert(1 / GrossProceed.FxRate, GrossProceed.Amount.Currency),
                Description = description
            };
            OptionAttached = true;
        }
    }
    public virtual WrappedMoney NetProceed
    {
        get
        {
            if (Expenses.IsEmpty) return GrossProceed.BaseCurrencyAmount;
            if (AcquisitionDisposal == TradeType.ACQUISITION) return GrossProceed.BaseCurrencyAmount + Expenses.Select(i => i.BaseCurrencyAmount).Sum();
            else return GrossProceed.BaseCurrencyAmount - Expenses.Select(i => i.BaseCurrencyAmount).Sum();
        }
    }
    /// <summary>
    /// Quantity but positive for an acquisition but negative for a disposal.
    /// </summary>
    public decimal RawQuantity => AcquisitionDisposal switch
    {
        TradeType.ACQUISITION => Quantity,
        TradeType.DISPOSAL => Quantity * -1,
        _ => throw new NotImplementedException($"Unknown trade type {AcquisitionDisposal}"),
    };

    protected string GetExpensesExplanation()
    {
        if (Expenses.IsEmpty) return string.Empty;
        StringBuilder stringBuilder = new();
        stringBuilder.Append("\n\tExpenses: ");
        foreach (var expense in Expenses)
        {
            stringBuilder.Append(expense.PrintToTextFile() + "\t");
        }
        return stringBuilder.ToString();
    }

    public virtual string PrintToTextFile()
    {
        string action = AcquisitionDisposal switch
        {
            TradeType.ACQUISITION => "Bought",
            TradeType.DISPOSAL => "Sold",
            _ => throw new NotImplementedException()
        };
        string netExplanation = AcquisitionDisposal switch
        {
            TradeType.ACQUISITION => $"Total cost: {NetProceed}",
            TradeType.DISPOSAL => $"Net proceed: {NetProceed}",
            _ => throw new NotImplementedException()
        };
        return $"{action} {Quantity} unit(s) of {AssetName} on {Date:dd-MMM-yyyy HH:mm} for {GrossProceed.PrintToTextFile()} " +
            $"with total expense {Expenses.Sum(expenses => expenses.BaseCurrencyAmount)}, {netExplanation}"
            + GetExpensesExplanation();
    }
}

