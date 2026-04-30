#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations; // Required for [Range]
using System.Windows.Media;                  // Required for Brushes and Stroke
using NinjaTrader.Gui;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.Data;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class zscore_NQESCorrelationArb : Indicator
    {
        private Series<double> ratioSeries;
        private SMA smaRatio;
        private StdDev stdDevRatio;

        // Use the full namespace for Range to avoid "ambiguous reference" errors
        [NinjaScriptProperty]
        [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
        public int Period { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description                                 = "NQ/ES Ratio Z-Score for Arbitrage";
                Name                                        = "zscore_NQESCorrelationArb";
                Calculate                                   = Calculate.OnEachTick;
                IsOverlay                                   = false;
                Period                                      = 20;

                // Ensure Brushes is recognized from System.Windows.Media
                AddPlot(new Stroke(System.Windows.Media.Brushes.Gold, 2), PlotStyle.Line, "ZScore");
                
                AddLine(Brushes.Gray, 0, "Mean");
                AddLine(Brushes.Red, 2, "UpperExtreme");
                AddLine(Brushes.Red, -2, "LowerExtreme");
            }
            else if (State == State.Configure)
            {
                // Note: Change this to your current contract month
                AddDataSeries("ES 06-26", BarsPeriodType.Minute, 5);
            }
            else if (State == State.DataLoaded)
            {
                ratioSeries = new Series<double>(this);
                // Initialize indicators in DataLoaded to ensure series are ready
                smaRatio = SMA(ratioSeries, Period);
                stdDevRatio = StdDev(ratioSeries, Period);
            }
        }

        protected override void OnBarUpdate()
        {
            // Ensure both data series have enough bars processed
            if (CurrentBars[0] < Period || CurrentBars[1] < Period)
                return;

            // Only calculate on the primary series update
            if (BarsInProgress == 0)
            {
                double nqPrice = Closes[0][0];
                double esPrice = Closes[1][0];

                if (esPrice != 0)
                {
                    double currentRatio = nqPrice / esPrice;
                    ratioSeries[0] = currentRatio;

                    double mean = smaRatio[0];
                    double sd = stdDevRatio[0];

                    if (sd != 0)
                    {
                        Value[0] = (currentRatio - mean) / sd;
                    }
                }
            }
        }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private zscore_NQESCorrelationArb[] cachezscore_NQESCorrelationArb;
		public zscore_NQESCorrelationArb zscore_NQESCorrelationArb(int period)
		{
			return zscore_NQESCorrelationArb(Input, period);
		}

		public zscore_NQESCorrelationArb zscore_NQESCorrelationArb(ISeries<double> input, int period)
		{
			if (cachezscore_NQESCorrelationArb != null)
				for (int idx = 0; idx < cachezscore_NQESCorrelationArb.Length; idx++)
					if (cachezscore_NQESCorrelationArb[idx] != null && cachezscore_NQESCorrelationArb[idx].Period == period && cachezscore_NQESCorrelationArb[idx].EqualsInput(input))
						return cachezscore_NQESCorrelationArb[idx];
			return CacheIndicator<zscore_NQESCorrelationArb>(new zscore_NQESCorrelationArb(){ Period = period }, input, ref cachezscore_NQESCorrelationArb);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.zscore_NQESCorrelationArb zscore_NQESCorrelationArb(int period)
		{
			return indicator.zscore_NQESCorrelationArb(Input, period);
		}

		public Indicators.zscore_NQESCorrelationArb zscore_NQESCorrelationArb(ISeries<double> input , int period)
		{
			return indicator.zscore_NQESCorrelationArb(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.zscore_NQESCorrelationArb zscore_NQESCorrelationArb(int period)
		{
			return indicator.zscore_NQESCorrelationArb(Input, period);
		}

		public Indicators.zscore_NQESCorrelationArb zscore_NQESCorrelationArb(ISeries<double> input , int period)
		{
			return indicator.zscore_NQESCorrelationArb(input, period);
		}
	}
}

#endregion
