# Car Reseller Simulator (crs-unity)

Симулятор перекупа автомобилей: ищи объявления, осматривай машины, торгуйся,
чини и продавай дороже. Unity 6, русский и английский языки.

Играть в браузере: `https://<owner>.github.io/crs-unity/`

## Ветки

| Ветка | Назначение |
|---|---|
| `main` | Только рабочие версии. Каждый push собирается и выкладывается на GitHub Pages |
| `develop` | Текущая разработка. Каждый push прогоняет тесты и Web-сборку |
| `feature/*` | Отдельные задачи, вливаются в `develop` через pull request |

Коммиты — по [Conventional Commits](https://www.conventionalcommits.org/ru/v1.0.0/):
`feat: торг с продавцом`, `fix: цена после ремонта`, `chore: обновить CI`.

## CI/CD

| Workflow | Когда запускается | Что делает |
|---|---|---|
| `ci.yml` | push и pull request в `main`/`develop` | EditMode-тесты → Web-сборка → деплой на Pages (только `main`) |
| `release.yml` | тег `vX.Y.Z` | Сборка Web и Android, GitHub Release с `.zip` и `.apk` |

Сборка идёт через [GameCI](https://game.ci) в Docker-образах `unityci/editor`.

### Секреты репозитория

Settings → Secrets and variables → Actions:

| Секрет | Значение |
|---|---|
| `UNITY_EMAIL` | email аккаунта Unity |
| `UNITY_PASSWORD` | пароль аккаунта Unity |
| `UNITY_LICENSE` | содержимое файла `Unity_lic.ulf` |

### Выпуск версии

```bash
git checkout main && git merge develop
git tag v0.1.0
git push origin main --tags
```

## Структура

```
Assets/
  Scripts/Core/     игровая логика без зависимостей от Unity (CRS.Core)
  Tests/EditMode/   автотесты логики (CRS.Tests.EditMode)
  Localization/     таблицы переводов RU/EN
.github/workflows/  CI/CD
```
