# Parser IP — GUI

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/platform-Windows%20x64-0078D4?logo=windows)](https://github.com/Andry495/Parser_IP_EXE_GUI)
[![License](https://img.shields.io/github/license/Andry495/Parser_IP_EXE_GUI)](LICENSE)
[![Release](https://img.shields.io/github/v/release/Andry495/Parser_IP_EXE_GUI?sort=semver)](https://github.com/Andry495/Parser_IP_EXE_GUI/releases)

## О проекте

**Parser IP — GUI** — утилита для **Windows x64**, которая показывает, **какие сетевые сокеты** использует выбранный **процесс**: протоколы **TCP и UDP**, адреса **IPv4 и IPv6**. Данные берутся из стандартных таблиц Windows (**iphlpapi**) с привязкой к **PID** — без установки драйверов и без сторонних сетевых библиотек.

Дополнительно можно **писать снимки в текстовый лог** и **запустить свой `.exe`** из программы, чтобы сразу вести дамп по PID этого процесса.

**Версия:** 1.0.0 · **Репозиторий:** [github.com/Andry495/Parser_IP_EXE_GUI](https://github.com/Andry495/Parser_IP_EXE_GUI)

---

## Возможности

- список процессов с **поиском по имени**;
- таблица сокетов выбранного процесса (**TCP / UDP**, **IPv4 / IPv6**), обновление примерно **каждые 2 секунды**;
- **дамп в файл** (UTF-8): снимки строк, отметка новых записей, строки `UNIQUE_IP` и `UNIQUE_UDP_BIND` где применимо;
- **запуск внешнего приложения** с аргументами и автоматическим логированием сокетов дочернего PID;
- интерфейс в **тёмной теме** (карточки, статус-строка).

---

## Системные требования

| Вариант | Что нужно на компьютере |
|--------|-------------------------|
| Сборка из исходников | Windows 10/11 x64, [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) и/или Visual Studio 2022+ (рабочая нагрузка «Разработка классических приложений .NET») |
| Готовый **framework-dependent** `.exe` | [.NET 8 Desktop Runtime (win-x64)](https://dotnet.microsoft.com/download/dotnet/8.0) |
| Готовый **self-contained** `.exe` | только Windows x64 (runtime встроен в файл, он больше по размеру) |

При **пустой или неполной** таблице сокетов имеет смысл запустить программу **от имени администратора**.

---

## Скачать

1. Откройте [**Releases**](https://github.com/Andry495/Parser_IP_EXE_GUI/releases).
2. В разделе **Assets** выберите архив:
   - **framework-dependent** — один `ParserIpExeMonitor.exe` (нужен .NET 8 Desktop на ПК);
   - **self-contained** — один крупный `.exe` без установки .NET.

Собрать архивы у себя: скрипт `scripts/publish-release.ps1` или инструкция в [**docs/RELEASING.md**](docs/RELEASING.md).

---

## Как пользоваться

1. Выберите процесс в списке (при необходимости отфильтруйте **поиском по имени**).
2. Ниже отобразятся **сокеты** этого PID.
3. **«Начать дамп»** — укажите файл `.log` или `.txt`; **«Стоп»** — завершить запись.
4. **«Запустить и дампить»** — укажите путь к `.exe` и при необходимости аргументы, затем файл лога.

Для **UDP** в системной таблице обычно видна **локальная привязка**; колонка удалённой стороны может быть **«—»**. Это ограничение источника данных, не ошибка программы.

---

## Формат файла дампа

Текст **UTF-8**, в строках снимка поля разделены **табуляцией**.

| Элемент | Назначение |
|---------|------------|
| `=== Dump started / stopped ===` | начало и конец сессии |
| `--- Snapshot ... \| rows: N ---` | блок одного снимка |
| поля `PID=`, `Proto=`, `Local=`, `Remote=`, `State=`, `New=` | одна строка таблицы на момент снимка |
| `UNIQUE_IP` | новый удалённый IP (для подходящих TCP-записей) |
| `UNIQUE_UDP_BIND` | новая локальная UDP-привязка |

**Пример:**

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

---

## Ограничения

Отображается только то, что отдаёт **Windows** для **TCP/UDP** с указанием **PID**. **Не** является сниффером пакетов и **не** показывает весь сетевой стек целиком (например, **ICMP** без сокетов сюда не попадает). Функции вроде **пинга, резолва доменов, команд для Keenetic** из других проектов с похожей идеей **здесь нет** — только монитор сокетов и текстовый дамп.

---

## Сборка из исходников

```powershell
git clone https://github.com/Andry495/Parser_IP_EXE_GUI.git
cd Parser_IP_EXE_GUI
dotnet run --project ParserIpExeMonitor\ParserIpExeMonitor.csproj
```

Или откройте **`Parser_IP_EXE.sln`** в Visual Studio и нажмите **F5**.

**Release-сборка:**

```powershell
dotnet build Parser_IP_EXE.sln -c Release
```

**Один файл `.exe` для распространения** (`Publish`):

```powershell
dotnet publish ParserIpExeMonitor\ParserIpExeMonitor.csproj -c Release -r win-x64 --self-contained false
dotnet publish ParserIpExeMonitor\ParserIpExeMonitor.csproj -c Release -r win-x64 --self-contained true -p:EnableCompressionInSingleFile=true
```

Готовый файл: `ParserIpExeMonitor\bin\Release\net8.0-windows\win-x64\publish\ParserIpExeMonitor.exe`  
В Visual Studio: **ПКМ по проекту → Publish** — профили в `ParserIpExeMonitor\Properties\PublishProfiles\`.

---

## Структура репозитория (кратко)

```
Parser_IP_EXE.sln          — решение Visual Studio
ParserIpExeMonitor/        — исходники приложения (WinForms, WinAPI iphlpapi)
LICENSE, CHANGELOG.md      — лицензия MIT и история версий
docs/                      — релизы, синхронизация с Git (для мейнтейнеров)
scripts/                   — publish-release.ps1, git-sync.ps1
```

---

## Прочая документация

| Файл | Для кого |
|------|----------|
| [CHANGELOG.md](CHANGELOG.md) | список изменений по версиям |
| [CONTRIBUTING.md](CONTRIBUTING.md) | участие в разработке |
| [docs/RELEASING.md](docs/RELEASING.md) | публикация релиза и артефактов |
| [docs/GITHUB_SYNC.md](docs/GITHUB_SYNC.md) | работа с Git / SSH / `gh` (не относится к пользователям программы) |

---

## Лицензия

Проект распространяется под лицензией **MIT** — см. файл [LICENSE](LICENSE).

Похожий по смыслу инструмент на Python — [Parser_IP_EXE](https://github.com/mazixs/Parser_IP_EXE) (другой код, лицензия **GPL-3.0**). Этот репозиторий — **отдельная** реализация на C#.
