# Run the project in VS Code

You do not need to type the Python commands manually. The project contains prepared VS
Code tasks and Run/Debug buttons for Windows, macOS and Linux.

## First-time setup

1. Install Python 3.11 or 3.12 from [python.org](https://www.python.org/downloads/).
   On Windows, enable **Add Python to PATH** during installation.
2. Install [Visual Studio Code](https://code.visualstudio.com/).
3. Extract the project ZIP.
4. Open the extracted `crypto-ml-signal` folder in VS Code. Do not open only an
   individual Python file.
5. If VS Code asks, install the recommended Python, Pylance and Python Debugger
   extensions.
6. Open the VS Code menu **Terminal → Run Task**.
7. Select **1. Setup Python environment** and wait until installation finishes.
8. Press `Ctrl+Shift+P` (`Cmd+Shift+P` on macOS), choose **Python: Select
   Interpreter**, and select the interpreter inside `.venv`.

You only need to complete the setup task once.

## Safest first run: offline demonstration

1. Open **Run and Debug** from the left sidebar or press `Ctrl+Shift+D`.
2. At the top, select **1. Offline Demo: Train + Signal**.
3. Click the green triangle or press `F5`.

The program trains on bundled **synthetic** candles, runs validation and a backtest, then
prints a sample `BUY`, `HOLD` or `SELL` result. This proves that Python, the model and the
VS Code configuration work without downloading real market data.

The terminal first shows the training metrics as percentages and then a signal like this:

```text
LATEST RESEARCH SIGNAL
----------------------
Market:        DEMOUSDT (1h)
Signal:        HOLD
SELL chance:   41.00%
HOLD chance:   35.00%
BUY chance:    24.00%
Min confidence: 50.00%
```

The exact numbers can differ after dependency updates. Demo results are not trading
evidence.

## Run with real BTC/USDT history

After the offline demo works:

1. Start V2RayN and confirm its local HTTP proxy is `127.0.0.1:10808`.
2. Open **Run and Debug**.
3. Select **3. Real BTC: Download + Train + Signal**.
4. Press `F5`.

This configuration:

1. downloads completed hourly BTC/USDT candles from Binance;
2. builds the 38 model features;
3. trains and validates the model;
4. backtests the untouched final period;
5. saves the model and reports;
6. requests recent completed candles and prints the newest signal.

No Binance API key is required. An internet connection and access to Binance's public
market-data endpoint are required.

## Available Run/Debug choices

| VS Code choice | What it does |
|---|---|
| `1. Offline Demo: Train + Signal` | Complete safe synthetic demonstration |
| `2. Offline Demo: Latest Signal Only` | Uses the already-trained demo model |
| `3. Real BTC: Download + Train + Signal` | Complete real-data research flow |
| `4. Real BTC: Download Data Only` | Only saves historical candles |
| `5. Real BTC: Train Existing Data` | Trains without downloading again |
| `6. Real BTC: Latest Signal Only` | Uses the saved BTC model for a new signal |
| `7. Run Unit Tests` | Checks leakage, targets, validation gaps and backtest costs |

## Where the results appear

Real BTC results are written to `artifacts/`. Offline demo results are written to
`artifacts/demo/`.

Important files:

- `REPORT.md`: readable performance summary;
- `equity_curve.png`: model versus buy-and-hold chart;
- `metadata.json`: validation and holdout metrics;
- `holdout_backtest.csv`: every simulated position, cost and return;
- `latest_signal.json`: newest signal and probabilities;
- `model.joblib`: trained model;
- `logs/crypto_signal.log`: timestamped trace of downloads, folds, training and errors.

## Common problems

### `No module named crypto_signal`

Run **Terminal → Run Task → 1. Setup Python environment**, then select the `.venv`
interpreter.

### PowerShell blocks activation

The prepared Windows tasks call `.venv\Scripts\python.exe` directly and do not require
manual activation. Run the task instead of typing `activate`.

### Binance connection error

First run the offline demo. If that succeeds, the Python project is working and the problem
is network access to Binance. Confirm V2RayN is running, its HTTP listener is on port `10808`,
and `[network].proxy_url` matches it. A SOCKS-only listener cannot be used as this HTTP proxy.

### Timestamp says `doesn't match format`

Version 0.2 accepts ISO-8601 timestamps both with and without fractional
seconds. Confirm that VS Code opened the newest project version and rerun the training
configuration.

### A signal says `SELL`

The default configuration is long-only. `SELL` means exit BTC and stay in cash; it does not
open a short or place a real exchange order.
