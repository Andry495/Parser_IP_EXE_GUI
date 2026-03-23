# Синхронизация с GitHub

Инструкции для разработчиков и мейнтейнеров репозитория. В основном **README** проекта этого раздела нет — он относится только к Git/GitHub, а не к приложению **Parser IP — GUI**.

## Обычный цикл (код → GitHub)

Из корня репозитория:

```powershell
git add -A
git status
git commit -m "краткое описание изменений"
git push origin main
```

Если ветка ушла вперёд на сервере:

```powershell
git pull --rebase origin main
git push origin main
```

Или запустите **`scripts\git-sync.ps1`** (пушит `main`, перед этим делает `pull --rebase`; **не коммитит** за вас).

---

## SSH и `git push`

Репозиторий настроен на **SSH** (`git@github.com:...`). В этом проекте задано:

`core.sshCommand` → системный OpenSSH и ключ `id_ed25519_github`.

Если снова появится **`Permission denied (publickey)`**:

```powershell
git config core.sshCommand "C:/Windows/System32/OpenSSH/ssh.exe -i C:/Users/ВАШ_ЛОГИН/.ssh/id_ed25519_github -o IdentitiesOnly=yes"
```

Проверка ключа:

```powershell
ssh -T git@github.com
```

---

## GitHub CLI (`gh`) и релизы

`git push` **не использует** токен `gh`. Отдельно для **`gh release`**, **Issues API** и т.п. нужен валидный токен в keyring.

Если **`gh auth status`** пишет *token in keyring is invalid*:

```powershell
gh auth login -h github.com
```

или сброс и повторный вход:

```powershell
gh auth logout -h github.com -u Andry495
gh auth login -h github.com
```

После этого снова можно создавать релизы (см. `docs/RELEASING.md`).

---

## Для ассистента / CI

После изменений в коде: **собрать проект**, **закоммитить**, **`git push origin main`**, если нет ошибок и нет секретов в коммите.
