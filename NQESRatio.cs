#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Gui;
using NinjaTrader.NinjaScript;
using NinjaTrader.Data;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class NQESRatio : Indicator
    {
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description                                 = @"Plots the ratio between NQ and ES";
                Name                                        = "NQESRatio";
                Calculate                                   = Calculate.OnEachTick;
                IsOverlay                                   = false;
                DisplayInDataBox                            = true;
                DrawOnPricePanel                            = false;
                
                AddPlot(new Stroke(Brushes.Cyan, 2), PlotStyle.Line, "Ratio");
            }
            else if (State == State.Configure)
            {
                // Better practice: Use "ES 09-24" (or current) or a string input
                // Note: The BarsPeriod should ideally match your primary chart
                AddDataSeries("ES 06-26", BarsPeriodType.Minute, 5);
            }
        }

        protected override void OnBarUpdate()
        {
            // 1. Ensure we have enough bars on both the primary (NQ) and secondary (ES) series
            if (CurrentBars[0] < 0 || CurrentBars[1] < 0)
                return;

            // 2. We only want to calculate when the primary bar updates
            if (BarsInProgress == 0)
            {
                double nqPrice = Closes[0][0];
                double esPrice = Closes[1][0];

                // 3. Prevent division by zero errors
                if (esPrice != 0)
                {
                    Value[0] = nqPrice / esPrice;
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
		private NQESRatio[] cacheNQESRatio;
		public NQESRatio NQESRatio()
		{
			return NQESRatio(Input);
		}

		public NQESRatio NQESRatio(ISeries<double> input)
		{
			if (cacheNQESRatio != null)
				for (int idx = 0; idx < cacheNQESRatio.Length; idx++)
					if (cacheNQESRatio[idx] != null &&  cacheNQESRatio[idx].EqualsInput(input))
						return cacheNQESRatio[idx];
			return CacheIndicator<NQESRatio>(new NQESRatio(), input, ref cacheNQESRatio);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.NQESRatio NQESRatio()
		{
			return indicator.NQESRatio(Input);
		}

		public Indicators.NQESRatio NQESRatio(ISeries<double> input )
		{
			return indicator.NQESRatio(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.NQESRatio NQESRatio()
		{
			return indicator.NQESRatio(Input);
		}

		public Indicators.NQESRatio NQESRatio(ISeries<double> input )
		{
			return indicator.NQESRatio(input);
		}
	}
}

#endregion
