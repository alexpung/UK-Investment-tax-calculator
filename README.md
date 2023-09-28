UK investment tax calculator

UK tax calculator for Interactive Broker

To help report UK tax for dividend and capital gain.

# What is included

1. Blazor WASM application ready to use. NEW: Site is live! https://alexpung.github.io/UK-Investment-tax-calculator/

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

# Privacy concerns:

Your trade data is not uploaded anywhere. They never leave your browser thanks to Blazor WASM framework. The calculation is entirely done in your browser.

# Design notes:

1. When a share forward or reverse split occurs it is assumed that the decimal shares will be lost (see StockSplit class). Other cases to be implemented as necessary.
2. Share matching rules are implemented according to: https://rppaccounts.co.uk/taxation-of-shares/
   1. acquisitions on the same day as the disposal
   2. acquisitions within 30 days after the day of disposal (except where made by a non-resident). Disposals are identified first with securities acquired earlier within the 30-day period after disposal � the First In First Out basis (FIFO)
   3. any shares acquired before the date of disposal which are included in an expanded �s. 104 holding�
   4. if the above identification rules fail to exhaust the shares disposed of, they are to be identified with subsequent acquisitions beyond the 30-day period following the disposal.

Note point #4 when buying back shorted shares #1 (same day rule) and #2 (bed&breakfast) still applies.

# Example
Examples can be found in https://github.com/alexpung/UK-Investment-tax-calculator/tree/master/UnitTest/System%20Test/Interactive%20brokers

### Output:
Dividend Summary

    Tax Year: 2020
    Region: JPN (JP)
    	Total dividends: �555.00
    	Total withholding tax: -�166.50
		
    		Transactions:
    		Asset Name: ABCD, Date: 02/02/2021, Type: Withholding Tax, Amount: -�30,000, FxRate: 0.00555, Sterling Amount: -�166.50, Description: ABC CASH DIVIDEND - JP TAX
    		Asset Name: ABCD, Date: 02/02/2021, Type: Dividend, Amount: �90,000, FxRate: 0.00555, Sterling Amount: �499.50, Description: ABC CASH DIVIDEND (Ordinary Dividend)
    		Asset Name: ABCD, Date: 02/02/2021, Type: Payment In Lieu of a Dividend, Amount: �10,000, FxRate: 0.00555, Sterling Amount: �55.50, Description: ABC CASH DIVIDEND (Ordinary Dividend)

### Trades Calculation
    Summary for tax year 2021:
    Number of disposals: 2
    Total disposal proceeds: �2,693.00
    Total allowable costs: �2,261.00
    Total gains (excluding loss): �432.00
    Total loss: �0.00
    
    Disposal 1: Sold 200 units of ABC on 03-May-2021 for �1,834.73.	Total gain (loss): �5.56
    All units of the disposals are matched with acquitions
    Trade details:
    	Sold 200 unit(s) of ABC on 03-May-2021 for $2,200.00 = �1,870.00 Fx rate = 0.85 with total expense �35.28, Net proceed: �1,834.73
    	Expenses: Commission: $1.50 = �1.28 Fx rate = 0.85	Tax: $40.00 = �34.00 Fx rate = 0.85	
    Trade matching:
    Same day: Matched 50 units of the disposal. Acquition cost is �460.28
    Matched trade: Bought 50 unit(s) of ABC on 03-May-2021 for $510.00 = �433.50 Fx rate = 0.85 with total expense �26.78, Total cost: �460.28
    	Expenses: Commission: $1.50 = �1.28 Fx rate = 0.85	Tax: $30.00 = �25.50 Fx rate = 0.85	
    Gain for this match is �458.68 - �460.28 = -�1.59
    
    
    
    Bed and breakfast: Matched 50 units of the disposal. Acquition cost is �560.29
    Matched trade: Bought 100 unit(s) of ABC on 04-May-2021 for $600.00 = �516.00 Fx rate = 0.86 with total expense �44.29, Total cost: �560.29
    	Expenses: Commission: $1.50 = �1.29 Fx rate = 0.86	Tax: $50.00 = �43.00 Fx rate = 0.86	
    Gain for this match is �458.68 - �560.29 = -�101.61
    50 units of the earlier trade is matched with 100 units of later trade due to share split in between.
    
    At time of disposal, section 104 contains 200 units with value �1,617.20
    Section 104: Matched 100 units of the disposal. Acquition cost is �808.60
    Gain for this match is �917.36 - �808.60 = �108.76
    
    Disposal 2: Sold 100 units of DEF on 05-May-2021 for �858.71.	Total gain (loss): �427.42
    All units of the disposals are matched with acquitions
    Trade details:
    	Sold 100 unit(s) of DEF on 05-May-2021 for $1,000.00 = �860.00 Fx rate = 0.86 with total expense �1.29, Net proceed: �858.71
    	Expenses: Commission: $1.50 = �1.29 Fx rate = 0.86	
    Trade matching:
    Cover unmatched disposal: Matched 100 units of the disposal. Acquition cost is �431.29
    Matched trade: Bought 100 unit(s) of DEF on 06-Dec-2021 for $500.00 = �430.00 Fx rate = 0.86 with total expense �1.29, Total cost: �431.29
    	Expenses: Commission: $1.50 = �1.29 Fx rate = 0.86	
    Gain for this match is �858.71 - �431.29 = �427.42

### Section104
    Section 104 detail history:
    Asset Name ABC
    Date		New Quantity (change)		New Value (change)
    01/05/2021	200 (+200)			�1,617.20 (+�1,617.20)		
    Involved trades:
    Bought 200 unit(s) of ABC on 01-May-2021 for $2,000.00 = �1,600.00 Fx rate = 0.8 with total expense �17.20, Total cost: �1,617.20
    	Expenses: Commission: $1.50 = �1.20 Fx rate = 0.8	Tax: $20.00 = �16.00 Fx rate = 0.8	
    
    03/05/2021	100 (-100)			�808.60 (-�808.60)		
    Involved trades:
    Sold 200 unit(s) of ABC on 03-May-2021 for $2,200.00 = �1,870.00 Fx rate = 0.85 with total expense �35.28, Net proceed: �1,834.73
    	Expenses: Commission: $1.50 = �1.28 Fx rate = 0.85	Tax: $40.00 = �34.00 Fx rate = 0.85	
    
    03/05/2021	200 (+100)			�0.00 (+�0.00)		
    Share adjustment on 03/05/2021 due to corporate action.
