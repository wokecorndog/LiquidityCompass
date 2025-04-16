# 🧭 LiquidityCompass

> _"It doesn’t matter if you're steering a kayak, a yacht, or a battleship — if the tide goes out or the sea turns shallow, you're not going anywhere."_

**LiquidityCompass** is a real-time NinjaTrader 8 indicator designed to assess DOM (Depth of Market) liquidity conditions on the ES futures. If the market is "thin," you're exposed — this tool helps visualize that before it's too late.

---

## 📌 Why I Built This

This is my **first NinjaScript indicator**, and I know it’s not perfect. I made it to:

- Learn how to code in NinjaTrader
- Help myself better understand liquidity environments
- Invite others to help improve and build on it

Please feel free to fork it, suggest improvements, or report issues — any feedback is welcome.

---

## ⚙️ What It Does

LiquidityCompass monitors the **top N levels** on both the bid and ask sides of the ES DOM:

- Sums the resting limit orders (liquidity) at each level
- Calculates the average resting size across both sides
- Categorizes the DOM as:
  - 🟩 **THICK** (Avg ≥ 80)
  - 🟨 **MEDIUM** (Avg ≥ 40 and < 80)
  - 🟥 **THIN** (Avg < 40)

It displays this label directly on your chart, with optional debug output showing the raw bid/ask totals and average.

---

## 💡 How It Helps

- Helps you avoid placing trades during **thin** liquidity conditions
- Adds context to sudden price movement and volatility
- Gives a quick glance of DOM environment without needing to stare at the SuperDOM

---

## 🧪 Features

- ✅ **New:** Renders UI using `OnRender()` for better separation of logic and display
- ✅ **New:** Timeout buffer to smooth out flickering DOM levels (e.g., brief 0s on bid/ask)
- ✅ **New:** Optional logging of both raw DOM updates and summarized outputs
- ✅ Configurable DOM instrument (default: `ES 06-25`)
- ✅ Adjustable depth levels (default: 10)
- ✅ User-defined thresholds for THIN / MEDIUM / THICK
- ✅ Optional debug mode with real-time stats

---

## 🛠️ Requirements

- NinjaTrader 8 (`v8.1.3.1` recommended)
- Level II market data subscription (required for DOM access)
- Indicator **must** be running on a chart to receive market depth events

✅ Set **Calculate** to `On each tick` for the most accurate DOM responsiveness.

---

## 📥 Installation

1. Clone or download the repo
2. Open **NinjaTrader 8**
3. Go to `Tools > NinjaScript Editor`
4. Right-click `Indicators > Add > Existing Item`, and select `LiquidityCompass.cs`
5. Press `F5` to compile
6. Apply the indicator to your chart

---

## 🤝 Thanks & Credits

- 🙌 Special thanks to [@alighten-dev](https://github.com/alighten-dev) for UI cleanup and separating rendering into `OnRender()` — helped make the code easier to manage and maintain.

---

## 🧠 Contributing

Want to help improve it?

- Suggest refinements or better logic
- Help optimize for performance
- Contribute UI improvements or additional settings

---

## 🪪 License

[MIT License](LICENSE)
