using InvestmentTaxCalculator.Model;
using InvestmentTaxCalculator.Parser;

using Syncfusion.Blazor.Inputs;

namespace InvestmentTaxCalculator.Components;
public partial class ImportFile
{
    private DuplicateWarningModal duplicateModal;

    private async Task LoadFiles(UploadChangeEventArgs args)
    {
        foreach (var file in args.Files)
        {
            try
            {
                TaxEventLists events = await fileParseController.ReadFile(file.File);
                var dividendWithUnknownRegions = events.Dividends.Where(x => x.CompanyLocation == CountryCode.UnknownRegion);
                foreach (var dividend in dividendWithUnknownRegions)
                {
                    toastService.ShowWarning($"Unknown region detected with dividend data with:<br> date: {dividend.Date.Date.ToShortDateString()}<br>" +
                        $"company: {dividend.AssetName}<br>description: {dividend.Proceed.Description}<br> Please check the country for the company manually.");
                }
                var duplicates = taxEventLists.GetDuplicates(events);
                int duplicateCount = duplicates.GetTotalNumberOfEvents();

                if (duplicateCount > 10)
                {
                    toastService.ShowError($"Import rejected. Found {duplicateCount} duplicates, which exceeds the limit of 10.");
                    continue; 
                }
                
                if (duplicateCount > 0)
                {
                    bool? skipDuplicates = await duplicateModal.ShowAsync(duplicates);
                    if (skipDuplicates == null)
                    {
                        continue; // Cancelled
                    }
                    taxEventLists.AddData(events, skipDuplicates.Value);
                }
                else
                {
                    taxEventLists.AddData(events);
                }
            }
            catch (ParseException ex)
            {
                toastService.ShowException(ex);
            }
            catch (Exception ex)
            {
                toastService.ShowException(ex);
            }
        }
        args.Files.Clear();
    }
}