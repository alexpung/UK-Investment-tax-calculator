# UK investment tax calculator

UK tax calculator for Interactive Broker

To help report UK tax for dividend and capital gain.

# What is included

1. (TBD) Blazor WASM application ready to use.

# Current functionality

1. Read Interactive Brokers XML file for dividends (dividend/dividend in lieu/Witholding tax).
2. Read Interactive Brokers XML file for shares buy and sell.
3. Show section 104 holdings.
4. Allow user to specify period to report. In this case trade and dividends of that period will be listed.
5. Dividend and withholding tax list and summary sorted by countries,
8. Capital gain calculations for each disposal.

# To use:

1. Configure flex query from interactive brokers. Following report required. Date format dd-MMM-yy
   1. Cash Transactions: Level of detail: Detail
   2. Corporate actions
   3. Trades: Level of detail: Orders
2. Download the flex query for each year in xml format using web browser.
3. Open the web application.
4. Go to the import sections and select the files, then press the "Upload" button.
5. Press "Start Calculation".
6. Export the results by pressing the buttons at the export file section.

# Design notes:

1. When a share forward or reverse split occurs it is assumed that the decimal shares will be lost (see StockSplit class). Other cases to be implemented as necessary.
2. Share matching rules are implemented according to: https://rppaccounts.co.uk/taxation-of-shares/
   1. acquisitions on the same day as the disposal
   2. acquisitions within 30 days after the day of disposal (except where made by a non-resident). Disposals are identified first with securities acquired earlier within the 30-day period after disposal – the First In First Out basis (FIFO)
   3. any shares acquired before the date of disposal which are included in an expanded ‘s. 104 holding’
   4. if the above identification rules fail to exhaust the shares disposed of, they are to be identified with subsequent acquisitions beyond the 30-day period following the disposal.

Note point #4 when buying back shorted shares #1 (same day rule) and #2 (bed&breakfast) still applies.
