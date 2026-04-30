#region Using declarations
using System;
using System.ComponentModel.DataAnnotations;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
#endregion

namespace NinjaTrader.NinjaScript.Strategies
{
    /// <summary>
    /// MNQ / MES Statistical Arbitrage (Pairs Trading) Strategy -- v4
    /// ----------------------------------------------------------
    /// CHANGE LOG v4 (CRITICAL FIX):
    ///   - Fixed race condition in pairActive tracking. v3 cleared the
    ///     pairActive lock as soon as both positions appeared flat at
    ///     the top of OnBarUpdate. But after sending entry orders, the
    ///     positions ARE still flat for several ticks until fills come
    ///     back -- so the lock got cleared, the same Z signal fired
    ///     again, and a SECOND pair of entry orders went out. Result:
    ///     doubled-up positions (e.g. 10 MNQ + 4 MES instead of 5 + 2),
    ///     and exit orders sized for 5/2 only closed half the position.
    ///     The remainder hung on until session-end auto-flatten.
    ///   - New approach: use a small state machine instead of a single
    ///     bool. States: FLAT -> ENTRY_PENDING -> OPEN -> EXIT_PENDING
    ///     -> FLAT. Transitions are driven by OnPositionUpdate so we
    ///     react to actual fills, not to position snapshots that may
    ///     pre-date our orders.
    ///   - Made entry/exit orders explicitly market orders for clarity
    ///     (NinjaScript default is already market, but stating it
    ///     prevents confusion if the code is later edited).
    /// </summary>
    public class MNQMES_ManualLevels : Strategy
    {
        // === STATE MACHINE ===
        // FLAT          : no pair, free to enter
        // ENTRY_PENDING : entry orders sent, awaiting fills on both legs
        // OPEN          : both legs filled, monitoring for exit conditions
        // EXIT_PENDING  : exit orders sent, awaiting fills to flatten
        private enum PairState { Flat, EntryPending, Open, ExitPending }
        private PairState pairState = PairState.Flat;

        // === INTERNAL STATE ===
        private Series<double> ratioSeries;
        private SMA smaRatio;
        private StdDev stdDevRatio;
        private DateTime entryTime;
        private double entryZ;

        // Track which trade we entered (for catastrophic-stop sign convention)
        private enum TradeType { None, Trade1, Trade2 }
        private TradeType activeTrade = TradeType.None;

        // === USER PARAMETERS ===

        // --- TRADE 1: NQ IS CHEAP ---
        [Display(Name="1a. Trade 1 Entry (Low)", Description="Entry when Z is below this (e.g. -2.0)", GroupName="1. Trade 1 (Low Entry)", Order=1)]
        public double Trade1Entry { get; set; }

        [Display(Name="1b. Trade 1 Exit (High)", Description="Exit when Z rises to this (e.g. 0.5)", GroupName="1. Trade 1 (Low Entry)", Order=2)]
        public double Trade1Exit { get; set; }

        // --- TRADE 2: NQ IS EXPENSIVE ---
        [Display(Name="2a. Trade 2 Entry (High)", Description="Entry when Z is above this (e.g. 2.0)", GroupName="2. Trade 2 (High Entry)", Order=3)]
        public double Trade2Entry { get; set; }

        [Display(Name="2b. Trade 2 Exit (Low)", Description="Exit when Z drops to this (e.g. -0.5)", GroupName="2. Trade 2 (High Entry)", Order=4)]
        public double Trade2Exit { get; set; }

        // --- TIMING & MATH ---
        [Display(Name="3. Max Minutes", Description="Force close after X minutes (e.g. 30)", GroupName="3. Timing & Math", Order=5)]
        public int MaxMinutes { get; set; }

        [Display(Name="4. Lookback Period", Description="Bars used for average (Standard 20)", GroupName="3. Timing & Math", Order=6)]
        public int Period { get; set; }

        // --- HEDGE SIZING ---
        // Approximate dollar-neutral: MnqQty/MesQty = (MES_price * $5) / (MNQ_price * $2)
        // At MNQ ~27,000 / MES ~6,850 that's ~0.63, i.e. ~1.6 MNQ per 1 MES.
        // Defaults below: 5 MNQ : 2 MES (test sizing, 7 contracts per pair total).
        [Display(Name="5a. MNQ Contracts", Description="MNQ contracts per leg (default 5)", GroupName="4. Hedge Ratio", Order=7)]
        public int MnqQty { get; set; }

        [Display(Name="5b. MES Contracts", Description="MES contracts per leg (default 2)", GroupName="4. Hedge Ratio", Order=8)]
        public int MesQty { get; set; }

        // --- CATASTROPHIC STOP ---
        [Display(Name="6. Catastrophic Z Stop", Description="Exit if Z moves this many SDs against entry (e.g. 2.0)", GroupName="5. Risk", Order=9)]
        public double CatastrophicZStop { get; set; }

        // --- SECONDARY INSTRUMENT ---
        // "MES 06-26" (explicit) or "MES ##-##" (continuous, requires Merge Policy
        // enabled under Tools > Options > Market Data > Historical).
        [Display(Name="7. MES Contract", Description="e.g. 'MES 06-26' (explicit) or 'MES ##-##' (continuous)", GroupName="6. Instruments", Order=10)]
        public string MesContract { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "MNQMES_ManualLevels";

                Trade1Entry = -2.0; Trade1Exit = 0.5;
                Trade2Entry =  2.0; Trade2Exit = -0.5;

                MaxMinutes = 30;
                Period     = 20;

                MnqQty = 5;
                MesQty = 2;

                CatastrophicZStop = 2.0;

                MesContract = "MES 06-26";

                Calculate           = Calculate.OnEachTick;
                EntriesPerDirection = 1;
            }
            else if (State == State.Configure)
            {
                if (string.IsNullOrWhiteSpace(MesContract))
                {
                    Log("MNQMES_ManualLevels: MesContract is empty.", LogLevel.Error);
                    SetState(State.Finalized);
                    return;
                }

                string trimmed = MesContract.Trim();
                if (!trimmed.StartsWith("MES ", StringComparison.OrdinalIgnoreCase))
                {
                    Log("MNQMES_ManualLevels: MesContract '" + MesContract
                        + "' bad format. Use 'MES 06-26' or 'MES ##-##'.",
                        LogLevel.Error);
                    SetState(State.Finalized);
                    return;
                }

                AddDataSeries(trimmed, BarsPeriodType.Minute, 1);
            }
            else if (State == State.DataLoaded)
            {
                if (BarsArray.Length < 2 || BarsArray[1] == null)
                {
                    Log("MNQMES_ManualLevels: secondary series did not load.", LogLevel.Error);
                    SetState(State.Finalized);
                    return;
                }

                Log("MNQMES_ManualLevels: secondary instrument loaded as '"
                    + BarsArray[1].Instrument.FullName + "'. Sizing: "
                    + MnqQty + " MNQ : " + MesQty + " MES.",
                    LogLevel.Information);

                ratioSeries  = new Series<double>(this);
                smaRatio     = SMA(ratioSeries, Period);
                stdDevRatio  = StdDev(ratioSeries, Period);
                pairState    = PairState.Flat;
                activeTrade  = TradeType.None;
            }
        }

        // -----------------------------------------------------------------
        // OnPositionUpdate fires whenever a position changes, including
        // partial fills. We use it to drive state transitions based on
        // ACTUAL fills rather than guessing from snapshots in OnBarUpdate.
        // -----------------------------------------------------------------
        protected override void OnPositionUpdate(Position position,
            double averagePrice, int quantity, MarketPosition marketPosition)
        {
            // Only react when BOTH legs have settled into their final state
            // for the current pair stage.
            MarketPosition mnqPos = Positions[0].MarketPosition;
            MarketPosition mesPos = Positions[1].MarketPosition;
            int mnqQty = Positions[0].Quantity;
            int mesQty = Positions[1].Quantity;

            // ENTRY_PENDING -> OPEN: both legs reached intended size
            if (pairState == PairState.EntryPending)
            {
                bool t1Filled = activeTrade == TradeType.Trade1
                                && mnqPos == MarketPosition.Long  && mnqQty >= MnqQty
                                && mesPos == MarketPosition.Short && mesQty >= MesQty;
                bool t2Filled = activeTrade == TradeType.Trade2
                                && mnqPos == MarketPosition.Short && mnqQty >= MnqQty
                                && mesPos == MarketPosition.Long  && mesQty >= MesQty;

                if (t1Filled || t2Filled)
                {
                    pairState = PairState.Open;
                    Print(Time[0] + " :: Pair fully filled. State -> Open.");
                }
            }
            // EXIT_PENDING -> FLAT: both legs back to zero
            else if (pairState == PairState.ExitPending)
            {
                if (mnqPos == MarketPosition.Flat && mesPos == MarketPosition.Flat)
                {
                    pairState   = PairState.Flat;
                    activeTrade = TradeType.None;
                    Print(Time[0] + " :: Pair fully exited. State -> Flat.");
                }
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBars[0] < Period || CurrentBars[1] < Period) return;
            if (BarsInProgress != 0) return;

            // === MATH ===
            double currentRatio = Closes[0][0] / Closes[1][0];
            ratioSeries[0] = currentRatio;

            double sd = stdDevRatio[0];
            if (sd <= 0) return;

            double currentZ = (currentRatio - smaRatio[0]) / sd;

            // -------------------------------------------------------------
            // ENTRY: only when fully Flat. Cannot enter while pending or open.
            // -------------------------------------------------------------
            if (pairState == PairState.Flat)
            {
                if (currentZ <= Trade1Entry)
                {
                    pairState   = PairState.EntryPending;  // LOCK before sending
                    activeTrade = TradeType.Trade1;
                    entryTime   = Time[0];
                    entryZ      = currentZ;

                    Print(Time[0] + " :: ENTRY T1. Z=" + currentZ.ToString("F3")
                        + " ratio=" + currentRatio.ToString("F5"));

                    // Market orders (NinjaScript default for these calls)
                    EnterLong (0, MnqQty, "T1");
                    EnterShort(1, MesQty, "T1");
                }
                else if (currentZ >= Trade2Entry)
                {
                    pairState   = PairState.EntryPending;
                    activeTrade = TradeType.Trade2;
                    entryTime   = Time[0];
                    entryZ      = currentZ;

                    Print(Time[0] + " :: ENTRY T2. Z=" + currentZ.ToString("F3")
                        + " ratio=" + currentRatio.ToString("F5"));

                    EnterShort(0, MnqQty, "T2");
                    EnterLong (1, MesQty, "T2");
                }
            }
            // -------------------------------------------------------------
            // EXIT: only when Open. Skip while EntryPending or ExitPending
            // to avoid sending exit orders on a position that's still
            // partially filling, and to avoid duplicate exits.
            // -------------------------------------------------------------
            else if (pairState == PairState.Open)
            {
                bool timeExpired = (Time[0] - entryTime).TotalMinutes >= MaxMinutes;

                if (activeTrade == TradeType.Trade1)
                {
                    bool targetHit    = currentZ >= Trade1Exit;
                    bool catastrophic = currentZ <= (entryZ - CatastrophicZStop);

                    if (targetHit || timeExpired || catastrophic)
                    {
                        pairState = PairState.ExitPending;  // LOCK before sending

                        Print(Time[0] + " :: EXIT T1. Z=" + currentZ.ToString("F3")
                            + " reason=" + (targetHit ? "target" : timeExpired ? "time" : "catastrophic"));

                        // Use the position's actual quantity, not MnqQty/MesQty,
                        // in case fills came back partial. This guarantees we
                        // exit EXACTLY what we hold, no orphans, no over-exits.
                        int qty0 = Positions[0].Quantity;
                        int qty1 = Positions[1].Quantity;

                        if (qty0 > 0) ExitLong (0, qty0, "ExitT1", "T1");
                        if (qty1 > 0) ExitShort(1, qty1, "ExitT1", "T1");
                    }
                }
                else if (activeTrade == TradeType.Trade2)
                {
                    bool targetHit    = currentZ <= Trade2Exit;
                    bool catastrophic = currentZ >= (entryZ + CatastrophicZStop);

                    if (targetHit || timeExpired || catastrophic)
                    {
                        pairState = PairState.ExitPending;

                        Print(Time[0] + " :: EXIT T2. Z=" + currentZ.ToString("F3")
                            + " reason=" + (targetHit ? "target" : timeExpired ? "time" : "catastrophic"));

                        int qty0 = Positions[0].Quantity;
                        int qty1 = Positions[1].Quantity;

                        if (qty0 > 0) ExitShort(0, qty0, "ExitT2", "T2");
                        if (qty1 > 0) ExitLong (1, qty1, "ExitT2", "T2");
                    }
                }
            }
            // EntryPending and ExitPending states intentionally do nothing
            // here -- the state machine waits for OnPositionUpdate to
            // confirm fills before allowing the next action.
        }
    }
}