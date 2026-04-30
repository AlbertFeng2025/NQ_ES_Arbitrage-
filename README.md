MNQ-MES Pair Trading: Mean Reversion Strategy
This project provides a Statistical Arbitrage (Mean Reversion) strategy for NinjaTrader 8. It focuses on the price relationship between the Nasdaq 100 (MNQ) and the S&P 500 (MES) Micro Futures.
📈 The Strategy Concept
The strategy operates on the principle that while MNQ and MES are highly correlated, they occasionally "decouple" due to sector-specific volatility (e.g., a tech spike or sell-off).
•	The Ratio: The strategy tracks the ratio of $MNQ / MES$.
•	The Z-Score: It converts this ratio into a Z-Score (standard deviations from the mean).
•	The Trade: When the Z-Score reaches an extreme (e.g., +2.0 or -2.0), the strategy enters a "Pair Trade"—shorting the overvalued instrument and longing the undervalued one.
•	The Exit: The strategy waits for the relationship to return to "Fair Value" (0) or overshoot to an opposite target before closing both positions simultaneously.
________________________________________
🛠 Features
•	Manual Threshold Controls: Independent settings for Trade 1 (Low Entry) and Trade 2 (High Entry).
•	Overshoot Logic: Allows the user to capture profits as the price swings past the mean to the opposite side.
•	Safety Timer: Built-in "Max Minutes" parameter to automatically flatten positions if the trade duration exceeds a user-defined limit.
•	Audit Logging: Automatically generates a .csv log in the NinjaTrader 8 folder to track entry/exit times, Z-Scores, and trade types for post-trade analysis.
•	Diagnostic Mode: Real-time printing to the NinjaTrader Output Window to monitor the strategy’s "thought process."
________________________________________
⚙️ Parameters
Parameter	Description	Default
Entry Level	The Z-Score level required to trigger an entry.	2.0
Opposite Exit	The Z-Score level on the other side of 0 to close the trade.	0.5
Lookback Period	Number of bars used to calculate the Moving Average/Mean.	20
Max Minutes	Maximum time allowed in a trade before an auto-exit.	30
________________________________________
📊 Included Indicators
•	zscore_NQESCorrelationArb: A visual oscillator that plots the Gold Line (Z-Score) on your chart.
•	ArbitrageDollarSpread: A P&L-focused indicator that calculates the raw dollar difference between $(MNQ \times \$2)$ and $(MES \times \$5)$.
________________________________________
🚀 How to Install
1.	Open NinjaTrader 8.
2.	Go to New > NinjaScript Editor.
3.	Right-click the Strategies folder and select New Strategy.
4.	Copy the code from the .cs files in this repository and paste them into the editor.
5.	Press F5 to compile.
⚠️ Disclaimer
This project is for educational and research purposes only. Futures trading contains substantial risk and is not for every investor. Past performance is not necessarily indicative of future results.

