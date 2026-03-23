# Parser IP — GUI

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/platform-Windows%20x64-0078D4?logo=windows)](https://github.com/Andry495/Parser_IP_EXE_GUI)
[![License](https://img.shields.io/github/license/Andry495/Parser_IP_EXE_GUI)](LICENSE)
[![Release](https://img.shields.io/github/v/release/Andry495/Parser_IP_EXE_GUI?sort=semver)](https://github.com/Andry495/Parser_IP_EXE_GUI/releases)

**Parser_IP_EXE_GUI** — настольное приложение для **Windows x64** на **C# / WinForms**: просмотр **сокетов выбранного процесса** (**TCP и UDP**, **IPv4 и IPv6**), **текстовый дамп** и **запуск `.exe`** с автологированием. Данные берутся из таблиц Windows (**iphlpapi**) с привязкой к **PID**.

Идея перекликается с утилитой на Python [Parser_IP_EXE](https://github.com/mazixs/Parser_IP_EXE); эта репозиторий — **отдельная реализация** с GUI под .NET **без NuGet** для сетевого слоя.

**Репозиторий:** [github.com/Andry495/Parser_IP_EXE_GUI](https://github.com/Andry495/Parser_IP_EXE_GUI)

---

## Содержание

| Документ | Назначение |
|----------|------------|
| **README.md** (этот файл) | Обзор, установка, использование, формат дампа, сборка |
| [**CHANGELOG.md**](CHANGELOG.md) | История версий |
| [**CONTRIBUTING.md**](CONTRIBUTING.md) | Участие в разработке |
| [**LICENSE**](LICENSE) | Лицензия MIT |
| [**docs/RELEASING.md**](docs/RELEASING.md) | Как собрать ZIP и опубликовать **Release** на GitHub |

**Текущая версия:** **1.0.0** (см. `ParserIpExeMonitor.csproj`, тег `v1.0.0`).

---

## Релизы на GitHub

1. Откройте [**Releases**](https://github.com/Andry495/Parser_IP_EXE_GUI/releases).
2. Скачайте архив (**Assets**):
   - **framework-dependent** — один `ParserIpExeMonitor.exe`, на ПК нужен [.NET 8 Desktop Runtime (win-x64)](https://dotnet.microsoft.com/download/dotnet/8.0).
   - **self-contained** — один `ParserIpExeMonitor.exe` с runtime (файл больше по размеру).

Если готовых архивов нет, соберите их локально: [**docs/RELEASING.md**](docs/RELEASING.md) или скрипт `scripts/publish-release.ps1`.

---

## Возможности

| Функция | Описание |
|--------|-----------|
| **Процессы** | Список процессов Windows, **поиск по имени**, обновление списка. |
| **Сокеты** | **TCP + UDP**, **IPv4 + IPv6**: протокол, локальный/удалённый endpoint, состояние; обновление **~каждые 2 с**. |
| **Дамп** | Текстовый файл **UTF-8**: снимки, `Proto` / `Local` / `Remote`, **`UNIQUE_IP`**, **`UNIQUE_UDP_BIND`**. |
| **Запуск .exe** | Путь, аргументы, **«Запустить и дампить»** — лог по PID дочернего процесса. |
| **UI** | Тёмная тема, карточки, строка состояния. |

---

## Ограничения

- Только сокеты **TCP/UDP**, видимые в **расширенных таблицах** Windows с **PID**. Не отображаются ICMP и прочий трафик без сокетов.
- Для **UDP** в таблице в основном **локальные привязки**; «откуда шлют UDP» в этом API **не показывается**.
- Не сниффер и не прокси — только то, что отдаёт **iphlpapi**.
- **Домены, пинг, Keenetic** из Python-версии **не реализованы**.

Подробнее: разделы ниже и [CHANGELOG.md](CHANGELOG.md).

---

## Требования

| Сценарий | Нужно |
|----------|--------|
| **Разработка** | Windows 10/11 x64, [SDK .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0) и/или VS 2022+ (рабочая нагрузка «Классические приложения .NET») |
| **Запуск FDD-сборки** | [.NET 8 Desktop Runtime win-x64](https://dotnet.microsoft.com/download/dotnet/8.0) |
| **Запуск self-contained** | Только Windows x64 |

---

## Быстрый старт (из исходников)

```powershell
git clone https://github.com/Andry495/Parser_IP_EXE_GUI.git
cd Parser_IP_EXE_GUI
dotnet run --project ParserIpExeMonitor\ParserIpExeMonitor.csproj
```

Или откройте **`Parser_IP_EXE.sln`** в Visual Studio → **F5**.

---

## Использование

1. Выберите процесс (поле **«Поиск по имени…»** при необходимости).
2. В таблице смотрите **сокеты** выбранного PID.
3. **«Начать дамп»** — укажите `.log`/`.txt`; **«Стоп»** — закрыть файл.
4. **«Запустить и дампить»** — `.exe` + аргументы, затем файл лога.

При пустом или неполном списке попробуйте запуск **от имени администратора**.

---

## Формат дампа

Файл — **текст UTF-8**, поля в строках снимка разделены **табуляцией**.

- Заголовки сессии: `=== Dump started ... ===`, `=== Dump stopped ... ===`
- Блоки: `--- Snapshot ... | rows: N ---`
- Строка записи: `timestamp`, `PID=`, `Proto=TCP|UDP`, `Local=`, `Remote=` (для UDP часто `—`), `State=`, `New=0|1`
- Дополнительно для новых ключей: `UNIQUE_IP`, `UNIQUE_UDP_BIND`

Примеры см. в этом README (ниже в исторических версиях документации) или в коде `Form1.WriteDumpSnapshot`.

---

## Сборка

```powershell
dotnet build Parser_IP_EXE.sln -c Release
```

## Публикация одним `.exe`

```powershell
# Нужен установленный .NET 8 на целевом ПК
dotnet publish ParserIpExeMonitor\ParserIpExeMonitor.csproj -c Release -r win-x64 --self-contained false

# Без установки .NET (крупнее)
dotnet publish ParserIpExeMonitor\ParserIpExeMonitor.csproj -c Release -r win-x64 --self-contained true -p:EnableCompressionInSingleFile=true
```

Выход: `ParserIpExeMonitor\bin\Release\net8.0-windows\win-x64\publish\`

В Visual Studio: **ПКМ по проекту → Publish** → профили в `Properties\PublishProfiles\`.

---

## Структура репозитория

```
Parser_IP_EXE/
├── LICENSE
├── CHANGELOG.md
├── CONTRIBUTING.md
├── README.md
├── Parser_IP_EXE.sln
├── scripts/
│   └── publish-release.ps1      # ZIP в artifacts/ (см. docs/RELEASING.md)
├── docs/
│   └── RELEASING.md
└── ParserIpExeMonitor/
    ├── Program.cs
    ├── Form1.cs / Form1.Designer.cs
    ├── AppTheme.cs / CardPanel.cs
    ├── TcpTableProvider.cs / UdpTableProvider.cs
    ├── NetTableReader.cs / NetConnectionInfo.cs
    ├── ParserIpExeMonitor.csproj
    └── Properties/PublishProfiles/
```

---

## Git и SSH (Windows)

Если `git push` по SSH падает с `Permission denied`, а `ssh -T git@github.com` работает:

```powershell
git config --global core.sshCommand "C:/Windows/System32/OpenSSH/ssh.exe -i C:/Users/ВАШ/.ssh/id_ed25519_github -o IdentitiesOnly=yes"
```

---

## Связь с Python-версией

[Parser_IP_EXE](https://github.com/mazixs/Parser_IP_EXE) (Python, GPL-3.0) — конфиг, пинг, домены, Keenetic.  
Данный проект — **самостоятельный** C# GUI; лицензия **MIT** ([LICENSE](LICENSE)).

---

## Лицензия

**MIT** — см. [LICENSE](LICENSE).

---

## Пример строк дампа

```text
=== Dump started 2026-03-23 14:30:00 ===
Process: chrome.exe (PID 12345)
--- Snapshot 2026-03-23 14:30:02 | rows: 2 ---
2026-03-23 14:30:02	PID=12345	Proto=TCP	Local=192.168.1.2:52341	Remote=93.184.216.34:443	State=Established	New=1
UNIQUE_IP	93.184.216.34
2026-03-23 14:30:02	PID=12345	Proto=UDP	Local=0.0.0.0:5353	Remote=—	State=UDP	New=1
UNIQUE_UDP_BIND	0.0.0.0:5353
=== Dump stopped 2026-03-23 14:35:00 ===
```
