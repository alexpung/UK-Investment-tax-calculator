## UK investment tax calculator
UK tax calculator for Interactive Broker.
To help report UK tax for dividend and capital gain.

## What is included

1. Blazor WASM application ready to use.  
https://alexpung.github.io/UK-Investment-tax-calculator/

## What is so special about this project?
1.  It is a web app so no installation required and secure.
2. Support trades and dividends in foreign currency.
3. Support shorting and forward split corporate action. (Reverse split I need sample)
4. Implementation of TCGA92/S105 (1)(a): Multiple trades in the same day for the same Buy/Sell side is treated as a single trade. This affect same day/bread and breakfast calculation.

## Supported import format and brokers
1. Interactive Brokers Flex queries only for the moment.

User can also add trades manually using the "Add trades" page.

An example is included (see below). For other brokers I suggest copying the xml example and modifying it manually if you only have small number of trades.
Or if you can code new parsers are welcome.

## Can broker X statements be supported?
The system is designed to accomodate new parsers of different statement files convertiable to a string (or bytestream?).  
Anyone interested can implement a new parser implementing ITaxEventFileParser.  
https://github.com/alexpung/UK-Investment-tax-calculator/tree/master/BlazorApp-Investment%20Tax%20Calculator/Parser/InteractiveBrokersXml

## Current functionality
### Parsed trade type:
1. Trades:
    1. Stock orders
    2. Future contracts (closed, not settled)
    3. Capital gain from foreign currency
2. Dividend income
    1. Dividend cash income
    2. Witholding tax paid
    3. Dividend in Lieu.
3. Corporate actions
    1. Forward split only
4. Options (experimental, use with verification)
    1. Stock option execise/expiration/assignment.
    2. Financial option open/close (cash settlement)
5. View trade calculations and dividend data in a table
6. Add dividends and trades from input of a form. (alpha: in development)

#### Pending implementation
More corporate actions  
PDF report
Tests and feedback are welcome, bugs are to be expected.  

#### Output files
1. Dividend summary by year.
2. Trade summary by year and trade details.
3. Section104 histories showing changes in the pool over time.

## To use:
File sample is [here](https://github.com/alexpung/UK-Investment-tax-calculator/blob/master/UnitTest/System%20Test/Interactive%20brokers/TaxExample.xml "here"), you can download it and put it in the web app to see how it works.
1. You should configure the base currency of your IBKR account to GBP.
2. Configure flex query from interactive brokers. Following report required. Date format dd-MMM-yy, the date and time separator should be set to a single space and time format set to HH:mm:ss.
   1. Cash Transactions: Level of detail: Detail (for dividends)
   2. Corporate actions (for stocks)
   3. Trades: Level of detail: Orders (for stocks)
   4. Statement of Funds: Level of detail: Currency (for Fx transactions) 
   5. Enable "Include exchange rates" at the bottom of the setting (for Fx transactions)"
3. Download the flex query for each year in xml format using web browser.
4. Open the web application.
5. Go to the import sections and select the files, then press the "Upload" button.
6. Press "Start Calculation".
7. Export the results by pressing the buttons at the export file section.

## Privacy concerns:

Your trade data is not uploaded anywhere. They never leave your browser thanks to Blazor WASM framework. The calculation is entirely done in your browser.  

## Example
Examples can be found in https://github.com/alexpung/UK-Investment-tax-calculator/tree/master/UnitTest/System%20Test/Interactive%20brokers

### Output:
Dividend Summary

    Tax Year: 2020
    Region: JPN (JP)
    	Total dividends: £555.00
    	Total withholding tax: -£166.50
		
    		Transactions:
    		Asset Name: ABCD, Date: 02/02/2021, Type: Withholding Tax, Amount: -¥30,000, FxRate: 0.00555, Sterling Amount: -£166.50, Description: ABC CASH DIVIDEND - JP TAX
    		Asset Name: ABCD, Date: 02/02/2021, Type: Dividend, Amount: ¥90,000, FxRate: 0.00555, Sterling Amount: £499.50, Description: ABC CASH DIVIDEND (Ordinary Dividend)
    		Asset Name: ABCD, Date: 02/02/2021, Type: Payment In Lieu of a Dividend, Amount: ¥10,000, FxRate: 0.00555, Sterling Amount: £55.50, Description: ABC CASH DIVIDEND (Ordinary Dividend)

### Trades Calculation
    Summary for tax year 2021:
    Number of disposals: 2
    Total disposal proceeds: £2,693.00
    Total allowable costs: £2,261.00
    Total gains (excluding loss): £432.00
    Total loss: £0.00
    
    Disposal 1: Sold 200 units of ABC on 03-May-2021 for £1,834.73.	Total gain (loss): £5.56
    All units of the disposals are matched with acquitions
    Trade details:
    	Sold 200 unit(s) of ABC on 03-May-2021 for $2,200.00 = £1,870.00 Fx rate = 0.85 with total expense £35.28, Net proceed: £1,834.73
    	Expenses: Commission: $1.50 = £1.28 Fx rate = 0.85	Tax: $40.00 = £34.00 Fx rate = 0.85	
    Trade matching:
    Same day: Matched 50 units of the disposal. Acquition cost is £460.28
    Matched trade: Bought 50 unit(s) of ABC on 03-May-2021 for $510.00 = £433.50 Fx rate = 0.85 with total expense £26.78, Total cost: £460.28
    	Expenses: Commission: $1.50 = £1.28 Fx rate = 0.85	Tax: $30.00 = £25.50 Fx rate = 0.85	
    Gain for this match is £458.68 - £460.28 = -£1.59
    
    
    
    Bed and breakfast: Matched 50 units of the disposal. Acquition cost is £560.29
    Matched trade: Bought 100 unit(s) of ABC on 04-May-2021 for $600.00 = £516.00 Fx rate = 0.86 with total expense £44.29, Total cost: £560.29
    	Expenses: Commission: $1.50 = £1.29 Fx rate = 0.86	Tax: $50.00 = £43.00 Fx rate = 0.86	
    Gain for this match is £458.68 - £560.29 = -£101.61
    50 units of the earlier trade is matched with 100 units of later trade due to share split in between.
    
    At time of disposal, section 104 contains 200 units with value £1,617.20
    Section 104: Matched 100 units of the disposal. Acquition cost is £808.60
    Gain for this match is £917.36 - £808.60 = £108.76
    
    Disposal 2: Sold 100 units of DEF on 05-May-2021 for £858.71.	Total gain (loss): £427.42
    All units of the disposals are matched with acquitions
    Trade details:
    	Sold 100 unit(s) of DEF on 05-May-2021 for $1,000.00 = £860.00 Fx rate = 0.86 with total expense £1.29, Net proceed: £858.71
    	Expenses: Commission: $1.50 = £1.29 Fx rate = 0.86	
    Trade matching:
    Cover unmatched disposal: Matched 100 units of the disposal. Acquition cost is £431.29
    Matched trade: Bought 100 unit(s) of DEF on 06-Dec-2021 for $500.00 = £430.00 Fx rate = 0.86 with total expense £1.29, Total cost: £431.29
    	Expenses: Commission: $1.50 = £1.29 Fx rate = 0.86	
    Gain for this match is £858.71 - £431.29 = £427.42

### Section104
    Section 104 detail history:
    Asset Name ABC
    Date		New Quantity (change)		New Value (change)
    01/05/2021	200 (+200)			£1,617.20 (+£1,617.20)		
    Involved trades:
    Bought 200 unit(s) of ABC on 01-May-2021 for $2,000.00 = £1,600.00 Fx rate = 0.8 with total expense £17.20, Total cost: £1,617.20
    	Expenses: Commission: $1.50 = £1.20 Fx rate = 0.8	Tax: $20.00 = £16.00 Fx rate = 0.8	
    
    03/05/2021	100 (-100)			£808.60 (-£808.60)		
    Involved trades:
    Sold 200 unit(s) of ABC on 03-May-2021 for $2,200.00 = £1,870.00 Fx rate = 0.85 with total expense £35.28, Net proceed: £1,834.73
    	Expenses: Commission: $1.50 = £1.28 Fx rate = 0.85	Tax: $40.00 = £34.00 Fx rate = 0.85	
    
    03/05/2021	200 (+100)			£0.00 (+£0.00)		
    Share adjustment on 03/05/2021 due to corporate action.

### Future Contracts
        Disposal 1: Close short position 2 units of NIYH4 on 05-Mar-2023.	Total gain (loss): -£28,011.90
    Trade details:
	    Bought 2 unit(s) of NIYH4 on 05-Mar-2023 12:35 with contract value ¥31,000,000 with total expense £6.30
	    Expenses: Commission: ¥900 = £6.30 Fx rate = 0.007	
    Trade matching:
    Same day: Matched 2 units of the disposal. Acquisition contract value is ¥31,000,000 and disposal contract value is ¥27,000,000
    Payment made to close the contract as loss is (¥27,000,000 - ¥31,000,000) * 0.007 = -£28,000.00, added to allowable cost
    Total dealing cost is £11.90
    Matched trade: Sold 2 unit(s) of NIYH4 on 05-Mar-2023 12:34 with contract value ¥27,000,000 with total expense £5.60
	    Expenses: Commission: ¥700 = £5.60 Fx rate = 0.008	
    Gain for this match is -£28,000.00 - £11.90  = -£28,011.90

    *******************************************************************************
    Disposal 2: Close long position 2 units of NIYH6 on 07-Mar-2023.	Total gain (loss): -£42,009.00
    Trade details:
	    Sold 2 unit(s) of NIYH6 on 07-Mar-2023 12:34 with contract value ¥20,000,000 with total expense £4.80
	    Expenses: Commission: ¥800 = £4.80 Fx rate = 0.006	
    Trade matching:
    At time of disposal, section 104 contains 2 units with contract value ¥27,000,000
    Section 104: Matched 2 units of the disposal. Acquisition contract value is ¥27,000,000 and disposal contract value ¥20,000,000, proportioned dealing cost is £4.20
    Payment made to close the contract as loss is (¥20,000,000 - ¥27,000,000) * 0.006 = -£42,000.00, added to allowable cost
    Total dealing cost is £9.00
    Gain for this match is -£42,000.00 - £9.00  = -£42,009.00

### FX
        Disposal 1: Dispose 375 units of USD on 17-Dec-2019 for £285.59.	Total gain (loss): -£1.14
    All units of the disposals are matched with acquisitions
    Trade details:
	    Dispose 375 unit(s) of USD on 17-Dec-2019 00:00 for $375.00 = £285.59 Fx rate = 0.76158. Description: Testing
    Trade matching:
    Bed and breakfast: 375 units of the acquisition trade against 375 units of the disposal trade. Acquisition cost is £286.74
    Matched trade: Acquire 425 unit(s) of USD on 18-Dec-2019 00:00 for $425.00 = £324.97 Fx rate = 0.76463. Description: Testing 2
    Gain for this match is £285.59 - £286.74 = -£1.14
