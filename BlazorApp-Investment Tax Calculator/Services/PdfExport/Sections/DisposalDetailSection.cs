using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel.Futures;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

namespace InvestmentTaxCalculator.Services.PdfExport.Sections;

public class DisposalDetailSection(TradeCalculationResult tradeCalculationResult) : ISection
{
    public string Name { get; set; } = "Trade disposals tax calculation";
    public string Title { get; set; } = "Trade Disposals Tax Calculation";

    public Section WriteSection(Section section, int taxYear)
    {
        var disposals = tradeCalculationResult.DisposalByYear
                        .Where(kvp => kvp.Key.Item1 == taxYear)
                        .SelectMany(kvp => kvp.Value);
        Paragraph paragraph = section.AddParagraph(Title);
        Style.StyleTitle(paragraph);

        if (!disposals.Any())
        {
            section.AddParagraph($"No disposals found for the tax year {taxYear}.");
            return section;
        }
        foreach (var disposal in disposals)
        {
            AddDisposalDetails(section, disposal);
            AddDisposalCalculationDetailDefault(section, disposal);
            section.AddParagraph().Format.SpaceAfter = Unit.FromPoint(10);
        }
        return section;
    }

    private static void AddDisposalDetails(Section section, ITradeTaxCalculation disposal)
    {
        Table table = Style.CreateTableWithProportionedWidth(section,
            [(7, ParagraphAlignment.Center),
            (25, ParagraphAlignment.Left),
            (10, ParagraphAlignment.Left),
            (10, ParagraphAlignment.Center),
            (15, ParagraphAlignment.Right),
            (15, ParagraphAlignment.Right),
            (15, ParagraphAlignment.Right)]);
        Row headerRow = table.AddRow();
        headerRow.Cells[0].AddParagraph("Trade ID");
        headerRow.Cells[1].AddParagraph("Asset Name");
        headerRow.Cells[2].AddParagraph("Asset Category");
        headerRow.Cells[3].AddParagraph("Disposal Date");
        headerRow.Cells[4].AddParagraph("Disposal Quantity");
        if (disposal is not FutureTradeTaxCalculation)
            headerRow.Cells[5].AddParagraph("Net Disposal Proceed");
        headerRow.Cells[6].AddParagraph("Gain (Loss)");
        Style.StyleHeaderRow(headerRow);

        Row dataRow = table.AddRow();
        dataRow.Cells[0].AddParagraph(disposal.Id.ToString());
        dataRow.Cells[1].AddParagraph(disposal.AssetName);
        dataRow.Cells[2].AddParagraph(disposal.AssetCategoryType.GetDescription());
        dataRow.Cells[3].AddParagraph(disposal.Date.ToShortDateString());
        dataRow.Cells[4].AddParagraph(disposal.TotalQty.ToString("F2"));
        if (disposal is not FutureTradeTaxCalculation)
            dataRow.Cells[5].AddParagraph(disposal.TotalCostOrProceed.ToString());
        dataRow.Cells[6].AddParagraph(disposal.Gain.ToString());
    }

    private static void AddDisposalCalculationDetailDefault(Section section, ITradeTaxCalculation disposal)
    {
        Table table = Style.CreateTableWithProportionedWidth(section,
            [(20, ParagraphAlignment.Left),
            (10, ParagraphAlignment.Right),
            (10, ParagraphAlignment.Right),
            (10, ParagraphAlignment.Right),
            (10, ParagraphAlignment.Right)]);
        Row tradeDetailHeaderRow = table.AddRow();
        Paragraph calcTitle = tradeDetailHeaderRow.Cells[0].AddParagraph("Calculation");
        calcTitle.Format.Font.Color = Colors.DarkBlue;
        tradeDetailHeaderRow.Cells[0].MergeRight = 4;
        tradeDetailHeaderRow.Cells[0].Format.Alignment = ParagraphAlignment.Center;
        tradeDetailHeaderRow.Cells[0].VerticalAlignment = VerticalAlignment.Center;
        Row tradeDetailHeaderRow2 = table.AddRow();
        tradeDetailHeaderRow2.Cells[0].AddParagraph("Trade Detail");
        tradeDetailHeaderRow2.Cells[1].MergeRight = 1;
        tradeDetailHeaderRow2.Cells[1].AddParagraph($"SubTotals");
        tradeDetailHeaderRow2.Cells[1].Format.Alignment = ParagraphAlignment.Center;
        tradeDetailHeaderRow2.Cells[3].MergeRight = 1;
        tradeDetailHeaderRow2.Cells[3].AddParagraph($"Totals");
        tradeDetailHeaderRow2.Cells[3].Format.Alignment = ParagraphAlignment.Center;
        Row tradeDetailHeaderRow3 = table.AddRow();
        tradeDetailHeaderRow3.Cells[1].AddParagraph("Original Amount");
        tradeDetailHeaderRow3.Cells[2].AddParagraph($"Sterling Amount");
        tradeDetailHeaderRow3.Cells[3].AddParagraph("Original Amount");
        tradeDetailHeaderRow3.Cells[4].AddParagraph($"Sterling Amount");
        Style.StyleHeaderRow(tradeDetailHeaderRow);
        Style.StyleHeaderRow(tradeDetailHeaderRow2);
        Style.StyleHeaderRow(tradeDetailHeaderRow3);
        if (disposal is FutureTradeTaxCalculation)
            ShowCalculationFutureContract(table, disposal);
        else
            ShowCalculationNormal(table, disposal);
    }

    private static string GetMatchQuantityDescription(TradeMatch match)
    {
        if (match.MatchAcquisitionQty == match.MatchDisposalQty)
        {
            return $"Matched {match.MatchAcquisitionQty:F2} unit(s)";
        }
        else
        {
            return $"Matched {match.MatchAcquisitionQty:F2} acquisition with {match.MatchDisposalQty:F2} disposal";
        }
    }

    private static void AddMatchingDetail(Table table, TradeMatch match)
    {
        if (match.TradeMatchType == TaxMatchType.SECTION_104)
        {
            Row AcquisitionDescriptionRow = table.AddRow();
            AcquisitionDescriptionRow.Cells[0].AddParagraph($"Section 104 pool quantity {match.Section104HistorySnapshot!.OldQuantity:F2}");
            AcquisitionDescriptionRow.Cells[0].MergeRight = 1;
            AcquisitionDescriptionRow.Cells[2].AddParagraph(match.Section104HistorySnapshot!.OldValue.ToString());
            Row AcquisitionCostRow = table.AddRow();
            AcquisitionCostRow.Cells[0].MergeRight = 2;
            AcquisitionCostRow.Cells[0].AddParagraph($"Acquistion value of the matching: {match.Section104HistorySnapshot!.OldValue} * " +
                $"{match.MatchAcquisitionQty:F2} / {match.Section104HistorySnapshot!.OldQuantity:F2}");
            AcquisitionCostRow.Cells[4].AddParagraph(ShowAcquisionCost(match));
        }
        else
        {
            foreach (var trade in match.MatchedBuyTrade!.TradeList)
            {
                Row AcquisitionDescriptionRow = table.AddRow();
                AcquisitionDescriptionRow.Cells[0].AddParagraph($"Acquire {trade.Quantity:F2} unit(s) on {trade.Date:dd-MMM-yyyy HH:mm}");
                AcquisitionDescriptionRow.Cells[0].MergeRight = 2;
                Row GrossProceedRow = table.AddRow();
                GrossProceedRow.Cells[0].AddParagraph("Acquisition cost");
                GrossProceedRow.Cells[1].AddParagraph(trade.GrossProceed.Amount.ToString());
                GrossProceedRow.Cells[2].AddParagraph(trade.GrossProceed.BaseCurrencyAmount.ToString());
                foreach (var expense in trade.Expenses)
                {
                    Row ExpensesRow = table.AddRow();
                    ExpensesRow.Cells[0].AddParagraph(expense.Description);
                    ExpensesRow.Cells[1].AddParagraph(expense.Amount.ToString());
                    ExpensesRow.Cells[2].AddParagraph(expense.BaseCurrencyAmount.ToString());
                }
            }
            Row totalCostRow = table.AddRow();
            totalCostRow.Cells[0].AddParagraph("Total cost");
            totalCostRow.Cells[2].AddParagraph(match.MatchedBuyTrade.TotalCostOrProceed.ToString());
            Row MatchedPortionTotalCostRow = table.AddRow();
            MatchedPortionTotalCostRow.Cells[0].AddParagraph($"Total cost for the matched portion = " +
                $"{match.MatchedBuyTrade.TotalCostOrProceed} * {match.MatchAcquisitionQty:F2} / {match.MatchedBuyTrade.TotalQty:F2}");
            MatchedPortionTotalCostRow.Cells[0].MergeRight = 2;
            MatchedPortionTotalCostRow.Cells[4].AddParagraph(ShowAcquisionCost(match));
        }
        if (match is FutureTradeMatch futureTradeMatch)
        {
            Row PnLCalcRow = table.AddRow();
            PnLCalcRow.Cells[0].AddParagraph($"Future contract PnL calculation {futureTradeMatch.MatchSellContractValue} - {futureTradeMatch.MatchBuyContractValue}");
            PnLCalcRow.Cells[0].MergeRight = 2;
            PnLCalcRow.Cells[3].AddParagraph((futureTradeMatch.MatchSellContractValue - futureTradeMatch.MatchBuyContractValue).ToString());
            PnLCalcRow.Cells[4].AddParagraph(futureTradeMatch.BaseCurrencyContractValueGain.ToString());
        }
        if (!string.IsNullOrEmpty(match.AdditionalInformation))
        {
            Row additionalInfoRow = table.AddRow();
            additionalInfoRow.Cells[0].AddParagraph($"Note: {match.AdditionalInformation}");
            additionalInfoRow.Cells[0].MergeRight = 4;
        }
    }

    private static string ShowAcquisionCost(TradeMatch match)
    {
        if (match is FutureTradeMatch futureTradeMatch)
        {
            return (futureTradeMatch.BaseCurrencyAcquisitionDealingCost * -1).ToString();
        }
        else
        {
            return (match.BaseCurrencyMatchAllowableCost * -1).ToString();
        }
    }

    private static void ShowNetProceedCalculation(Table table, Trade trade)
    {
        Row GrossProceedRow = table.AddRow();
        GrossProceedRow.Cells[0].AddParagraph("Gross Disposal Proceeds");
        GrossProceedRow.Cells[0].MergeRight = 2;
        GrossProceedRow.Cells[3].AddParagraph(trade.GrossProceed.Amount.ToString());
        GrossProceedRow.Cells[4].AddParagraph(trade.GrossProceed.BaseCurrencyAmount.ToString());
        foreach (var expense in trade.Expenses)
        {
            Row ExpensesRow = table.AddRow();
            ExpensesRow.Cells[0].AddParagraph(expense.Description);
            ExpensesRow.Cells[3].AddParagraph((expense.Amount * -1).ToString());
            ExpensesRow.Cells[4].AddParagraph((expense.BaseCurrencyAmount * -1).ToString());

        }
    }

    private static void ShowCalculationNormal(Table table, ITradeTaxCalculation disposal)
    {
        foreach (var trade in disposal.TradeList)
        {
            Cell mergeCell = table.AddRow().Cells[0];
            mergeCell.MergeRight = 2;
            mergeCell.AddParagraph($"Dispose {trade.Quantity:F2} unit(s) on {trade.Date:dd-MMM-yyyy HH:mm}");
            ShowNetProceedCalculation(table, trade);
        }
        Row totalRow = table.AddRow();
        Style.StyleSumRow(totalRow);
        totalRow.Cells[0].AddParagraph("Net Proceed");
        totalRow.Cells[0].MergeRight = 2;
        totalRow.Cells[4].AddParagraph(disposal.TotalCostOrProceed.ToString());
        if (!disposal.CalculationCompleted)
        {
            Row row = table.AddRow();
            row.Cells[0].MergeRight = 2;
            row.Cells[0].AddParagraph($"The trade is not completely matched, {disposal.UnmatchedQty} remaining");
            Row matchedProceedRow = table.AddRow();
            matchedProceedRow.Cells[0].AddParagraph("Net Proceed of matched portion");
            matchedProceedRow.Cells[0].MergeRight = 2;
            matchedProceedRow.Cells[4].AddParagraph(disposal.TotalProceeds.ToString());
        }
        foreach (var match in disposal.MatchHistory)
        {
            Row row = table.AddRow();
            row.Format.Borders.Top.Width = 1;
            row.Cells[0].AddParagraph($"{match.TradeMatchType.GetDescription()}: " + GetMatchQuantityDescription(match));
            row.Cells[0].MergeRight = 2;
            AddMatchingDetail(table, match);
        }
        totalRow = table.AddRow();
        Style.StyleSumRow(totalRow);
        totalRow.Cells[0].AddParagraph("Net Gain (Loss)");
        totalRow.Cells[4].AddParagraph(disposal.Gain.ToString());
    }

    private static void ShowCalculationFutureContract(Table table, ITradeTaxCalculation disposal)
    {
        Cell closingValueCell = table.AddRow().Cells[0];
        closingValueCell.MergeRight = 2;
        FutureTradeTaxCalculation futureTradeTaxCalculation = (FutureTradeTaxCalculation)disposal;
        closingValueCell.AddParagraph($"Closing Contract Value: {futureTradeTaxCalculation.TotalContractValue}");
        if (!disposal.CalculationCompleted)
        {
            Row row = table.AddRow();
            row.Cells[0].MergeRight = 2;
            row.Cells[0].AddParagraph($"The trade is not completely matched, {futureTradeTaxCalculation.UnmatchedQty} remaining");
            Row matchedProceedRow = table.AddRow();
            matchedProceedRow.Cells[0].AddParagraph($"Contract value of matched portion:");
            matchedProceedRow.Cells[0].MergeRight = 2;
            matchedProceedRow.Cells[4].AddParagraph((futureTradeTaxCalculation.TotalContractValue - futureTradeTaxCalculation.UnmatchedContractValue).ToString());
        }
        foreach (var match in disposal.MatchHistory)
        {
            Row row = table.AddRow();
            row.Format.Borders.Top.Width = 1;
            row.Cells[0].AddParagraph($"{match.TradeMatchType.GetDescription()}: " + GetMatchQuantityDescription(match));
            row.Cells[0].MergeRight = 2;
            AddMatchingDetail(table, match);
        }
        Row totalRow = table.AddRow();
        Style.StyleSumRow(totalRow);
        totalRow.Cells[0].AddParagraph("Net Gain (Loss)");
        totalRow.Cells[4].AddParagraph(disposal.Gain.ToString());
    }
}
