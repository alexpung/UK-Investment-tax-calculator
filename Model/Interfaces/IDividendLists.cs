using System.Collections.Generic;

namespace CapitalGainCalculator.Model.Interfaces;
public interface IDividendLists
{
    List<Dividend> Dividends { get; set; }
}