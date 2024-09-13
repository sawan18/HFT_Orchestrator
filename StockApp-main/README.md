# StockApp
This app predicts stock stock trades and trades select stock using TD Ameritrade APIs.

## Code Structure
Code is based on MS .NET (C#). To open the solution, run StockManager.sln.

## Run Settings
Applications requires a set of configurations to determine the stocks to monitor and trade on. 
These are located at ../RunSettings/Config/Settings. 

- **Settings.csv** - Contains the important runtime settings
- **Stocks.csv** - Contains the list of stock to track and the algorithms to use to simulate trade.

## Running the Applications in Visual Studio
1. Create a blank database on SQL Server (local or remote)
2. Ensure that you have the following setting in your debug Start options

meta:"c:\kaamran" server:localhost database:StockManager

Note: This assumes a local installation of SQL Server and a database called StockManager with the Runsettings in a folder "C:\kaamran".

## Config Settings Details
- *TestMode*: {True or False} Determines when the system is running in test mode.
- *Algorithm*: {PriceTrend, Macd} Default trade simulation algorithm.
- *RunMode*: {predict_trades, process_trades, predict_prices} Use **predict_trades** to predict trades using the algorithm selected. Run as **process_trades** to process trades and **predict_prices** to predict the price of trades.
- *RefreshToken*: The refresh token used for trading. This is valid for 90 days from the refresh token date. Set a reminder to update at least a week before it expires
- *ClientId*: The client id provided by TD for the account.
- *RefreshTokenDate*: The expiry date for the refresh token.
- *SimulationStartDate*: The start of algorithm data simulation
- *MonitoringStart*: The monitoring start time. That marks the period, trading is tracked and stocks are traded.
- *MonitoringEnd*: This marks the end of the trading period.
- *HeartBeatMins*: This is the heart beat detail for the system. Defaults to 5mins.
