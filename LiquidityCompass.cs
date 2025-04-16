#region Using declarations
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.ComponentModel.DataAnnotations;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class LiquidityCompass : Indicator
    {
        private TextBlock domTextBlock;
        private TextBlock debugTextBlock;
        private Grid chartGrid;

        private int[] bidVolumes;
        private int[] askVolumes;
        private DateTime[] bidClearTimes;
        private DateTime[] askClearTimes;
        private double lastBidTotal = 0;
        private double lastAskTotal = 0;

        private double currentBidTotal = 0;
        private double currentAskTotal = 0;
        private string currentLabel = "";
        private Brush currentColor = Brushes.Gray;

        private readonly TimeSpan bufferDuration = TimeSpan.FromMilliseconds(200);

        [NinjaScriptProperty]
        public string DomInstrument { get; set; } = "ES 06-25";

        [NinjaScriptProperty]
        public int DepthLevels { get; set; } = 10;

        [NinjaScriptProperty]
        [Range(1, int.MaxValue), Gui.PropertyEditor("NinjaTrader.Gui.Tools.IntEditor")]
        public int ThinThreshold { get; set; } = 40;

        [NinjaScriptProperty]
        [Range(1, int.MaxValue), Gui.PropertyEditor("NinjaTrader.Gui.Tools.IntEditor")]
        public int ThickThreshold { get; set; } = 80;

        [NinjaScriptProperty]
        [Display(Name = "Show Debug", Order = 10, GroupName = "Parameters")]
        public bool ShowDebug { get; set; } = true;

        [NinjaScriptProperty]
        [Display(Name = "Enable Logging", Order = 11, GroupName = "Parameters")]
        public bool EnableLogging { get; set; } = false;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Displays DOM liquidity status using full top N depth snapshot.";
                Name = "LiquidityCompass";
                IsOverlay = true;
            }
            else if (State == State.Configure)
            {
                AddDataSeries(DomInstrument, BarsPeriodType.Tick, 1);
            }
            else if (State == State.DataLoaded)
            {
                bidVolumes = new int[DepthLevels];
                askVolumes = new int[DepthLevels];
                bidClearTimes = new DateTime[DepthLevels];
                askClearTimes = new DateTime[DepthLevels];
            }
            else if (State == State.Terminated)
            {
                RemoveTextBlocks();
            }
        }

        protected override void OnBarUpdate() { }

        protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            EnsureTextBlocksCreated();

            if (domTextBlock != null)
            {
                domTextBlock.Text = $"DOM ({DomInstrument}): {currentLabel} ({(currentBidTotal + currentAskTotal) / (2.0 * DepthLevels):N0})";
                domTextBlock.Foreground = currentColor;
            }

            if (debugTextBlock != null && ShowDebug)
            {
                debugTextBlock.Text = $"[DEBUG] Bid: {currentBidTotal:N0} | Ask: {currentAskTotal:N0} | Avg: {(currentBidTotal + currentAskTotal) / (2.0 * DepthLevels):N0}";
            }
        }

        private void EnsureTextBlocksCreated()
        {
            if ((domTextBlock == null || (ShowDebug && debugTextBlock == null)) && ChartControl != null)
            {
                chartGrid = ChartControl.Parent as Grid;
                if (chartGrid != null)
                {
                    ChartControl.Dispatcher.InvokeAsync(() =>
                    {
                        if (domTextBlock == null)
                        {
                            domTextBlock = new TextBlock
                            {
                                Text = "",
                                Foreground = Brushes.Gray,
                                FontSize = 18,
                                Background = Brushes.Black,
                                Opacity = 0.7,
                                Margin = new Thickness(20, 20, 0, 0),
                                HorizontalAlignment = HorizontalAlignment.Left,
                                VerticalAlignment = VerticalAlignment.Top
                            };
                            chartGrid.Children.Add(domTextBlock);
                        }

                        if (ShowDebug && debugTextBlock == null)
                        {
                            debugTextBlock = new TextBlock
                            {
                                Text = "",
                                Foreground = Brushes.LightGray,
                                FontSize = 14,
                                Background = Brushes.Black,
                                Opacity = 0.6,
                                Margin = new Thickness(20, 42, 0, 0),
                                HorizontalAlignment = HorizontalAlignment.Left,
                                VerticalAlignment = VerticalAlignment.Top
                            };
                            chartGrid.Children.Add(debugTextBlock);
                        }
                    });
                }
            }
        }

        private void RemoveTextBlocks()
        {
            if (chartGrid != null)
            {
                ChartControl.Dispatcher.InvokeAsync(() =>
                {
                    if (domTextBlock != null && chartGrid.Children.Contains(domTextBlock))
                        chartGrid.Children.Remove(domTextBlock);

                    if (debugTextBlock != null && chartGrid.Children.Contains(debugTextBlock))
                        chartGrid.Children.Remove(debugTextBlock);

                    domTextBlock = null;
                    debugTextBlock = null;
                });
            }
        }

        protected override void OnMarketDepth(MarketDepthEventArgs e)
        {
            if (!e.Instrument.MasterInstrument.Name.Equals(DomInstrument.Split(' ')[0], StringComparison.OrdinalIgnoreCase))
                return;

            if (e.Position >= DepthLevels)
                return;

            if (EnableLogging)
                Print($"[SuperDOM] L{e.Position} | Type: {e.MarketDataType} | Volume: {e.Volume}");

            DateTime now = DateTime.Now;

            if (e.MarketDataType == MarketDataType.Bid)
            {
                if (e.Operation == Operation.Remove || e.Volume == 0)
                    bidClearTimes[e.Position] = now;
                else
                {
                    bidVolumes[e.Position] = (int)e.Volume;
                    bidClearTimes[e.Position] = DateTime.MinValue;
                }
            }
            else if (e.MarketDataType == MarketDataType.Ask)
            {
                if (e.Operation == Operation.Remove || e.Volume == 0)
                    askClearTimes[e.Position] = now;
                else
                {
                    askVolumes[e.Position] = (int)e.Volume;
                    askClearTimes[e.Position] = DateTime.MinValue;
                }
            }

            double bidTotal = 0, askTotal = 0;
            for (int i = 0; i < DepthLevels; i++)
            {
                if (bidClearTimes[i] != DateTime.MinValue && now - bidClearTimes[i] > bufferDuration)
                    bidVolumes[i] = 0;
                if (askClearTimes[i] != DateTime.MinValue && now - askClearTimes[i] > bufferDuration)
                    askVolumes[i] = 0;

                bidTotal += bidVolumes[i];
                askTotal += askVolumes[i];
            }

            currentBidTotal = bidTotal == 0 ? lastBidTotal : bidTotal;
            currentAskTotal = askTotal == 0 ? lastAskTotal : askTotal;
            lastBidTotal = currentBidTotal;
            lastAskTotal = currentAskTotal;

            double avg = (currentBidTotal + currentAskTotal) / (2.0 * DepthLevels);
            currentLabel = avg >= ThickThreshold ? "THICK" : avg >= ThinThreshold ? "MEDIUM" : "THIN";
            currentColor = avg >= ThickThreshold ? Brushes.LimeGreen : avg >= ThinThreshold ? Brushes.Gold : Brushes.Red;

            if (EnableLogging)
                Print($"[LiquidityCompass] BidSum: {currentBidTotal}, AskSum: {currentAskTotal}, Avg: {avg}");

            ForceRefresh();
        }
    }
}


#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private LiquidityCompass[] cacheLiquidityCompass;
		public LiquidityCompass LiquidityCompass(string domInstrument, int depthLevels, int thinThreshold, int thickThreshold, bool showDebug, bool enableLogging)
		{
			return LiquidityCompass(Input, domInstrument, depthLevels, thinThreshold, thickThreshold, showDebug, enableLogging);
		}

		public LiquidityCompass LiquidityCompass(ISeries<double> input, string domInstrument, int depthLevels, int thinThreshold, int thickThreshold, bool showDebug, bool enableLogging)
		{
			if (cacheLiquidityCompass != null)
				for (int idx = 0; idx < cacheLiquidityCompass.Length; idx++)
					if (cacheLiquidityCompass[idx] != null && cacheLiquidityCompass[idx].DomInstrument == domInstrument && cacheLiquidityCompass[idx].DepthLevels == depthLevels && cacheLiquidityCompass[idx].ThinThreshold == thinThreshold && cacheLiquidityCompass[idx].ThickThreshold == thickThreshold && cacheLiquidityCompass[idx].ShowDebug == showDebug && cacheLiquidityCompass[idx].EnableLogging == enableLogging && cacheLiquidityCompass[idx].EqualsInput(input))
						return cacheLiquidityCompass[idx];
			return CacheIndicator<LiquidityCompass>(new LiquidityCompass(){ DomInstrument = domInstrument, DepthLevels = depthLevels, ThinThreshold = thinThreshold, ThickThreshold = thickThreshold, ShowDebug = showDebug, EnableLogging = enableLogging }, input, ref cacheLiquidityCompass);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.LiquidityCompass LiquidityCompass(string domInstrument, int depthLevels, int thinThreshold, int thickThreshold, bool showDebug, bool enableLogging)
		{
			return indicator.LiquidityCompass(Input, domInstrument, depthLevels, thinThreshold, thickThreshold, showDebug, enableLogging);
		}

		public Indicators.LiquidityCompass LiquidityCompass(ISeries<double> input , string domInstrument, int depthLevels, int thinThreshold, int thickThreshold, bool showDebug, bool enableLogging)
		{
			return indicator.LiquidityCompass(input, domInstrument, depthLevels, thinThreshold, thickThreshold, showDebug, enableLogging);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.LiquidityCompass LiquidityCompass(string domInstrument, int depthLevels, int thinThreshold, int thickThreshold, bool showDebug, bool enableLogging)
		{
			return indicator.LiquidityCompass(Input, domInstrument, depthLevels, thinThreshold, thickThreshold, showDebug, enableLogging);
		}

		public Indicators.LiquidityCompass LiquidityCompass(ISeries<double> input , string domInstrument, int depthLevels, int thinThreshold, int thickThreshold, bool showDebug, bool enableLogging)
		{
			return indicator.LiquidityCompass(input, domInstrument, depthLevels, thinThreshold, thickThreshold, showDebug, enableLogging);
		}
	}
}

#endregion
