using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel.Futures;
using InvestmentTaxCalculator.Model.UkTaxModel.Stocks;

using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

using System.Text;

namespace InvestmentTaxCalculator.Services.PdfExport.Sections;

public class DisposalDetailSection(TradeCalculationResult tradeCalculationResult) : ISection
{
    public static bool OnlyShowTaxableTrades { get; set; } = true;
    public string Name { get; set; } = "Trade disposals tax calculation";
    public string Title { get; set; } = "Trade Disposals Tax Calculation";

    public Section WriteSection(Section section, int taxYear)
    {
        IEnumerable<ITradeTaxCalculation> disposals;
        if (OnlyShowTaxableTrades)
        {
            disposals = tradeCalculationResult.DisposalByYear
                        .Where(kvp => kvp.Key.Item1 == taxYear)
                        .SelectMany(kvp => kvp.Value);
        }
        else
        {
            disposals = tradeCalculationResult.DisposalByYearIncludeNonTaxable
                        .Where(kvp => kvp.Key.Item1 == taxYear)
                        .SelectMany(kvp => kvp.Value);
        }
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
            section.AddParagraph().Format.SpaceAfter = Unit.FromPoint(10);
            if (disposal is FutureTradeTaxCalculation futureDisposal)
            {
                AddDisposalCalculationFutureContract(section, futureDisposal);
            }
            else if (disposal is CorporateActionTaxCalculation corporateActionDisposal)
            {
                AddDisposalCalculationCorporateAction(section, corporateActionDisposal);
            }
            else
            {
                AddDisposalCalculationDetailDefault(section, disposal);
            }
            section.AddParagraph().Format.SpaceAfter = Unit.FromPoint(10);
            ShowCalculationNormal(section, disposal);
            section.AddParagraph().Format.SpaceAfter = Unit.FromPoint(10);
            if (disposal.MatchHistory.Exists(match => match.TradeMatchType == TaxMatchType.SECTION_104))
            {
                var section104Match = disposal.MatchHistory.First(match => match.TradeMatchType == TaxMatchType.SECTION_104);
                if (section104Match is FutureTradeMatch futureTradeMatch)
                {
                    ShowSection104SnapshotFutureContract(section, futureTradeMatch);
                }
                else
                {
                    ShowSection104SnapshotNormal(section, section104Match);
                }

            }
            if (disposal is not CorporateActionTaxCalculation && disposal.MatchHistory.Exists(match => match.TradeMatchType != TaxMatchType.SECTION_104))
            {
                AddAcquisitionTradeDetails(section, disposal);
            }
            section.AddPageBreak();
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
            (15, ParagraphAlignment.Center),
            (15, ParagraphAlignment.Right),
            (15, ParagraphAlignment.Right),
            (15, ParagraphAlignment.Right)]);
        Row headerRow = table.AddRow();
        headerRow.Cells[0].AddParagraph("Trade ID");
        headerRow.Cells[1].AddParagraph("Asset Name");
        headerRow.Cells[2].AddParagraph("Asset Category");
        headerRow.Cells[3].AddParagraph("Disposal Date");
        headerRow.Cells[4].AddParagraph("Residency Status");
        headerRow.Cells[5].AddParagraph("Disposal Quantity");
        if (disposal is not FutureTradeTaxCalculation)
            headerRow.Cells[6].AddParagraph("Net Disposal Proceed");
        headerRow.Cells[7].AddParagraph("Taxable Gain (Loss)");
        Style.StyleHeaderRow(headerRow);

        Row dataRow = table.AddRow();
        dataRow.Cells[0].AddParagraph(disposal.Id.ToString());
        dataRow.Cells[1].AddParagraph(disposal.AssetName);
        dataRow.Cells[2].AddParagraph(disposal.AssetCategoryType.GetDescription());
        dataRow.Cells[3].AddParagraph(disposal.Date.ToShortDateString());
        dataRow.Cells[4].AddParagraph(disposal.ResidencyStatusAtTrade.GetDescription());
        dataRow.Cells[5].AddParagraph(disposal.TotalQty.ToString("F2"));
        if (disposal is not FutureTradeTaxCalculation)
            dataRow.Cells[6].AddParagraph(disposal.TotalCostOrProceed.ToString());
        dataRow.Cells[7].AddParagraph(disposal.Gain.ToString());
    }

    private static void AddDisposalCalculationDetailDefault(Section section, ITradeTaxCalculation disposal)
    {
        Table table = Style.CreateTableWithProportionedWidth(section,
            [(10, ParagraphAlignment.Left),
            (10, ParagraphAlignment.Right),
            (10, ParagraphAlignment.Right),
            (10, ParagraphAlignment.Right),
            (10, ParagraphAlignment.Right)]);
        Row tradeDetailHeaderRow = table.AddRow();
        Paragraph calcTitle = tradeDetailHeaderRow.Cells[0].AddParagraph("Calculation");
        tradeDetailHeaderRow.Format.Alignment = ParagraphAlignment.Center;
        tradeDetailHeaderRow.Cells[0].MergeRight = 4;
        calcTitle.Format.Font.Color = Colors.DarkBlue;
        Row tradeDetailHeaderRow2 = table.AddRow();
        tradeDetailHeaderRow2.Cells[0].AddParagraph("Trade Details");
        tradeDetailHeaderRow2.Cells[1].AddParagraph("Disposal Date");
        tradeDetailHeaderRow2.Cells[2].AddParagraph($"Quantity");
        tradeDetailHeaderRow2.Cells[3].AddParagraph("Original Amount");
        tradeDetailHeaderRow2.Cells[4].AddParagraph($"Sterling Amount");
        Style.StyleHeaderRow(tradeDetailHeaderRow);
        Style.StyleHeaderRow(tradeDetailHeaderRow2);
        var disposalNum = 1;
        foreach (var trade in disposal.TradeList)
        {
            Row disposalDateAndQuanityRow = table.AddRow();
            disposalDateAndQuanityRow.Cells[0].AddParagraph($"Disposal {disposalNum++}:");
            disposalDateAndQuanityRow.Cells[1].AddParagraph(trade.Date.ToString("dd-MMM-yyyy HH:mm"));
            disposalDateAndQuanityRow.Cells[2].AddParagraph(trade.Quantity.ToString("F2"));
            disposalDateAndQuanityRow.Cells[3].AddParagraph(trade.GrossProceed.Amount.ToString());
            disposalDateAndQuanityRow.Cells[4].AddParagraph(trade.GrossProceed.BaseCurrencyAmount.ToString());
            foreach (var expense in trade.Expenses)
            {
                Row expenseRow = table.AddRow();
                expenseRow.Cells[0].AddParagraph(expense.Description);
                expenseRow.Cells[3].AddParagraph(expense.Amount.ToString());
                expenseRow.Cells[4].AddParagraph(expense.BaseCurrencyAmount.ToString());
            }
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
    }

    private static void AddDisposalCalculationFutureContract(Section section, FutureTradeTaxCalculation disposal)
    {
        Table table = Style.CreateTableWithProportionedWidth(section,
            [(10, ParagraphAlignment.Left),
            (10, ParagraphAlignment.Right),
            (10, ParagraphAlignment.Right),
            (10, ParagraphAlignment.Right),
            (10, ParagraphAlignment.Right)]);
        Row tradeDetailHeaderRow = table.AddRow();
        Paragraph calcTitle = tradeDetailHeaderRow.Cells[0].AddParagraph("Calculation");
        tradeDetailHeaderRow.Format.Alignment = ParagraphAlignment.Center;
        tradeDetailHeaderRow.Cells[0].MergeRight = 4;
        calcTitle.Format.Font.Color = Colors.DarkBlue;
        Row tradeDetailHeaderRow2 = table.AddRow();
        tradeDetailHeaderRow2.Cells[0].AddParagraph("Trade Details");
        tradeDetailHeaderRow2.Cells[1].AddParagraph("Disposal Date");
        tradeDetailHeaderRow2.Cells[2].AddParagraph($"Quantity");
        tradeDetailHeaderRow2.Cells[3].AddParagraph("Transaction Cost");
        tradeDetailHeaderRow2.Cells[4].AddParagraph($"Contract Value");
        Style.StyleHeaderRow(tradeDetailHeaderRow);
        Style.StyleHeaderRow(tradeDetailHeaderRow2);
        var disposalNum = 1;
        foreach (FutureContractTrade trade in disposal.TradeList.Cast<FutureContractTrade>())
        {
            Row disposalDateAndQuanityRow = table.AddRow();
            disposalDateAndQuanityRow.Cells[0].AddParagraph($"Disposal {disposalNum++}:");
            disposalDateAndQuanityRow.Cells[1].AddParagraph(trade.Date.ToString("dd-MMM-yyyy HH:mm"));
            disposalDateAndQuanityRow.Cells[2].AddParagraph(trade.Quantity.ToString("F2"));
            disposalDateAndQuanityRow.Cells[4].AddParagraph(trade.ContractValue.Amount.ToString());
            foreach (var expense in trade.Expenses)
            {
                Row expenseRow = table.AddRow();
                expenseRow.Cells[0].AddParagraph(expense.Description);
                expenseRow.Cells[3].AddParagraph(expense.Display());
            }
        }
        Row totalRow = table.AddRow();
        Style.StyleSumRow(totalRow);
        totalRow.Cells[0].AddParagraph("Totals");
        totalRow.Cells[3].AddParagraph(disposal.TotalCostOrProceed.ToString());
        totalRow.Cells[4].AddParagraph(disposal.TotalContractValue.ToString());
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
    }

    private static void AddDisposalCalculationCorporateAction(Section section, CorporateActionTaxCalculation disposal)
    {
        Table table = Style.CreateTableWithProportionedWidth(section,
            [(15, ParagraphAlignment.Left),
            (35, ParagraphAlignment.Left)]);

        Row titleRow = table.AddRow();
        titleRow.Cells[0].MergeRight = 1;
        titleRow.Cells[0].AddParagraph("Corporate Action Details");
        titleRow.Format.Alignment = ParagraphAlignment.Center;
        titleRow.Cells[0].Format.Font.Bold = true;
        titleRow.Cells[0].Format.Font.Color = Colors.DarkBlue;
        Style.StyleHeaderRow(titleRow);

        Row reasonRow = table.AddRow();
        reasonRow.Cells[0].AddParagraph("Description");
        reasonRow.Cells[1].AddParagraph(disposal.RelatedCorporateAction.Reason);

        Row dateRow = table.AddRow();
        dateRow.Cells[0].AddParagraph("Date");
        dateRow.Cells[1].AddParagraph(disposal.Date.ToShortDateString());

        Row proceedRow = table.AddRow();
        proceedRow.Cells[0].AddParagraph("Proceeds");
        proceedRow.Cells[1].AddParagraph(disposal.TotalCostOrProceed.ToString());

        if (disposal.TotalAllowableCost.Amount > 0)
        {
            Row costRow = table.AddRow();
            costRow.Cells[0].AddParagraph("Allowable Cost");
            costRow.Cells[1].AddParagraph(disposal.TotalAllowableCost.ToString());
        }

        Row gainRow = table.AddRow();
        Style.StyleSumRow(gainRow);
        gainRow.Cells[0].AddParagraph("Gain (Loss)");
        gainRow.Cells[1].AddParagraph(disposal.Gain.ToString());
    }

    private static string GetMatchQuantityDescription(TradeMatch match)
    {
        if (match.MatchAcquisitionQty == match.MatchDisposalQty)
        {
            return $"{match.MatchAcquisitionQty:F2}";
        }
        else
        {
            return $"{match.MatchAcquisitionQty:F2} acquisition / {match.MatchDisposalQty:F2} disposal";
        }
    }

    private static void ShowCalculationNormal(Section section, ITradeTaxCalculation disposal)
    {

        Table table = Style.CreateTableWithProportionedWidth(section,
            [(10, ParagraphAlignment.Center),
            (10, ParagraphAlignment.Center),
            (10, ParagraphAlignment.Center),
            (10, ParagraphAlignment.Center),
            (10, ParagraphAlignment.Right),
            (10, ParagraphAlignment.Right),
            (10, ParagraphAlignment.Right)]);
        Row matchHeaderRow = table.AddRow();
        matchHeaderRow.Cells[0].AddParagraph("Match Type");
        matchHeaderRow.Cells[1].AddParagraph("Taxable?");
        matchHeaderRow.Cells[2].AddParagraph("Quantity");
        matchHeaderRow.Cells[3].AddParagraph("Acquisition Date");
        matchHeaderRow.Cells[4].AddParagraph("Disposal Proceeed");
        if (disposal is FutureTradeTaxCalculation)
            matchHeaderRow.Cells[5].AddParagraph("Allowable Cost");
        else
            matchHeaderRow.Cells[5].AddParagraph("Acquisition Cost");
        matchHeaderRow.Cells[6].AddParagraph("Gain (Loss)");
        Style.StyleHeaderRow(matchHeaderRow);
        foreach (var match in disposal.MatchHistory)
        {
            Row row = table.AddRow();
            row.Cells[0].AddParagraph($"{match.TradeMatchType.GetDescription()}");
            row.Cells[1].AddParagraph(match.IsTaxable.GetDescription());
            row.Cells[2].AddParagraph(GetMatchQuantityDescription(match));
            row.Cells[3].AddParagraph(match.MatchedBuyTrade?.Date.ToShortDateString() ?? string.Empty);
            row.Cells[4].AddParagraph(match.BaseCurrencyMatchDisposalProceed.ToString());
            row.Cells[5].AddParagraph(match.BaseCurrencyMatchAllowableCost.ToString());
            row.Cells[6].AddParagraph(match.MatchGain.ToString());
            if (!string.IsNullOrEmpty(match.AdditionalInformation))
            {
                Row additionalInfoRow = table.AddRow();
                additionalInfoRow.Cells[0].AddParagraph($"Note: {match.AdditionalInformation}");
                additionalInfoRow.Cells[0].MergeRight = 6;
            }
            if (match is FutureTradeMatch futureMatch)
            {
                Row paymentInfoRow = table.AddRow();
                paymentInfoRow.Cells[0].AddParagraph(futureMatch.ShowPaymentForContractGainOrLoss(futureMatch.BaseCurrencyContractValueGain));
                paymentInfoRow.Cells[0].MergeRight = 6;
            }
        }
        Row totalRow = table.AddRow();
        Style.StyleSumRow(totalRow);
        var totalgainLabel = totalRow.Cells[0].AddParagraph("Total Taxable Gain (Loss)");
        totalgainLabel.Format.Alignment = ParagraphAlignment.Left;
        totalRow.Cells[0].MergeRight = 2;
        totalRow.Cells[6].AddParagraph(disposal.Gain.ToString());
    }

    private static void ShowSection104SnapshotFutureContract(Section section, TradeMatch match)
    {
        Table table = Style.CreateTableWithProportionedWidth(section,
            [(10, ParagraphAlignment.Left),
            (10, ParagraphAlignment.Right),
            (10, ParagraphAlignment.Right),
            (10, ParagraphAlignment.Right)]);
        Row titleRow = table.AddRow();
        titleRow.Cells[0].MergeRight = 3;
        titleRow.Format.Alignment = ParagraphAlignment.Center;
        Style.StyleHeaderRow(titleRow);
        Paragraph title = titleRow.Cells[0].AddParagraph("Section 104 Pool Change");
        title.Format.Font.Color = Colors.DarkBlue;
        Row headerRow = table.AddRow();
        Style.StyleHeaderRow(headerRow);
        headerRow.Cells[1].AddParagraph("Quantity");
        headerRow.Cells[2].AddParagraph("Dealing Cost");
        headerRow.Cells[3].AddParagraph("Contract value");
        Row oldValueRow = table.AddRow();
        oldValueRow.Cells[0].AddParagraph("Old Value");
        oldValueRow.Cells[1].AddParagraph(match.Section104HistorySnapshot!.OldQuantity.ToString());
        oldValueRow.Cells[2].AddParagraph(match.Section104HistorySnapshot!.OldValue.ToString());
        oldValueRow.Cells[3].AddParagraph(match.Section104HistorySnapshot!.OldContractValue.ToString());
        Row valueChangeRow = table.AddRow();
        valueChangeRow.Cells[0].AddParagraph("Value Change");
        valueChangeRow.Cells[1].AddParagraph(match.Section104HistorySnapshot!.QuantityChange.ToString());
        valueChangeRow.Cells[2].AddParagraph(match.Section104HistorySnapshot!.ValueChange.ToString());
        valueChangeRow.Cells[3].AddParagraph(match.Section104HistorySnapshot!.ContractValueChange.ToString());
        Row newValueRow = table.AddRow();
        Style.StyleSumRow(newValueRow);
        newValueRow.Cells[0].AddParagraph("New Value");
        newValueRow.Cells[1].AddParagraph(match.Section104HistorySnapshot!.NewQuantity.ToString());
        newValueRow.Cells[2].AddParagraph(match.Section104HistorySnapshot!.NewValue.ToString());
        newValueRow.Cells[3].AddParagraph(match.Section104HistorySnapshot!.NewContractValue.ToString());
    }

    private static void ShowSection104SnapshotNormal(Section section, TradeMatch match)
    {
        Table table = Style.CreateTableWithProportionedWidth(section,
            [(10, ParagraphAlignment.Left),
            (10, ParagraphAlignment.Right),
            (10, ParagraphAlignment.Right)]);
        Row titleRow = table.AddRow();
        titleRow.Cells[0].MergeRight = 2;
        titleRow.Format.Alignment = ParagraphAlignment.Center;
        Style.StyleHeaderRow(titleRow);
        Paragraph title = titleRow.Cells[0].AddParagraph("Section 104 Pool Change");
        title.Format.Font.Color = Colors.DarkBlue;
        Row headerRow = table.AddRow();
        Style.StyleHeaderRow(headerRow);
        headerRow.Cells[1].AddParagraph("Quantity");
        headerRow.Cells[2].AddParagraph("Value");
        Row oldValueRow = table.AddRow();
        oldValueRow.Cells[0].AddParagraph("Old Value");
        oldValueRow.Cells[1].AddParagraph(match.Section104HistorySnapshot!.OldQuantity.ToString());
        oldValueRow.Cells[2].AddParagraph(match.Section104HistorySnapshot!.OldValue.ToString());
        Row valueChangeRow = table.AddRow();
        valueChangeRow.Cells[0].AddParagraph("Value Change");
        valueChangeRow.Cells[1].AddParagraph(match.Section104HistorySnapshot!.QuantityChange.ToString());
        valueChangeRow.Cells[2].AddParagraph(match.Section104HistorySnapshot!.ValueChange.ToString());
        Row newValueRow = table.AddRow();
        Style.StyleSumRow(newValueRow);
        newValueRow.Cells[0].AddParagraph("New Value");
        newValueRow.Cells[1].AddParagraph(match.Section104HistorySnapshot!.NewQuantity.ToString());
        newValueRow.Cells[2].AddParagraph(match.Section104HistorySnapshot!.NewValue.ToString());
    }

    private static void AddAcquisitionTradeDetails(Section section, ITradeTaxCalculation disposal)
    {
        Table table = Style.CreateTableWithProportionedWidth(section,
            [(10, ParagraphAlignment.Center),
            (10, ParagraphAlignment.Center),
            (10, ParagraphAlignment.Center),
            (10, ParagraphAlignment.Right),
            (10, ParagraphAlignment.Right)]);
        Row topheaderRow = table.AddRow();
        topheaderRow.Cells[0].AddParagraph("Acquisition Trade Details");
        topheaderRow.Format.Alignment = ParagraphAlignment.Center;
        topheaderRow.Cells[0].MergeRight = 4;
        Style.StyleHeaderRow(topheaderRow);
        Row headerRow = table.AddRow();
        Style.StyleHeaderRow(headerRow);
        headerRow.Cells[0].AddParagraph("Trade ID");
        headerRow.Cells[1].AddParagraph("Acquisition Date");
        headerRow.Cells[2].AddParagraph("Residency Status");
        headerRow.Cells[3].AddParagraph("Quantity");
        headerRow.Cells[4].AddParagraph("Allowable Cost in Sterling");
        var acquisitionMatches = disposal.MatchHistory
                                    .Where(match => match.TradeMatchType != TaxMatchType.SECTION_104);
        foreach (var match in acquisitionMatches)
        {
            var trade = match.MatchedBuyTrade!;
            Row row = table.AddRow();
            row.Cells[0].AddParagraph(trade.Id.ToString());
            row.Cells[1].AddParagraph(trade.Date.ToShortDateString());
            row.Cells[2].AddParagraph(trade.ResidencyStatusAtTrade.GetDescription());
            row.Cells[3].AddParagraph(trade.TotalQty.ToString("F2"));
            row.Cells[4].AddParagraph(trade.TotalCostOrProceed.ToString());
            var totalPurchaseCost = trade.TradeList.Select(t => t.GrossProceed.BaseCurrencyAmount).Sum();
            // 1. Process Purchase Costs
            string purchaseDetails = FormatBreakdown("Purchase costs", trade.TradeList.Select(t => t.GrossProceed.Display()), totalPurchaseCost.ToString());
            StringBuilder sb = new();
            InsertBreakdownText(sb, purchaseDetails);
            // 2. Process Expenses
            var expenseGroup = trade.TradeList.SelectMany(t => t.Expenses).GroupBy(expense => expense.Description);
            foreach (var expenseSubGroup in expenseGroup)
            {
                var totalExpense = expenseSubGroup.Sum(expense => expense.BaseCurrencyAmount);
                var items = expenseSubGroup.Select(e => e.Display());

                string expenseDetails = FormatBreakdown(expenseSubGroup.Key, items, totalExpense.ToString());
                InsertBreakdownText(sb, expenseDetails);
            }

            // 3. Add to Table once
            if (sb.Length > 0)
            {
                Row costBreakdownRow = table.AddRow();
                costBreakdownRow.Cells[0].MergeRight = 4;
                costBreakdownRow.Cells[0].AddParagraph(sb.ToString().TrimEnd()); // Trim trailing newline
            }
            // Show calculation of proportioned cost if partially matched
            if (match.MatchAcquisitionQty != trade.TotalQty)
            {
                Row proportionedCostRow = table.AddRow();
                proportionedCostRow.Cells[0].MergeRight = 4;
                proportionedCostRow.Cells[0].AddParagraph($"Proportioned Cost for matched quantity: {match.MatchAcquisitionQty:F2} * {trade.TotalCostOrProceed} / {trade.TotalQty} = {match.BaseCurrencyMatchAllowableCost}");
            }
        }
    }

    private static string FormatBreakdown(string label, IEnumerable<string> items, string total)
    {
        var itemList = items.ToList();
        if (itemList.Count == 0) return string.Empty;
        if (itemList.Count == 1) return $"{label}: {total}";
        return $"{label}: {string.Join(" + ", itemList)} = {total}";
    }

    private static void InsertBreakdownText(StringBuilder stringBuilder, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        string currentContent = stringBuilder.ToString();
        string lastLine = currentContent.Split('\n').LastOrDefault() ?? "";
        if (lastLine.Length + text.Length > 140 && stringBuilder.Length > 0)
        {
            stringBuilder.Append('\n');
        }
        else if (stringBuilder.Length > 0)
        {
            stringBuilder.Append('\t'); // Separator for items on the same line
        }
        stringBuilder.Append(text);
    }
}
