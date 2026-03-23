# Публикация релиза на GitHub

Если **`gh release create`** возвращает **401 / Bad credentials**, выполните `gh auth login -h github.com` или создайте релиз **вручную** на сайте (см. § 3 «Вручную»).

## 1. Подготовка артефактов (локально)

Из корня репозитория:

```powershell
.\scripts\publish-release.ps1
```

Скрипт создаёт папку `artifacts\` (в `.gitignore`, в git не коммитится):

- `ParserIpExeMonitor-win-x64-framework-dependent.zip` — один `.exe`, на ПК нужен **.NET 8 Desktop Runtime**.
- `ParserIpExeMonitor-win-x64-self-contained.zip` — один `.exe` со встроенным runtime (крупнее).

## 2. Создать тег и отправить на GitHub

```powershell
git tag -a v1.0.0 -m "v1.0.0"
git push origin v1.0.0
```

(Замените `1.0.0` на актуальную версию из `CHANGELOG.md` и `ParserIpExeMonitor.csproj`.)

## 3. Оформить Release на сайте GitHub

### Через GitHub CLI

```powershell
gh auth login -h github.com
gh release create v1.0.0 --title "Parser IP GUI v1.0.0" --notes-file CHANGELOG.md artifacts\*.zip
```

### Вручную

1. Репозиторий → **Releases** → **Draft a new release**.
2. **Choose a tag** — создать тег `v1.0.0` от `main` (или выбрать существующий после `git push`).
3. Заголовок, описание (можно скопировать из `CHANGELOG.md`).
4. Прикрепить ZIP из `artifacts\`.
5. **Publish release**.

## Проверка `gh`

Если видите «token in keyring is invalid»:

```powershell
gh auth login -h github.com
# или
gh auth logout -h github.com -u Andry495
gh auth login -h github.com
```
