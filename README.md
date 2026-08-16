# Crypto ML Signal

A complete, research-first Python baseline that downloads Binance spot OHLCV history,
creates causal market features, trains a three-class model (`BUY`, `HOLD`, `SELL`),
performs leakage-aware walk-forward validation, runs a strict cost-aware holdout
backtest, and emits a current signal from the latest completed candle.

Version 0.2 routes external calls through the local V2RayN HTTP proxy at
`http://127.0.0.1:10808`, writes detailed console and rotating file logs, and prints
training results as readable percentages and formatted numbers.

This is a learning and research system. It does **not** place orders. In the default
long-only mode, `SELL` means exit to cash; it does not open a short position.

## Run with one click in VS Code

Open `crypto-ml-signal.code-workspace`, run **Terminal → Run Task → 1. Setup Python
environment**, then open **Run and Debug**, select **1. Offline Demo: Train + Signal**, and
press `F5`. The complete beginner walkthrough is in
[RUN_IN_VSCODE.md](RUN_IN_VSCODE.md).

For a file-by-file and function-by-function explanation, read
[docs/CODE_WALKTHROUGH.md](docs/CODE_WALKTHROUGH.md).

## Why this design

- Completed candles only: the unfinished Binance candle is discarded.
- Causal features only: each feature uses the current or earlier candles.
- Future-return labels: `BUY` and `SELL` require a move beyond a configurable neutral band.
- Purging: the training boundary is separated from validation/test by the prediction horizon.
- Walk-forward CV: training always occurs before validation in time.
- Untouched final holdout: the last 20% is evaluated once.
- Realistic friction: fees and slippage are charged on every position change and
  terminal liquidation; buy-and-hold also pays entry and exit costs.
- Spot-first safety: signals and paper research, with no API keys and no execution code.

## Architecture

```text
Binance closed candles
        -> validation and CSV cache
        -> causal price/volume/calendar features
        -> 3-class future-return target
        -> purged walk-forward validation
        -> strict chronological holdout
        -> cost-aware long-only backtest
        -> saved model + BUY/HOLD/SELL JSON
```

## Quick start with real BTC/USDT data

Python 3.11 or newer is required.

```bash
python -m venv .venv
source .venv/bin/activate          # Windows: .venv\Scripts\activate
python -m pip install --upgrade pip
python -m pip install -e .

crypto-signal --config config.toml download
crypto-signal --config config.toml train
crypto-signal --config config.toml signal
```

Or run the complete flow:

```bash
crypto-signal --config config.toml run-all
```

No exchange API key is needed because v0.2 reads only public Binance spot candles.
Start V2RayN before a real-data command and confirm its local HTTP port is `10808`.

## Proxy configuration

All external API calls use the `[network]` section of `config.toml`:

```toml
[network]
proxy_url = "http://127.0.0.1:10808"
timeout_seconds = 30
```

The configured proxy is passed to both HTTP and HTTPS traffic through Python's
`ProxyHandler`. If you later have direct Binance access, set `proxy_url = "off"`.

## Logs and progress tracing

The CLI displays progress in the VS Code terminal and saves the same timestamped events
to `artifacts/logs/crypto_signal.log`. Demo logs are stored in
`artifacts/demo/logs/crypto_signal.log`.

The log shows:

- every 1,000-candle download page and its newest timestamp;
- CSV validation, row counts and time gaps;
- feature/label construction and class counts;
- every chronological validation fold and its metrics;
- evaluation and production model training durations;
- untouched-test accuracy and macro F1;
- backtest return, drawdown, costs and position changes;
- saved models, reports, charts and signals;
- full error tracebacks and local-CSV fallback behavior.

Set `level = "DEBUG"` in `[logging]` when you need request-level details. Log files rotate
at 5 MB by default, keeping three older copies.

## Human-readable and JSON output

Normal commands print readable results such as `Balanced accuracy: 38.42%` and
`Total return: +12.31%`. For another program that needs raw data, add `--json` before
the command:

```bash
crypto-signal --config config.toml --json train
```

## Fully offline demo

The archive includes deterministic synthetic demo data. It proves that the software
runs; it is not evidence that the strategy works in a real market.

```bash
crypto-signal --config config.demo.toml train
crypto-signal --config config.demo.toml signal --offline
```

Or run training and the offline signal together:

```bash
crypto-signal --config config.demo.toml demo
```

To recreate the demo CSV:

```bash
python scripts/generate_demo_data.py
```

## Outputs

After training, the configured artifact directory contains:

| File | Purpose |
|---|---|
| `model.joblib` | Production model trained on all labeled rows after evaluation |
| `metadata.json` | Config, data ranges, class counts, CV and holdout metrics |
| `REPORT.md` | Human-readable run summary |
| `holdout_predictions.csv` | Untouched test probabilities and signals |
| `holdout_backtest.csv` | Positions, turnover, costs, returns and equity |
| `equity_curve.png` | ML strategy versus buy-and-hold on the strict holdout |
| `latest_signal.json` | Latest signal, probabilities, price and model timestamp |

## Signal meaning

The model predicts whether the close price after `prediction_horizon` candles will be:

- `BUY`: above the current close by more than `label_threshold`;
- `SELL`: below the current close by more than `label_threshold`;
- `HOLD`: inside the neutral band.

A prediction becomes an actionable model signal only when its class probability is at
least `probability_threshold`. Otherwise the system emits `HOLD`. In long-only backtests,
`HOLD` keeps the current position and `SELL` exits to cash.

## Change the market or timeframe

Edit `config.toml`, for example:

```toml
[market]
symbol = "ETHUSDT"
interval = "4h"
start = "2020-01-01T00:00:00Z"
end = ""
raw_data_path = "data/ETHUSDT_4h.csv"
```

Supported intervals in v0.2 are `1m`, `3m`, `5m`, `15m`, `30m`, `1h`, `2h`,
`4h`, `6h`, `8h`, `12h`, `1d`, `3d`, and `1w`. Short intervals are much more
sensitive to latency, spread and slippage; begin with `1h` or `4h`.

Use fees that match your actual venue and account tier. The defaults are deliberately
conservative examples, not Binance fee claims.

## Tests

```bash
PYTHONPATH=src python -m unittest discover -s tests -v
```

Tests explicitly cover proxy routing, mixed ISO-8601 timestamps, human formatting,
feature causality, unknown future labels, purge gaps, long-only signal state, and costs.

## Documentation map

- [RUN_IN_VSCODE.md](RUN_IN_VSCODE.md): installation and one-click VS Code use.
- [docs/CODE_WALKTHROUGH.md](docs/CODE_WALKTHROUGH.md): every class, script, important
  function and the complete training process.
- [ROADMAP.md](ROADMAP.md): research-to-paper-trading development stages.
- [docs/RESEARCH.md](docs/RESEARCH.md): primary sources and design justification.
- [DISCLAIMER.md](DISCLAIMER.md): financial and software limitations.

## What not to conclude

A high historical Sharpe ratio or accuracy does not prove a profitable live strategy.
Do not keep changing settings until the final holdout looks good; that turns the holdout
into training data. Freeze a version, record every experiment, and require forward paper
trading before considering real capital. See [ROADMAP.md](ROADMAP.md) and
[docs/RESEARCH.md](docs/RESEARCH.md).
