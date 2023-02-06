using CapitalGainCalculator.Model;
using Castle.Core.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapitalGainCalculator.Parser
{
    public class FileParseController
    {
        private readonly IEnumerable<ITaxEventFileParser> _taxEventFileParsers;
        public FileParseController(IEnumerable<ITaxEventFileParser> taxEventFileParsers)
        {
            _taxEventFileParsers = taxEventFileParsers;
        }
        public IList<TaxEvent> ParseFolder(string folderPath)
        {
            List<TaxEvent> taxEvents = new();
            string[] fileEntries = Directory.GetFiles(folderPath);
            foreach (string fileName in fileEntries)
            {
                foreach (ITaxEventFileParser taxEventFileParser in _taxEventFileParsers)
                {
                    if(taxEventFileParser.CheckFileValidity(fileName))
                    {
                        taxEvents.AddRange(taxEventFileParser.ParseFile(fileName));
                        break;
                    }
                }
            }
            return taxEvents;
        }
    }
}
