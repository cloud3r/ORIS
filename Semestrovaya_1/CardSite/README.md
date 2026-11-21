\# CardSite



Веб-приложение — каталог карточек Dragon Ball Super Card Game: Fusion World.



\## Технологии



\- C# / .NET 10

\- HttpListener

\- PostgreSQL 18

\- Docker и Docker Compose

\- Собственный ORM

\- Собственный HTML-шаблонизатор MiniTemplateEngine

\- HTML, CSS, JavaScript



\## Требования



Для запуска необходимы:



\- Docker Desktop

\- Docker Compose



Устанавливать .NET и PostgreSQL на компьютер отдельно не требуется.



\## Запуск проекта



1\. Склонируйте репозиторий:



&#x20;  ```bash

&#x20;  git clone <repository-url>

&#x20;  ```



2\. Перейдите в корневую папку проекта.



3\. Создайте файл `.env` на основе `.env.example`:



&#x20;  ```bash

&#x20;  cp .env.example .env

&#x20;  ```



4\. Откройте `.env` и задайте собственные пароли:



&#x20;  ```env

&#x20;  POSTGRES\_PASSWORD=your\_database\_password

&#x20;  CARDSITE\_ADMIN\_USERNAME=admin

&#x20;  CARDSITE\_ADMIN\_PASSWORD=your\_admin\_password

&#x20;  ```



5\. Запустите приложение:



&#x20;  ```bash

&#x20;  docker compose up --build

&#x20;  ```



Docker автоматически:



\- Соберёт приложение CardSite.

\- Запустит контейнер PostgreSQL.

\- Создаст базу данных `cardsitedb`.

\- Выполнит SQL-скрипт `CardSiteDB.sql`.

\- Дождётся готовности базы данных.

\- Передаст приложению строку подключения.

\- Создаст первоначальную учётную запись администратора.

\- Запустит веб-сервер.



\## Доступ к приложению



После запуска приложение доступно по адресу:



http://localhost:8080



\## Административная панель



Логин и пароль администратора задаются в `.env`:



\- `CARDSITE\_ADMIN\_USERNAME`

\- `CARDSITE\_ADMIN\_PASSWORD`



Первоначальный администратор создаётся автоматически при запуске приложения, если в базе ещё нет администраторов.



\## Остановка приложения



Для остановки контейнеров:



```bash

docker compose down

```



Данные PostgreSQL при этом сохраняются.



\## Полный сброс базы данных



Чтобы удалить контейнеры вместе с сохранёнными данными PostgreSQL:



```bash

docker compose down -v

```



\*\*Внимание:\*\* эта команда удаляет базу данных и все сохранённые в ней данные. При следующем запуске база будет создана заново, а SQL-скрипт выполнится повторно.



\## Структура проекта



```text

CardSite/

├── CardSite/

├── MiniTemplateEngine/

├── CardSiteDB.sql

├── Dockerfile

├── .dockerignore

├── .gitignore

├── .env.example

├── docker-compose.yml

└── README.md

```

