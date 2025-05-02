# KanbanBoardAPI
На основе предоставленной структуры проекта и предыдущего кода, вот обновленный README.md файл для GitHub:

# Kanban Board API & Web Application

Полнофункциональное веб-приложение для управления задачами с бэкендом на ASP.NET Core и фронтендом на Razor Pages.

## 🚀 Особенности

- **Полная система аутентификации** (регистрация, вход, JWT токены)
- **Kanban доска** с тремя статусами задач
- **Drag-and-drop** интерфейс для управления задачами
- **RESTful API** для интеграции с другими сервисами
- **Entity Framework Core** для работы с базой данных
- **Адаптивный дизайн** на Bootstrap 5

## 📂 Структура проекта

```
Keen/
├── KanbanBoardAPI/
│   ├── Controllers/
│   │   ├── AuthController.cs       # Контроллер аутентификации
│   │   ├── TaskController.cs       # API для работы с задачами
│   │   └── HomeController.cs       # Базовый контроллер
│   ├── Data/
│   │   ├── AppDbContext.cs         # Контекст базы данных
│   │   └── Migrations/             # Миграции Entity Framework
│   ├── Models/
│   │   ├── KanbanTask.cs           # Модель задачи
│   │   ├── Role.cs                 # Модель ролей
│   │   └── User.cs                 # Модель пользователя
│   ├── Pages/
│   │   ├── Shared/                 # Общие компоненты
│   │   │   ├── _Layout.cshtml      # Основной макет
│   │   │   └── ...                 # Другие shared компоненты
│   │   ├── Index.cshtml            # Главная страница (Kanban доска)
│   │   └── ...                     # Другие страницы
│   ├── wwwroot/
│   │   ├── css/                    # Стили
│   │   │   └── site.css            # Основные стили
│   │   ├── js/                     # Скрипты
│   │   │   └── site.js             # Основная логика приложения
│   │   └── lib/                    # Сторонние библиотеки
│   ├── appsettings.json            # Конфигурация приложения
│   └── Program.cs                  # Точка входа
└── README.md                       # Этот файл
```

## 🛠 Технологии

- **Backend**:
  - ASP.NET Core 6.0
  - Entity Framework Core
  - JWT аутентификация
  - Swagger (если настроен)

- **Frontend**:
  - Razor Pages
  - Bootstrap 5
  - JavaScript (ES6+)
  - SortableJS для drag-and-drop

## ⚙️ Установка и запуск

1. **Требования**:
   - .NET 6.0 SDK
   - SQL Server (или другая поддерживаемая СУБД)

2. **Настройка базы данных**:
   - Измените строку подключения в `appsettings.json`
   - Выполните миграции:
     ```bash
     dotnet ef database update
     ```

3. **Запуск приложения**:
   ```bash
   dotnet run
   ```

4. **Доступ**:
   - Веб-интерфейс: `https://localhost:5001`
   - API: `https://localhost:5001/api/[endpoint]`

## 📌 Ключевые файлы

- `site.js` - Основная бизнес-логика фронтенда
- `TaskController.cs` - API для работы с задачами
- `AuthController.cs` - API для аутентификации
- `KanbanTask.cs` - Модель задачи
- `Index.cshtml` - Интерфейс Kanban доски

## 🌟 Скриншоты

![Kanban Board Interface](https://via.placeholder.com/800x500?text=Kanban+Board+Screenshot)
![Authentication Modal](https://via.placeholder.com/800x500?text=Auth+Modal+Screenshot)

## 🤝 Участие в разработке

1. Форкните репозиторий
2. Создайте ветку для вашей фичи (`git checkout -b feature/AmazingFeature`)
3. Сделайте коммит изменений (`git commit -m 'Add some AmazingFeature'`)
4. Запушьте в ветку (`git push origin feature/AmazingFeature`)
5. Откройте Pull Request

## 📜 Лицензия

MIT License. Смотрите файл `LICENSE` для подробностей.

---

**Автор**: [Mishka]  
**Версия**: 3.1.1  
**Дата**: 2025
