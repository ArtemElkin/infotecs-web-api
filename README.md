# Infotecs WebAPI

WebAPI приложение для работы с timescale данными результатов обработки.

## Технологии

- .NET 8.0
- Entity Framework Core
- PostgreSQL
- Swagger/OpenAPI

## Быстрый старт

### 1. Установка зависимостей

```bash
# .NET 8 SDK
sudo apt-get install -y dotnet-sdk-8.0

# PostgreSQL
sudo apt-get install -y postgresql postgresql-contrib
sudo systemctl start postgresql
sudo -u postgres psql -c "ALTER USER postgres PASSWORD 'postgres';"
sudo -u postgres psql -c "CREATE DATABASE infotecs_db;"
```

### 2. Настройка

Отредактируйте `Infotecs/appsettings.json` - укажите connection string к PostgreSQL.

### 3. Запуск

```bash
cd Infotecs
dotnet ef tool install --global dotnet-ef  # Если не установлен
dotnet ef migrations add InitialCreate
dotnet ef database update
dotnet run
```

Приложение доступно на `http://localhost:5000` (Swagger UI на корневом пути).

## API Endpoints

### 1. Загрузка CSV файла
**POST** `/api/data/upload`

Формат CSV: `Date;ExecutionTime;Value`

Валидация:
- Дата: не позже текущей и не раньше 01.01.2000
- Время выполнения ≥ 0
- Значение показателя ≥ 0
- Количество строк: 1-10,000

Автоматически вычисляются: дельта времени, средние значения, медиана, мин/макс.

### 2. Получение списка Results
**GET** `/api/data/results`

Query параметры (все опциональны):
- `fileName` - фильтр по имени файла
- `minDateFrom`, `minDateTo` - диапазон дат
- `avgValueFrom`, `avgValueTo` - диапазон среднего значения
- `avgExecutionTimeFrom`, `avgExecutionTimeTo` - диапазон среднего времени выполнения

### 3. Последние 10 значений по файлу
**GET** `/api/data/values/{fileName}`

Возвращает последние 10 значений, отсортированных по дате.

## Формат даты в CSV

Поддерживаются форматы:
- `yyyy-MM-ddTHH-mm-ss.ffffZ` (формат из задания)
- `yyyy-MM-ddTHH:mm:ss.ffffZ` (стандартный ISO)

Пример: `2024-01-15T10-30-45.1234Z`

## Запуск тестов

```bash
cd Infotecs.Tests
dotnet test
```

Тесты покрывают основную бизнес-логику: парсинг CSV, валидацию данных, вычисление статистики.

## Структура проекта

```
Infotecs/
├── Controllers/     # API контроллеры
├── Services/        # Бизнес-логика
├── Models/          # Модели данных
├── DTOs/            # Data Transfer Objects
├── Data/            # DbContext
└── Migrations/      # Миграции БД

Infotecs.Tests/
└── Services/        # Unit тесты
```
