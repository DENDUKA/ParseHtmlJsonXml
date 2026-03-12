# WordCounter Service

Простой сервис для подсчета частоты слов в HTML, JSON и XML документах по URL. 

## Стек
* **.NET 10** (ASP.NET Core Web API)
* **Clean Architecture** (разбиение на слои: Api -> Domain <- Infrastructure)
* **Serilog** (пишет логи в консоль и в файл `log.txt`)
* **Swashbuckle** (Swagger UI для вызова)

## Структура проекта
* `WordCounter.Api` — Точка входа. Тут контроллеры, DI и оркестрация (Application logic).
* `WordCounter.Domain` — Доменные модели и енамы.
* `WordCounter.Infrastructure` —

## Как запустить
Нужен .NET 10 SDK.

Открывается Swagger: [http://localhost:<port>/swagger](http://localhost:<port>/swagger)

## Использование API
Есть три ручки в контроллере `Analysis`, по одной на каждый формат:
* `POST /api/Analysis/html`
* `POST /api/Analysis/json`
* `POST /api/Analysis/xml`

json example :
http://jsonplaceholder.typicode.com/posts

xml example :
https://www.w3schools.com/xml/cd_catalog.xml
