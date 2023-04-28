using System;

namespace CapitalGainCalculator.Model.Interfaces;

public interface ITaxYear
{
    public int ToTaxYear(DateTime dateTime);
}