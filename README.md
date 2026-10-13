# FinTrackBY

[![build](https://github.com/gordon0056/FinTrackBY/actions/workflows/build.yml/badge.svg)](https://github.com/gordon0056/FinTrackBY/actions/workflows/build.yml)

Read in: [English](#-english) | [Русский](#-русский)

---

## English

WPF desktop app for viewing National Bank of Belarus exchange rates (`api.nbrb.by/exrates`).
The app loads the currency list and rate dynamics, shows them in a table and on a chart, converts amounts with cross-rate through BYN, and works offline from the last saved JSON file.

### Features

* Rates table with code, name, rate, change vs previous day
* History loading for a custom period via dialog with date and currency selection
* Save rates to JSON, open it back with editable cells
* Rate dynamics chart with period switch (`1W`, `1M`, `3M`, `6M`, `1Y`) and hover tracker
* Currency converter with cross-rate through BYN
* Dark and light themes with persistent setting
* Offline mode: shows last saved data when network is unavailable

---

### Dependencies

* **Visual Studio 2022** with `.NET desktop development` workload
* **.NET Framework 4.7.2** (target framework, Windows-only)
* **NuGet packages** (restored automatically): `Newtonsoft.Json 13.0.3`, `OxyPlot.Core 2.1.2`, `OxyPlot.Wpf 2.1.2`, `MSTest.TestFramework 3.11.1`, `MSTest.TestAdapter 3.11.1`

---

### Build

#### Visual Studio

```cmd
Open FinTrackBY.sln in Visual Studio 2022, press F5.
NuGet restores Newtonsoft.Json and OxyPlot on first build.
```

#### MSBuild (Release)

```cmd
nuget restore FinTrackBY.sln
msbuild FinTrackBY.sln /p:Configuration=Release /p:Platform="Any CPU"
```

---

### Tests

Unit tests use **MSTest v2** and drive the services through a stubbed NBRB API client, so they need no network access.
CI runs the same tests on every push and pull request and uploads the `results.trx` report as the `test-results` artifact.

#### Visual Studio

Open `FinTrackBY.sln` and use `Test` -> `Run All Tests`.

#### Command line

```cmd
nuget restore FinTrackBY.sln
msbuild FinTrackBY.sln /p:Configuration=Release /p:Platform="Any CPU"
vstest.console.exe Tests\bin\Release\FinTrackBY.Tests.dll /logger:"trx;LogFileName=results.trx"
```

`vstest.console.exe` ships with Visual Studio and lives in
`C:\Program Files\Microsoft Visual Studio\2022\<edition>\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe`.

---

### Quick Start

1. Run the app, it loads today's rates from NBRB.
2. Use `Load history` to pick dates and currencies for a custom period.
3. Use `Save` / `Open` to store rates in a JSON file and work offline.

---

### Example rates.json

```json
[
  {
    "CurId": 431,
    "Date": "2026-07-24T00:00:00",
    "Abbreviation": "USD",
    "Name": "Доллар США",
    "OfficialRate": 2.89,
    "Scale": 1,
    "ChangePercent": 0.12
  }
]
```

---

### Project Layout

```text
FinTrackBY/Models    - NBRB DTOs and UI records
FinTrackBY/Services  - API client, repositories, JSON storage, converter
FinTrackBY/ViewModels - MVVM layer, chart and converter logic
FinTrackBY/Views     - main window, table, chart, converter, dialogs
FinTrackBY/Themes    - dark and light resource dictionaries
Tests/Unit           - MSTest checks for DTOs, repository, storage, services and converter
```

---

### Error Handling

| Case | Behavior |
| ---- | -------- |
| `No network` | shows last saved `rates.json`, status message about offline mode |
| `Empty rate` | cell renders empty, converter returns no result |
| `Missing rate for date` | converter shows "no rate for selected date" |

[⬆ Back to top](#fintrackby)

---

## Русский

WPF-приложение для просмотра курсов валют Национального банка Беларуси (`api.nbrb.by/exrates`).
Загружает список валют и динамику курсов, показывает в таблице и на графике, конвертирует суммы кросс-курсом через BYN и работает офлайн из последнего сохраненного JSON-файла.

### Возможности

* Таблица курсов: код, название, курс, изменение к прошлому дню
* Загрузка истории за произвольный период через диалог с датами и валютами
* Сохранение в JSON и открытие обратно с редактируемыми ячейками
* График динамики с периодами (`1Н`, `1М`, `3М`, `6М`, `1Г`) и ховер-трекером
* Конвертер валют кросс-курсом через BYN
* Темная и светлая темы с сохранением настройки
* Офлайн-режим: показывает последние сохраненные данные без сети

---

### Зависимости

* **Visual Studio 2022** с workload `.NET desktop development`
* **.NET Framework 4.7.2** (целевой фреймворк, только Windows)
* **NuGet-пакеты** (подтягиваются автоматически): `Newtonsoft.Json 13.0.3`, `OxyPlot.Core 2.1.2`, `OxyPlot.Wpf 2.1.2`, `MSTest.TestFramework 3.11.1`, `MSTest.TestAdapter 3.11.1`

---

### Сборка

#### Visual Studio

```cmd
Открыть FinTrackBY.sln в Visual Studio 2022, нажать F5.
NuGet подтянет Newtonsoft.Json и OxyPlot при первой сборке.
```

#### MSBuild (Release)

```cmd
nuget restore FinTrackBY.sln
msbuild FinTrackBY.sln /p:Configuration=Release /p:Platform="Any CPU"
```

---

### Тесты

Юнит-тесты на **MSTest v2**: сервисы проверяются через стаб API-клиента НБРБ, поэтому сеть не нужна.
CI запускает эти же тесты на каждом push и pull request и выкладывает отчет `results.trx` в артефакт `test-results`.

#### Visual Studio

Открыть `FinTrackBY.sln`, `Тест` -> `Запустить все тесты`.

#### Командная строка

```cmd
nuget restore FinTrackBY.sln
msbuild FinTrackBY.sln /p:Configuration=Release /p:Platform="Any CPU"
vstest.console.exe Tests\bin\Release\FinTrackBY.Tests.dll /logger:"trx;LogFileName=results.trx"
```

`vstest.console.exe` входит в состав Visual Studio и лежит в
`C:\Program Files\Microsoft Visual Studio\2022\<издание>\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe`.

---

### Быстрый старт

1. Запустить приложение, оно загрузит курсы на сегодня из НБРБ.
2. Кнопка `Загрузить историю` — выбор дат и валют за период.
3. Кнопки `Сохранить` / `Открыть` — работа с JSON-файлом и офлайн.

---

### Пример rates.json

```json
[
  {
    "CurId": 431,
    "Date": "2026-07-24T00:00:00",
    "Abbreviation": "USD",
    "Name": "Доллар США",
    "OfficialRate": 2.89,
    "Scale": 1,
    "ChangePercent": 0.12
  }
]
```

---

### Структура проекта

```text
FinTrackBY/Models    - DTO НБРБ и записи для UI
FinTrackBY/Services  - API-клиент, репозитории, JSON-хранилище, конвертер
FinTrackBY/ViewModels - MVVM-слой, логика графика и конвертера
FinTrackBY/Views     - главное окно, таблица, график, конвертер, диалоги
FinTrackBY/Themes    - темная и светлая темы
Tests/Unit           - MSTest-проверки DTO, репозитория, хранилища, сервисов и конвертера
```

---

### Обработка ошибок

| Случай | Поведение |
| ------ | --------- |
| `Нет сети` | показывает последний `rates.json`, статус про офлайн |
| `Пустой курс` | ячейка пустая, конвертер без результата |
| `Нет курса на дату` | конвертер пишет "нет курса на выбранную дату" |

[⬆ Наверх](#fintrackby)


