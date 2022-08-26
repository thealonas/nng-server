# nng server

[![License badge](https://img.shields.io/badge/license-EUPL-blue.svg)](LICENSE)
[![GitHub issues](https://img.shields.io/github/issues/MrAlonas/nng-server)](https://github.com/MrAlonas/nng-server/issues)
[![Docker Build and Push](https://github.com/MrAlonas/nng-server/actions/workflows/docker.yml/badge.svg)](https://github.com/MrAlonas/nng-server/actions/workflows/docker.yml)

Скрипт для групп nng, автоматизирующий выдачу редакторов в группах, достигших 50 или 100 участников. Помимо этого, скрипт автоматически убирает заблокированные страницы из редакторов.

## Установка

Воспользуйтесь готовым [Docker-контейнером](https://github.com/orgs/MrAlonas/packages/container/package/nng-server).

## Настройка

### Переменные среды

* `DataURL` — Ссылка на [общий список](https://github.com/MrAlonas/nng#datajson)
* `UserToken` — Токен страницы, от которой выполняются действия
* `UpdateTimeInMinutes` **(10)** —  Интервал проверки последней группы
