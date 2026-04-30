#region Using declarations
using System;
using System.Windows.Media;
using NinjaTrader.Gui;
using NinjaTrader.NinjaScript;
using NinjaTrader.Data;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class z_pricePnL : Indicator
    {
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Calculates the Dollar Value Difference between MNQ and MES";
                Name        = "z_pricePnL";
                Calculate   = Calculate.OnEachTick;
                IsOverlay   = false; // Displays in a separate panel
                
                AddPlot(new Stroke(Brushes.Cyan, 2), PlotStyle.Line, "DollarSpread");
            }
            else if (State == State.Configure)
            {
                // Add the second instrument
                AddDataSeries("MES 06-26", BarsPeriodType.Minute, 1);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBars[0] < 1 || CurrentBars[1] < 1) return;

            if (BarsInProgress == 0)
            {
                // MNQ value: Price * $2 per point
                double mnqValue = Closes[0][0] * 2;
                
                // MES value: Price * $5 per point
                double mesValue = Closes[1][0] * 5;

                // The Spread (The total dollar gap)
                Value[0] = mnqValue - mesValue;
            }
        }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private z_pricePnL[] cachez_pricePnL;
		public z_pricePnL z_pricePnL()
		{
			return z_pricePnL(Input);
		}

		public z_pricePnL z_pricePnL(ISeries<double> input)
		{
			if (cachez_pricePnL != null)
				for (int idx = 0; idx < cachez_pricePnL.Length; idx++)
					if (cachez_pricePnL[idx] != null &&  cachez_pricePnL[idx].EqualsInput(input))
						return cachez_pricePnL[idx];
			return CacheIndicator<z_pricePnL>(new z_pricePnL(), input, ref cachez_pricePnL);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.z_pricePnL z_pricePnL()
		{
			return indicator.z_pricePnL(Input);
		}

		public Indicators.z_pricePnL z_pricePnL(ISeries<double> input )
		{
			return indicator.z_pricePnL(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.z_pricePnL z_pricePnL()
		{
			return indicator.z_pricePnL(Input);
		}

		public Indicators.z_pricePnL z_pricePnL(ISeries<double> input )
		{
			return indicator.z_pricePnL(input);
		}
	}
}

#endregion
