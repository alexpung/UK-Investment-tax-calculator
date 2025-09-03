namespace InvestmentTaxCalculator.Model.UkTaxModel;

public class UkCapitalGainAllowance
{
    public Dictionary<int, decimal> Allowances { get; } = new()
    {
        { 2015, 11100m },
        { 2016, 11100m },
        { 2017, 11300m },
        { 2018, 11700m },
        { 2019, 12000m },
        { 2020, 12300m },
        { 2021, 12300m },
        { 2022, 12300m },
        { 2023, 6000m },
        { 2024, 3000m }
    };

    public WrappedMoney GetTaxAllowance(int year)
    {
        if (Allowances.TryGetValue(year, out decimal allowance))
        {
            return new WrappedMoney(allowance, "GBP");
        }
        else
        {
            return new WrappedMoney(3000, "GBP");
        }
    }
}
