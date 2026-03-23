# Parser IP — GUI

WinForms-приложение для Windows: выбор процесса, просмотр TCP-соединений, дамп удалённых IP в текстовый файл, запуск `.exe` с автодампом.

## Сборка

- Visual Studio 2022 или новее (рабочая нагрузка **Разработка классических приложений .NET**)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

Откройте `Parser_IP_EXE.sln`, конфигурация **Debug** → **F5**.

## Публикация одним `.exe`

```powershell
dotnet publish ParserIpExeMonitor\ParserIpExeMonitor.csproj -c Release -r win-x64 --self-contained false
```

Готовый файл: `ParserIpExeMonitor\bin\Release\net8.0-windows\win-x64\publish\ParserIpExeMonitor.exe`  
(на целевом ПК нужен установленный **.NET 8 Desktop Runtime**).

Профили для Visual Studio: `ParserIpExeMonitor\Properties\PublishProfiles\`.

## Права

Для чтения таблицы TCP с привязкой к PID может потребоваться запуск **от имени администратора**.

## Репозиторий

https://github.com/Andry495/Parser_IP_EXE_GUI
