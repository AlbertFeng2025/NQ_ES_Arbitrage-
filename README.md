MNQ-MES Pair Trading: Mean Reversion Strategy
This project provides a Statistical Arbitrage (Mean Reversion) strategy for NinjaTrader 8. It focuses on the price relationship between the Nasdaq 100 (MNQ) and the S&P 500 (MES) Micro Futures.
📈 The Strategy Concept
The strategy operates on the principle that while MNQ and MES are highly correlated, they occasionally "decouple" due to sector-specific volatility (e.g., a tech spike or sell-off).
•	The Ratio: The strategy tracks the ratio of $MNQ / MES$.
•	The Z-Score: It converts this ratio into a Z-Score (standard deviations from the mean).
•	The Trade: When the Z-Score reaches an extreme (e.g., +2.0 or -2.0), the strategy enters a "Pair Trade"—shorting the overvalued instrument and longing the undervalued one.
•	The Exit: The strategy waits for the relationship to return to "Fair Value" (0) or overshoot to an opposite target before closing both positions simultaneously.
________________________________________
