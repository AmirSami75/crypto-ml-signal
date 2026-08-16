# Roadmap: from research project to trustworthy signals

The goal is not “predict every candle.” The goal is to build a reproducible decision
system that can abstain (`HOLD`), survives costs, and is proven on data it never saw.

## Phase 0 — Define the contract (day 1)

Freeze these choices before looking at results:

- Venue and instrument: Binance spot, initially `BTCUSDT`.
- Timeframe: `1h` first; test `4h` only as a separately logged experiment.
- Decision: long or cash. No leverage and no shorting in v0.2.
- Forecast horizon: six candles.
- Cost assumption: fee plus slippage on every position change.
- Success metrics: holdout return after costs, maximum drawdown, turnover, exposure,
  Sharpe, balanced accuracy, and macro F1.

**Gate:** the experiment configuration is committed before the final holdout is opened.

## Phase 1 — Data foundation (days 2–4)

1. Download public Binance closed candles with pagination.
2. Validate ordering, duplicates, positive prices, OHLC relationships, and time gaps.
3. Save raw data unchanged and record the exact date range.
4. Decide how to handle any gaps before modeling; never silently forward-fill prices.
5. Add two other liquid assets (`ETHUSDT`, then one untouched asset) only for robustness,
   not for selecting the prettiest result.

**Gate:** repeated downloads are deterministic for the same completed time range, and
data-quality checks pass.

## Phase 2 — Non-ML baselines (days 5–7)

Before trusting ML, implement and record:

- buy-and-hold;
- always-cash;
- a simple moving-average trend rule;
- a previous-return or majority-class prediction baseline.

The ML model must beat useful baselines after identical costs, not merely achieve more
than 50% accuracy.

**Gate:** one report compares all baselines on identical periods and assumptions.

## Phase 3 — ML baseline in this repository (week 2)

1. Use only causal returns, trend, volatility, candle, volume, and calendar features.
2. Label moves beyond a neutral band as `BUY`/`SELL`, otherwise `HOLD`.
3. Use `TimeSeriesSplit` plus a gap equal to the forecast horizon.
4. Keep the latest 20% completely untouched until the design is frozen.
5. Train histogram gradient boosting with conservative fixed complexity.
6. Convert probabilities to signals with an abstention threshold.
7. Backtest on the strict holdout with fees, slippage, turnover and next-bar returns.

**Gate:** all leakage tests pass, the holdout was opened once, and results are saved even
if they are bad.

## Phase 4 — Robustness, not parameter hunting (weeks 3–4)

Run a small predeclared test matrix:

| Check | Pass condition |
|---|---|
| Time stability | No single month contributes most of the profit |
| Cost stress | Result remains acceptable at 1.5x and 2x assumed friction |
| Threshold stability | Nearby thresholds do not reverse the conclusion |
| Asset robustness | Directionally similar behavior on BTC and ETH |
| Regime behavior | Bull, bear and sideways windows are reported separately |
| Benchmark | ML adds value versus buy-and-hold at comparable drawdown/exposure |

Keep an experiment ledger: hypothesis, exact config, date, result, and decision. Count
failed trials. Repeatedly selecting the best of many variants is backtest overfitting.

**Gate:** performance is not dependent on one asset, month, cost guess, or exact setting.

## Phase 5 — Forward paper trading (minimum 8–12 weeks)

Freeze the model and schedule signal generation only after each candle is confirmed closed.
Record:

- forecast timestamp and model version;
- input candle freshness;
- signal and all class probabilities;
- simulated fill, fee, slippage and position;
- data/API failures and missed runs;
- realized return and drawdown.

Do not retrain because of a few losses. Establish a fixed retraining cadence, such as
monthly, and compare the frozen model with the candidate before promotion.

**Gate:** enough independent signals exist, realized paper slippage is understood, no
operational gaps are hidden, and forward performance is consistent with risk limits.

## Phase 6 — Better data and models (months 3–5)

Add one source at a time and require incremental out-of-sample value:

1. Cross-asset context: BTC, ETH, stablecoin volume, total-market proxy.
2. Market microstructure: spread, order-book imbalance and trade imbalance.
3. Derivatives context: funding, open interest and basis—only if the venue permits access.
4. On-chain or news/sentiment data with strict publication timestamps.
5. Probability calibration and drift monitoring.
6. Model comparison: regularized logistic regression, gradient boosting, then sequence
   models. LSTM/Transformers earn inclusion only if they beat the simple model under the
   same walk-forward protocol.

**Gate:** every added source has timestamp provenance and improves multiple forward folds.

## Phase 7 — Alerts and operations (months 4–6)

Build a read-only service before any trading integration:

- scheduled job after candle close;
- JSON/SQLite signal log;
- Telegram, email or dashboard alerts;
- health check for stale candles and missing data;
- model/data drift report;
- idempotency so a candle creates at most one signal;
- monitoring for unusually high turnover or probability distribution changes.

**Gate:** the alert service runs reliably for at least one month without duplicate or stale
signals.

## Phase 8 — Optional execution, only after the gates

Execution is a separate security-critical project. If it is ever added:

- use an exchange subaccount and an API key with trading permission but no withdrawals;
- start with the minimum order size and spot only;
- enforce maximum position, maximum daily loss, maximum drawdown and maximum orders;
- reject stale signals and abnormal spreads;
- reconcile exchange positions after every action;
- include a manual kill switch and default to no trade on any error;
- never log secrets or put keys in source control.

Moving to real capital is a personal financial decision, not a software milestone. Crypto
markets are volatile, platforms can fail, and no signal has guaranteed value.
