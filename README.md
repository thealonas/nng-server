# nng server

[![License badge](https://img.shields.io/badge/license-EUPL-blue.svg)](LICENSE)
[![GitHub issues](https://img.shields.io/github/issues/MrAlonas/nng-server)](https://github.com/MrAlonas/nng-server/issues)
[![Docker Build and Push](https://github.com/MrAlonas/nng-server/actions/workflows/docker.yml/badge.svg)](https://github.com/MrAlonas/nng-server/actions/workflows/docker.yml)

Скрипт для групп nng, автоматизирующий выдачу редакторов в группах, достигших 50 или 100 участников. Помимо этого, скрипт автоматически убирает заблокированные страницы из редакторов, очищает статусы и актуализирует чёрный список.

## Установка

Воспользуйтесь готовым [Docker-контейнером](https://github.com/orgs/MrAlonas/packages/container/package/nng-server).

## Настройка

### Переменные среды

* `REDIS_URL` — Ссылка на базу данных
