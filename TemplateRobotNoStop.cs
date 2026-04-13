using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Logging;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;

/* Description
trading robot for osengine

TODO: описание стратегии

Buy:
TODO

Sell:
TODO

Exit from buy:
TODO

Exit from sell:
TODO

Volume:
Calculated as % of deposit. No stop-loss distance required.
Supports SPOT / LinearPerpetual, Stocks MOEX, Bonds MOEX.
*/

namespace OsEngine.Robots
{
    [Bot("TemplateRobotNoStop")]
    public class TemplateRobotNoStop : BotPanel
    {
        // ── Вкладка ──────────────────────────────────────────────────────────────────
        private readonly BotTabSimple _tab;

        // ── Базовые параметры ────────────────────────────────────────────────────────
        private readonly StrategyParameterString _regime;
        private StrategyParameterString _tradeLogOnOff;

        // ── Параметры объёма ─────────────────────────────────────────────────────────
        private readonly StrategyParameterString _modeTrade;
        private readonly StrategyParameterString _assetNameCurrent;
        private readonly StrategyParameterDecimal _volumeLong;
        private readonly StrategyParameterDecimal _volumeShort;
        private readonly StrategyParameterDecimal _slippagePercent;
        private readonly StrategyParameterInt _bondDaysToMaturity;
        private readonly StrategyParameterButton _tradePeriodsShowDialogButton;

        // ── Параметры неторгового времени ────────────────────────────────────────────
        private readonly StrategyParameterInt _timeZoneUtc;
        private bool _nonTradePeriodLogged;
        private readonly NonTradePeriods _tradePeriods;

        // ── TODO: добавить параметры индикаторов здесь ───────────────────────────────
        // private readonly StrategyParameterInt _lengthMyIndicator;

        // ── TODO: добавить индикаторы здесь ──────────────────────────────────────────
        // private Aindicator _myIndicator;

        // ── Рабочие переменные расчёта объёма ────────────────────────────────────────
        // Синхронизированные копии параметров GUI (обновляются через SyncParams)
        private decimal _curVolumeLong;
        private decimal _curVolumeShort;
        private decimal _curSlippagePercent;
        private int _curBondDaysToMaturity;
        private int _curTimeZoneUtc;

        // ════════════════════════════════════════════════════════════════════════════
        //   КОНСТРУКТОР
        // ════════════════════════════════════════════════════════════════════════════

        public TemplateRobotNoStop(string name, StartProgram startProgram) : base(name, startProgram)
        {
            _tradePeriods = new NonTradePeriods(name);
            _tradePeriods.NonTradePeriodGeneral.NonTradePeriod1Start = new TimeOfDay { Hour = 0, Minute = 0 };
            _tradePeriods.NonTradePeriodGeneral.NonTradePeriod1End = new TimeOfDay { Hour = 10, Minute = 5 };
            _tradePeriods.NonTradePeriodGeneral.NonTradePeriod1OnOff = true;
            _tradePeriods.NonTradePeriodGeneral.NonTradePeriod2Start = new TimeOfDay { Hour = 13, Minute = 54 };
            _tradePeriods.NonTradePeriodGeneral.NonTradePeriod2End = new TimeOfDay { Hour = 14, Minute = 6 };
            _tradePeriods.NonTradePeriodGeneral.NonTradePeriod2OnOff = false;
            _tradePeriods.NonTradePeriodGeneral.NonTradePeriod3Start = new TimeOfDay { Hour = 18, Minute = 1 };
            _tradePeriods.NonTradePeriodGeneral.NonTradePeriod3End = new TimeOfDay { Hour = 23, Minute = 58 };
            _tradePeriods.NonTradePeriodGeneral.NonTradePeriod3OnOff = true;
            _tradePeriods.TradeInSunday = false;
            _tradePeriods.TradeInSaturday = false;
            _tradePeriods.Load();

            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            _tradePeriodsShowDialogButton = CreateParameterButton("Non trade periods", "Base");
            _tradePeriodsShowDialogButton.UserClickOnButtonEvent += () => _tradePeriods.ShowDialog();

            // Базовые настройки
            _regime = CreateParameter("Regime", "Off",
                new[] { "Off", "On", "LONG-POS", "SHORT-POS", "CLOSE-POS" }, "Base");
            _timeZoneUtc = CreateParameter("Time zone UTC", 4, -24, 24, 1, "Base");

            // Настройки объёма
            _modeTrade = CreateParameter("Trade Section",
                "SPOT и LinearPerpetual",
                new[] { "SPOT и LinearPerpetual", "Stocks MOEX", "Bonds MOEX" }, "Base");
            _assetNameCurrent = CreateParameter("Deposit Asset", "USDT",
                new[] { "USDT", "USDC", "USD", "RUB", "EUR", "BTC", "ETH", "XRP", "LTC", "SOL", "Prime" }, "Base");
            _tradeLogOnOff = CreateParameter("Trade debug log", "Off", new[] { "On", "Off" }, "Base");
            _volumeLong = CreateParameter("Volume Long (%)", 2.5m, 0.1m, 50m, 0.1m, "Base");
            _volumeShort = CreateParameter("Volume Short (%)", 2.5m, 0.1m, 50m, 0.1m, "Base");
            _slippagePercent = CreateParameter("Slippage (%)", 0.1m, 0.01m, 2m, 0.01m, "Base");
            _bondDaysToMaturity = CreateParameter("Bond days to maturity", 30, 1, 365, 1, "Base");

            // TODO: создать параметры индикаторов здесь
            // _lengthMyIndicator = CreateParameter("Length", 20, 5, 200, 5, "Indicator");

            // TODO: создать и подключить индикаторы здесь
            // _myIndicator = IndicatorsFactory.CreateIndicatorByName("MyIndicator", name + "MyIndicator", false);
            // _myIndicator = (Aindicator)_tab.CreateCandleIndicator(_myIndicator, "Prime");
            // ((IndicatorParameterInt)_myIndicator.Parameters[0]).ValueInt = _lengthMyIndicator.ValueInt;
            // _myIndicator.Save();

            // Подписки
            ParametrsChangeByUser += OnParametrsChangeByUser;
            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;

            SyncParams();

            Description = "TODO: описание робота. " +
                          "Volume is calculated as % of deposit (no stop distance required).";
        }

        // ════════════════════════════════════════════════════════════════════════════
        //   ОБЯЗАТЕЛЬНЫЕ ПЕРЕГРУЗКИ
        // ════════════════════════════════════════════════════════════════════════════

        public override string GetNameStrategyType() => "TemplateRobotNoStop";

        public override void ShowIndividualSettingsDialog() { }

        // ════════════════════════════════════════════════════════════════════════════
        //   СИНХРОНИЗАЦИЯ ПАРАМЕТРОВ
        // ════════════════════════════════════════════════════════════════════════════

        private void OnParametrsChangeByUser()
        {
            // TODO: обновить параметры индикаторов здесь
            // ((IndicatorParameterInt)_myIndicator.Parameters[0]).ValueInt = _lengthMyIndicator.ValueInt;
            // _myIndicator.Save();
            // _myIndicator.Reload();

            SyncParams();
        }

        private DateTime LocalTime(DateTime utcTime)
        {
            if (utcTime == DateTime.MinValue) return utcTime;
            return utcTime.AddHours(_curTimeZoneUtc);
        }

        private void SyncParams()
        {
            _curVolumeLong = Math.Min(_volumeLong.ValueDecimal, 2500m);
            _curVolumeShort = Math.Min(_volumeShort.ValueDecimal, 2500m);
            _curSlippagePercent = _slippagePercent.ValueDecimal;
            _curTimeZoneUtc = _timeZoneUtc.ValueInt;
            _curBondDaysToMaturity = _bondDaysToMaturity.ValueInt;
        }

        // ════════════════════════════════════════════════════════════════════════════
        //   ГЛАВНЫЙ ОБРАБОТЧИК СВЕЧИ
        // ════════════════════════════════════════════════════════════════════════════

        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            if (_regime.ValueString == "Off") return;

            // TODO: заменить минимальное количество свечей под свой индикатор
            if (candles.Count < 20) return;

            DateTime now = LocalTime(_tab.TimeServerCurrent);
            if (!_tradePeriods.CanTradeThisTime(now))
            {
                if (!_nonTradePeriodLogged)
                {
                    _nonTradePeriodLogged = true;
                    SendNewLogMessage($"⏰ Неторговое время (UTC+{_curTimeZoneUtc}): {now:HH:mm:ss} — входы заблокированы", LogMessageType.System);
                }
                return;
            }

            if (_nonTradePeriodLogged)
            {
                _nonTradePeriodLogged = false;
                SendNewLogMessage($"✅ Торговое время возобновлено (UTC+{_curTimeZoneUtc}): {now:HH:mm:ss}", LogMessageType.System);
            }

            List<Position> openPositions = _tab.PositionsOpenAll;

            if (openPositions != null && openPositions.Count != 0)
                LogicClosePosition(candles);

            if (_regime.ValueString == "CLOSE-POS") return;

            if (openPositions == null || openPositions.Count == 0)
                LogicOpenPosition(candles);
        }

        // ════════════════════════════════════════════════════════════════════════════
        //   ЛОГИКА ОТКРЫТИЯ — TODO: реализовать
        //
        //   Шаблон показывает правильный порядок действий:
        //   1. Получить сигнальные значения индикаторов
        //   2. Рассчитать объём через CalcVolume(side, entryPrice)
        //   3. Отправить ордер
        // ════════════════════════════════════════════════════════════════════════════

        private void LogicOpenPosition(List<Candle> candles)
        {
            decimal lastPrice = candles[candles.Count - 1].Close;

            // TODO: получить значения индикатора
            // decimal signalValue = _myIndicator.DataSeries[0].Last;

            // ── LONG ─────────────────────────────────────────────────────────────────
            if (_regime.ValueString == "On" || _regime.ValueString == "LONG-POS")
            {
                // TODO: условие входа в лонг
                // if (lastPrice > signalValue)
                // {
                //     decimal entry = _tab.PriceBestAsk;
                //     if (entry <= 0) entry = lastPrice;
                //
                //     decimal volume = CalcVolume(Side.Buy, entry);
                //     if (volume <= 0) return;
                //
                //     decimal slippage = entry * (_curSlippagePercent / 100m);
                //     _tab.BuyAtLimit(volume, entry + slippage);
                //
                //     SendNewLogMessage(
                //         $"[OPEN] BUY | entry≈{entry:F4} vol={volume}",
                //         LogMessageType.System);
                // }
            }

            // ── SHORT ────────────────────────────────────────────────────────────────
            if (_regime.ValueString == "On" || _regime.ValueString == "SHORT-POS")
            {
                // TODO: условие входа в шорт
                // if (lastPrice < signalValue)
                // {
                //     decimal entry = _tab.PriceBestBid;
                //     if (entry <= 0) entry = lastPrice;
                //
                //     decimal volume = CalcVolume(Side.Sell, entry);
                //     if (volume <= 0) return;
                //
                //     decimal slippage = entry * (_curSlippagePercent / 100m);
                //     _tab.SellAtLimit(volume, entry - slippage);
                //
                //     SendNewLogMessage(
                //         $"[OPEN] SELL | entry≈{entry:F4} vol={volume}",
                //         LogMessageType.System);
                // }
            }
        }

        // ════════════════════════════════════════════════════════════════════════════
        //   ЛОГИКА ЗАКРЫТИЯ — TODO: реализовать
        // ════════════════════════════════════════════════════════════════════════════

        private void LogicClosePosition(List<Candle> candles)
        {
            List<Position> openPositions = _tab.PositionsOpenAll;

            for (int i = 0; openPositions != null && i < openPositions.Count; i++)
            {
                Position pos = openPositions[i];
                if (pos.State != PositionStateType.Open) continue;

                // TODO: добавить условия выхода по сигналу
                // Например:
                // if (pos.Direction == Side.Buy && /* сигнал разворота */ )
                //     _tab.CloseAtMarket(pos, pos.OpenVolume);
            }
        }

        // ════════════════════════════════════════════════════════════════════════════
        //   РАСЧЁТ ОБЪЁМА
        //
        //   Объём = (Баланс × VolumePercent%) / entryPrice
        //
        //   Например: баланс = 10 000 USDT, Volume Long = 2.5%
        //   → позиция = 10 000 × 0.025 = 250 USDT → делим на цену входа.
        //
        //   Если выбран актив "Prime" — берётся portfolio.ValueCurrent (весь портфель).
        //   Иначе — только остаток конкретного актива (USDT, RUB и т.д.).
        //
        //   Вызывать: decimal volume = CalcVolume(Side.Buy, entryPrice);
        // ════════════════════════════════════════════════════════════════════════════

        private decimal CalcVolume(Side side, decimal entryPrice)
        {
            if (_tab?.Security == null || _tab.Security.Lot <= 0) return 0;
            if (entryPrice <= 0) return 0;
            return GetVolume(side, entryPrice);
        }

        // ────────────────────────────────────────────────────────────────────────────

        private struct VolumeCalcCtx
        {
            public Side Side;
            public decimal EntryPrice;
            public decimal Balance;
            public decimal DepositPct;
            public decimal PosSize;
            public decimal Volume;
            public Security Sec;
            public string RejectReason;
        }

        private decimal GetVolume(Side side, decimal entryPrice)
        {
            VolumeCalcCtx ctx = new VolumeCalcCtx
            {
                Side = side,
                EntryPrice = entryPrice,
                RejectReason = "ok",
            };

            // --- Баланс ---
            ctx.Balance = GetAssetValue(_tab.Portfolio, _assetNameCurrent.ValueString);
            if (ctx.Balance <= 0) return Reject(ref ctx, "balance <= 0");

            // --- Размер позиции ---
            ctx.DepositPct = side == Side.Buy ? _curVolumeLong : _curVolumeShort;
            ctx.PosSize = ctx.Balance * (ctx.DepositPct / 100m);

            // --- Инструмент ---
            ctx.Sec = _tab.Security;
            if (ctx.Sec == null) return Reject(ref ctx, "sec == null");

            if (StartProgram != StartProgram.IsOsOptimizer &&
                StartProgram != StartProgram.IsTester)
            {
                if (ctx.Sec.State != SecurityStateType.Activ)
                    return Reject(ref ctx, $"state not active ({ctx.Sec.State})");

                if (ctx.Sec.PriceLimitHigh > 0 && entryPrice > ctx.Sec.PriceLimitHigh)
                    return Reject(ref ctx, $"entryPrice {entryPrice} > PriceLimitHigh {ctx.Sec.PriceLimitHigh}");

                if (ctx.Sec.PriceLimitLow > 0 && entryPrice < ctx.Sec.PriceLimitLow)
                    return Reject(ref ctx, $"entryPrice {entryPrice} < PriceLimitLow {ctx.Sec.PriceLimitLow}");

                if ((ctx.Sec.SecurityType == SecurityType.Futures || ctx.Sec.SecurityType == SecurityType.Option) &&
                    ctx.Sec.Expiration.Year > 1970 &&
                    ctx.Sec.Expiration < DateTime.Now)
                    return Reject(ref ctx, $"instrument expired (Expiration={ctx.Sec.Expiration:yyyy-MM-dd})");

                if (ctx.Sec.SecurityType == SecurityType.Bond &&
                    ctx.Sec.MaturityDate != DateTime.MinValue &&
                    ctx.Sec.MaturityDate < DateTime.Now.AddDays(_curBondDaysToMaturity))
                    return Reject(ref ctx, $"bond maturity too close ({ctx.Sec.MaturityDate:yyyy-MM-dd})");
            }

            // --- Расчёт объёма ---
            decimal mult = ctx.Sec.DecimalsVolume > 0 ? (decimal)Math.Pow(10, ctx.Sec.DecimalsVolume) : 1m;

            switch (_modeTrade.ValueString)
            {
                case "SPOT и LinearPerpetual":
                    if (ctx.Sec.SecurityType != SecurityType.CurrencyPair &&
                        ctx.Sec.SecurityType != SecurityType.Futures &&
                        ctx.Sec.SecurityType != SecurityType.None)
                        return Reject(ref ctx, $"wrong secType for SPOT ({ctx.Sec.SecurityType})");

                    if (ctx.Sec.UsePriceStepCostToCalculateVolume && ctx.Sec.PriceStep > 0 && ctx.Sec.PriceStepCost > 0)
                    {
                        decimal contractCost = entryPrice / ctx.Sec.PriceStep * ctx.Sec.PriceStepCost;
                        if (contractCost <= 0) return Reject(ref ctx, "contractCost <= 0");
                        ctx.Volume = Math.Floor(ctx.PosSize / contractCost * mult) / mult;
                    }
                    else
                    {
                        ctx.Volume = Math.Floor(ctx.PosSize / entryPrice * mult) / mult;
                    }
                    break;

                case "Stocks MOEX":
                    if (ctx.Sec.SecurityType != SecurityType.Stock &&
                        ctx.Sec.SecurityType != SecurityType.Fund &&
                        ctx.Sec.SecurityType != SecurityType.None)
                        return Reject(ref ctx, $"wrong secType for Stocks ({ctx.Sec.SecurityType})");
                    if (ctx.Sec.Lot <= 0) return Reject(ref ctx, "Lot <= 0");

                    ctx.Volume = Math.Floor(ctx.PosSize / entryPrice / ctx.Sec.Lot * mult) / mult;
                    break;

                case "Bonds MOEX":
                    if (ctx.Sec.SecurityType != SecurityType.Bond &&
                        ctx.Sec.SecurityType != SecurityType.None)
                        return Reject(ref ctx, $"wrong secType for Bonds ({ctx.Sec.SecurityType})");
                    if (ctx.Sec.Lot <= 0 || ctx.Sec.NominalCurrent <= 0)
                        return Reject(ref ctx, $"Lot={ctx.Sec.Lot} or NominalCurrent={ctx.Sec.NominalCurrent} <= 0");

                    decimal bondPrice = ctx.Sec.NominalCurrent * entryPrice / 100m;
                    if (bondPrice <= 0) return Reject(ref ctx, "bondPrice <= 0");
                    ctx.Volume = Math.Floor(ctx.PosSize / bondPrice / ctx.Sec.Lot * mult) / mult;
                    break;

                default:
                    return Reject(ref ctx, $"unknown mode '{_modeTrade.ValueString}'");
            }

            if (ctx.Volume <= 0) return Reject(ref ctx, "volume <= 0 after calculation");

            // --- Округление по шагу объёма ---
            if (ctx.Sec.VolumeStep > 0)
                ctx.Volume = Math.Floor(ctx.Volume / ctx.Sec.VolumeStep) * ctx.Sec.VolumeStep;

            // --- Проверка минимального объёма ---
            if (ctx.Sec.MinTradeAmount > 0)
            {
                decimal minVolume = ctx.Sec.MinTradeAmountType == MinTradeAmountType.C_Currency
                    ? ctx.Sec.MinTradeAmount / entryPrice
                    : ctx.Sec.MinTradeAmount;

                if (ctx.Volume < minVolume)
                    return Reject(ref ctx, $"volume={ctx.Volume} < minVolume={minVolume} (MinTradeAmount={ctx.Sec.MinTradeAmount} type={ctx.Sec.MinTradeAmountType})");
            }

            return LogVolume(ref ctx);
        }

        // Помечает контекст как отклонённый, логирует и возвращает 0.
        private decimal Reject(ref VolumeCalcCtx ctx, string reason)
        {
            ctx.Volume = 0;
            ctx.RejectReason = reason;
            return LogVolume(ref ctx);
        }

        private decimal LogVolume(ref VolumeCalcCtx ctx)
        {
            if (_tradeLogOnOff.ValueString == "On")
            {
                Security s = ctx.Sec;
                string log =
                $@" -GET VOLUME DEBUG
                SECURITY               = {s?.Name} | TYPE = {s?.SecurityType} | STATE = {s?.State}
                MODE                   = {_modeTrade.ValueString}
                SIDE                   = {ctx.Side}
                ------ ASSET / BALANCE ------
                ASSET                  = {_assetNameCurrent.ValueString}
                BALANCE                = {ctx.Balance:F6}
                ------ VOLUME ------
                VOLUME PCT             = {ctx.DepositPct:F4} %
                POS SIZE               = {ctx.PosSize:F6}
                SLIPPAGE %             = {_curSlippagePercent:F4} %
                ------ PRICE ------
                ENTRY PRICE            = {ctx.EntryPrice:F4}
                ------ INSTRUMENT ------
                LOT                    = {s?.Lot}
                DECIMALS VOL           = {s?.DecimalsVolume}
                VOLUME STEP            = {s?.VolumeStep}
                MIN TRADE AMOUNT       = {s?.MinTradeAmount} ({s?.MinTradeAmountType})
                PRICE STEP             = {s?.PriceStep}
                STEP COST              = {s?.PriceStepCost}
                EXPIRATION             = {s?.Expiration:yyyy-MM-dd}
                ------ RESULT ------
                VOLUME                 = {ctx.Volume}
                REJECT REASON          = {ctx.RejectReason}
                -";

                SendNewLogMessage(log, LogMessageType.System);
            }

            return ctx.Volume;
        }

        private decimal GetAssetValue(Portfolio portfolio, string assetName)
        {
            if (portfolio == null) return 0;

            // Prime = суммарная стоимость портфеля в валюте счёта (деньги + открытые позиции).
            // Вы можете выбрать как Prime, так и конкретный актив — например, USDT.
            // В первом случае объём рассчитывается от всего портфеля, во втором — только от этого актива.
            if (assetName == "Prime") return portfolio.ValueCurrent;

            List<PositionOnBoard> positions = portfolio.GetPositionOnBoard();
            if (positions == null) return 0;

            foreach (PositionOnBoard p in positions)
            {
                if (p.SecurityNameCode.Equals(assetName, StringComparison.OrdinalIgnoreCase))
                    return p.ValueCurrent;
            }

            return 0;
        }
    }
}