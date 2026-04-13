# TemplateRobotNoStop

Шаблон торгового робота для [OsEngine](https://github.com/AlexWan/OsEngine).

Объём позиции рассчитывается как **процент от депозита** — без привязки к стоп-лоссу. Подходит для стратегий, где выход управляется индикатором, трейлингом или временны́м фильтром, а не фиксированным стопом.

---

## Связанные шаблоны

- [TemplateRobotSL](https://github.com/si0683/osengine-trading-robot-template-sl) — версия с расчётом объёма через риск % и автоматическим стоп-лоссом

---

## Отличие от TemplateRobotSL

| | TemplateRobotSL | TemplateRobotNoStop |
|---|---|---|
| Объём | риск % / расстояние до стопа | депозит × % напрямую |
| Стоп-лосс | выставляется автоматически | не выставляется |
| Входные параметры CalcVolume | `(side, entry, stopPrice)` | `(side, entry)` |
| Плечо | косвенно через стоп | явно через % > 100 |

---

## Расчёт объёма

```
PosSize = Balance × (VolumePct / 100)
Volume  = PosSize / EntryPrice          // SPOT / LinearPerpetual
Volume  = PosSize / EntryPrice / Lot    // Stocks MOEX
Volume  = PosSize / BondPrice  / Lot    // Bonds MOEX
```

Примеры при балансе **10 000 USDT**:

| Volume Long (%) | PosSize | Плечо |
|---|---|---|
| 25% | 2 500 USDT | 0.25x |
| 100% | 10 000 USDT | 1x (без плеча) |
| 200% | 20 000 USDT | 2x |
| 500% | 50 000 USDT | 5x |

Максимальное значение параметра ограничено **2500%** (25x) — защита от случайной опечатки.

---

## Параметры

### Вкладка Base

| Параметр | По умолчанию | Описание |
|---|---|---|
| Non trade periods | — | Кнопка настройки неторгового времени |
| Regime | Off | `Off` / `On` / `LONG-POS` / `SHORT-POS` / `CLOSE-POS` |
| Time zone UTC | 4 | Часовой пояс для фильтра торгового времени |
| Trade Section | SPOT и LinearPerpetual | Тип инструмента (влияет на формулу объёма) |
| Deposit Asset | USDT | Актив депозита или `Prime` (весь портфель) |
| Trade debug log | Off | Подробный лог расчёта объёма в System log |
| Volume Long (%) | 2.5 | Доля депозита на лонг-вход (макс. 2500%) |
| Volume Short (%) | 2.5 | Доля депозита на шорт-вход (макс. 2500%) |
| Slippage (%) | 0.1 | Проскальзывание при выставлении ордера |
| Bond days to maturity | 30 | Мин. дней до погашения облигации для входа |

### Режимы Regime

| Значение | Поведение |
|---|---|
| `Off` | Робот выключен, никаких действий |
| `On` | Лонг и шорт разрешены |
| `LONG-POS` | Только лонг |
| `SHORT-POS` | Только шорт |
| `CLOSE-POS` | Только закрытие открытых позиций |

### Deposit Asset

Если выбран конкретный актив (например, `USDT`) — объём считается от остатка именно этого актива на счёте. Если выбран `Prime` — от `portfolio.ValueCurrent`, то есть от суммарной стоимости всего портфеля включая открытые позиции.

---

## Поддерживаемые секции (Trade Section)

| Секция | Типы инструментов |
|---|---|
| SPOT и LinearPerpetual | `CurrencyPair`, `Futures` |
| Stocks MOEX | `Stock`, `Fund` |
| Bonds MOEX | `Bond` |

---

## Как адаптировать под свою стратегию

1. Добавить параметры индикатора в блок `// TODO: параметры индикаторов`
2. Создать и подключить индикатор в конструкторе в блок `// TODO: индикаторы`
3. Реализовать условие входа в `LogicOpenPosition` — вызвать `CalcVolume(side, entry)` и выставить ордер
4. Реализовать условие выхода в `LogicClosePosition`
5. При необходимости обновить параметры индикатора в `OnParametrsChangeByUser`

Минимальный пример входа в лонг:

```csharp
decimal entry = _tab.PriceBestAsk;
if (entry <= 0) entry = lastPrice;

decimal volume = CalcVolume(Side.Buy, entry);
if (volume <= 0) return;

decimal slippage = entry * (_curSlippagePercent / 100m);
_tab.BuyAtLimit(volume, entry + slippage);
```

---

## Требования

- [OsEngine](https://github.com/AlexWan/OsEngine) последней версии
- .NET (версия по требованиям OsEngine)

---

## Лицензия

MIT

---

## Теги

`osengine` `trading-robot` `algorithmic-trading` `quant` `template` `bot` `csharp` `dotnet` `moex` `crypto` `spot` `futures` `stocks` `bonds` `volume-calculation` `deposit-percent` `leverage` `no-stop` `trading-bot` `robot-template`
