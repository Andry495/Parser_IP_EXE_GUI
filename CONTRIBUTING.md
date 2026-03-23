# Contributing

Thanks for your interest in **Parser IP GUI**.

## How to contribute

1. Open an [issue](https://github.com/Andry495/Parser_IP_EXE_GUI/issues) for bugs or ideas.
2. Fork the repo, branch from `main`, keep pull requests focused.
3. Build before submitting:

   ```powershell
   dotnet build Parser_IP_EXE.sln -c Release
   ```

## Code

- C# with nullable reference types enabled.
- Prefer WinAPI for network tables; avoid unnecessary NuGet dependencies.

## Docs

If dump format or UI behavior changes, update **README.md** and **CHANGELOG.md**.
