using Model.TaxEvents;

namespace Model.Interfaces;
public interface IDividendLists
{
    List<Dividend> Dividends { get; set; }
}