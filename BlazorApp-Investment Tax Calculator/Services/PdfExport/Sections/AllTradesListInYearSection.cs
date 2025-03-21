using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel.Futures;

using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

namespace InvestmentTaxCalculator.Services.PdfExport.Sections;

public class AllTradesListInYearSection(TradeCalculationResult tradeCalculationResult) : ISection
{
    public string Name { get; set; } = "List of all trades (in year)";
    public string Title { get; set; } = "List of all trades during the tax year";

    public Section WriteSection(Section section, int taxYear)
    {
        Paragraph paragraph = section.AddParagraph(Title);
        Style.StyleTitle(paragraph);
        IEnumerable<IGrouping<AssetCategoryType, ITradeTaxCalculation>> tradeTaxCalculations = tradeCalculationResult.TradeByYear
            .Where(kvp => kvp.Key.Item1 == taxYear)
            .SelectMany(kvp => kvp.Value)
            .GroupBy(trade => trade.AssetCategoryType);
        foreach (var grouping in tradeTaxCalculations)
        {
            Paragraph tableSubheading = section.AddParagraph(grouping.Key.GetDescription());
            Style.StyleTableSubheading(tableSubheading);
            tableSubheading.Format.KeepWithNext = true;
            WriteTradeTable(section, grouping, grouping.Key);
        }
        return section;
    }

    private static void WriteTradeTable(Section section, IEnumerable<ITradeTaxCalculation> tradeTaxCalculations, AssetCategoryType assetCategoryType)
    {
        List<(int, ParagraphAlignment)> columnProportionedWidthAndAlignment = [
            (35, ParagraphAlignment.Left),
            (70, ParagraphAlignment.Left),
            (70, ParagraphAlignment.Left),
            (70, ParagraphAlignment.Left),
            (70, ParagraphAlignment.Left),
            (40, ParagraphAlignment.Right),
            (70, ParagraphAlignment.Right)
        ];
        if (assetCategoryType == AssetCategoryType.FUTURE)
        {
            columnProportionedWidthAndAlignment.Add((70, ParagraphAlignment.Right));
        }
        Table table = Style.CreateTableWithProportionedWidth(section, columnProportionedWidthAndAlignment);
        Row headerRow = table.AddRow();
        headerRow.Cells[0].AddParagraph("ID");
        headerRow.Cells[1].AddParagraph("Date/Time");
        headerRow.Cells[2].AddParagraph("Asset Name");
        headerRow.Cells[3].AddParagraph("Asset Type");
        headerRow.Cells[4].AddParagraph("Acq/Dis");
        headerRow.Cells[5].AddParagraph("Quantity");
        headerRow.Cells[6].AddParagraph("Total Cost/Proceeds");
        if (assetCategoryType == AssetCategoryType.FUTURE)
        {
            headerRow.Cells[7].AddParagraph("Contract Value");
        }
        Style.StyleHeaderRow(headerRow);
        foreach (var trade in tradeTaxCalculations.OrderBy(trade => trade.Date))
        {
            Row row = table.AddRow();
            row.Cells[0].AddParagraph(trade.Id.ToString());
            row.Cells[1].AddParagraph(trade.Date.ToShortDateString());
            row.Cells[2].AddParagraph(trade.AssetName);
            row.Cells[3].AddParagraph(trade.AssetCategoryType.GetDescription());
            row.Cells[4].AddParagraph(trade.AcquisitionDisposal.GetDescription());
            row.Cells[5].AddParagraph(trade.TotalQty.ToString("F2"));
            row.Cells[6].AddParagraph(trade.TotalCostOrProceed.ToString());
            if (trade is FutureTradeTaxCalculation futureTrade)
            {
                row.Cells[7].AddParagraph(futureTrade.TotalContractValue.ToString());
            }
            if (trade.TradeList.Count >= 2)
            {
                Row subrow = table.AddRow();
                subrow.Shading.Color = Colors.LightGray;
                int subId = 1;
                foreach (var subTrade in trade.TradeList)
                {
                    subrow.Cells[0].AddParagraph($"{trade.Id}.{subId}");
                    subrow.Cells[1].AddParagraph(subTrade.Date.ToString());
                    subrow.Cells[2].AddParagraph(subTrade.AssetName);
                    subrow.Cells[3].AddParagraph(subTrade.AssetType.GetDescription());
                    subrow.Cells[4].AddParagraph(subTrade.AcquisitionDisposal.GetDescription());
                    subrow.Cells[5].AddParagraph(subTrade.Quantity.ToString("F2"));
                    subrow.Cells[6].AddParagraph(subTrade.NetProceed.ToString());
                    if (trade is FutureContractTrade futureSubtrade)
                    {
                        subrow.Cells[7].AddParagraph(futureSubtrade.ContractValue.ToString());
                    }
                    subId++;
                }
            }
        }
    }
}
