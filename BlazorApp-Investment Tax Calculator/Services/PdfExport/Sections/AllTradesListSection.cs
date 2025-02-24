using InvestmentTaxCalculator.Enumerations;
using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Model.Interfaces;
using InvestmentTaxCalculator.Model.TaxEvents;
using InvestmentTaxCalculator.Model.UkTaxModel.Futures;

using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

namespace InvestmentTaxCalculator.Services.PdfExport.Sections;

public class AllTradesListSection(TradeCalculationResult tradeCalculationResult) : ISection
{
    public string Name { get; set; } = "List of all trades";
    public string Title { get; set; } = "List of all trades";

    public Section WriteSection(Section section, int taxYear)
    {
        Paragraph paragraph = section.AddParagraph(Title);
        Style.StyleTitle(paragraph);
        IEnumerable<IGrouping<AssetCategoryType, ITradeTaxCalculation>> tradeTaxCalculations = tradeCalculationResult.CalculatedTrade.GroupBy(trade => trade.AssetCategoryType);
        foreach (var grouping in tradeTaxCalculations)
        {
            Paragraph tableSubheading = section.AddParagraph(grouping.Key.GetDescription());
            tableSubheading.Format.Font.Color = Colors.DarkBlue;
            tableSubheading.Format.Font.Size = 14;
            tableSubheading.Format.SpaceAfter = Unit.FromPoint(10);
            Table table = section.AddTable();
            WriteTradeTable(table, grouping, taxYear, grouping.Key);
        }
        return section;
    }

    private void WriteTradeTable(Table table, IEnumerable<ITradeTaxCalculation> tradeTaxCalculations, int taxYear, AssetCategoryType assetCategoryType)
    {
        table.AddColumn(Unit.FromPoint(35)).Format.Alignment = ParagraphAlignment.Left;
        table.AddColumn(Unit.FromPoint(70)).Format.Alignment = ParagraphAlignment.Left;
        table.AddColumn(Unit.FromPoint(70)).Format.Alignment = ParagraphAlignment.Left;
        table.AddColumn(Unit.FromPoint(70)).Format.Alignment = ParagraphAlignment.Left;
        table.AddColumn(Unit.FromPoint(70)).Format.Alignment = ParagraphAlignment.Left;
        table.AddColumn(Unit.FromPoint(40)).Format.Alignment = ParagraphAlignment.Right;
        table.AddColumn(Unit.FromPoint(70)).Format.Alignment = ParagraphAlignment.Right;
        if (assetCategoryType == AssetCategoryType.FUTURE)
        {
            table.AddColumn(Unit.FromPoint(70)).Format.Alignment = ParagraphAlignment.Right;
        }
        Row headerRow = table.AddRow();
        headerRow.Cells[0].AddParagraph("ID");
        headerRow.Cells[1].AddParagraph("Date/Time");
        headerRow.Cells[2].AddParagraph("Asset Ticker/Name");
        headerRow.Cells[3].AddParagraph("Asset Type");
        headerRow.Cells[4].AddParagraph("Acq/Dis");
        headerRow.Cells[5].AddParagraph("Quantity");
        headerRow.Cells[6].AddParagraph("Total Cost/Proceeds");
        if (assetCategoryType == AssetCategoryType.FUTURE)
        {
            headerRow.Cells[7].AddParagraph("Contract Value");
        }
        Style.StyleHeaderRow(headerRow);
        foreach (var trade in tradeTaxCalculations
            .Where(trade => tradeCalculationResult.IsTradeInSelectedTaxYear([taxYear], trade))
            .OrderBy(trade => trade.Date))
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
        table.Format.SpaceAfter = Unit.FromPoint(20);
    }
}
