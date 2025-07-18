# Vstupi.kz

Онлайн-платформа для подачи заявлений в вузы Казахстана, прохождения вступительных экзаменов и отслеживания грантов. Реализовано на ASP.NET Core с использованием Entity Framework Core, SQL Server, Tailwind CSS и JavaScript. Поддерживает авторизацию, управление заявлениями, экзамены с античит-механизмами и просмотр доступных специальностей.

## Основные функции

- **Авторизация**:
  - Регистрация пользователей с проверкой данных (имя, email, телефон, балл аттестата).
  - Вход с хешированием паролей (BCrypt).
  - Выход из системы с очисткой сессии.
- **Подача заявлений**:
  - Создание заявления с выбором факультета и специальности.
  - Валидация контактных данных (телефон, email, паспорт).
  - Ограничение на одно заявление на пользователя.
- **Онлайн-экзамены**:
  - Прохождение тестов с ограничением времени (30 минут).
  - Античит: блокировка при выходе из полноэкранного режима, потере фокуса или скрытии вкладки (до 3 попыток).
  - Отображение прогресса и результатов прошлых попыток.
- **Просмотр специальностей**:
  - Список специальностей с информацией о факультетах и доступных грантах.
  - Фильтрация специальностей по факультету через AJAX.
- **Управление данными**:
  - Хранение информации о пользователях, заявлениях, экзаменах и грантах в SQL Server.
  - Использование сессий для отслеживания авторизации и статуса экзаменов.

## Требования

- **ОС**: Windows/Linux/macOS
- **.NET**: .NET 6.0 или выше
- **СУБД**: Microsoft SQL Server
- **Браузер**: Современный браузер с поддержкой JavaScript (Chrome, Firefox, Edge)
- **Зависимости**:
  - Microsoft.EntityFrameworkCore
  - Microsoft.AspNetCore.Authentication.JwtBearer
  - BCrypt.Net-Next
  - Tailwind CSS (CDN)
  - Font Awesome (CDN)

## Установка

1. **Клонируйте репозиторий**:
   ```bash
   git clone https://github.com/TemhaN/Vstupi.kz.git
   cd Vstupi.kz

2. **Настройте базу данных**:
   - Убедитесь, что SQL Server установлен и запущен.
   - Создайте базу данных, например, `AdmissionSystem`.
   - Обновите строку подключения в `appsettings.json`:
     ```json
     {
       "ConnectionStrings": {
         "DefaultConnection": "Server=your_server;Database=AdmissionSystem;Integrated Security=True;TrustServerCertificate=true;"
       }
     }

3. **Примените миграции EF Core**:
   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update

4. **Установите зависимости**:
   - Убедитесь, что все NuGet-пакеты установлены:
     ```bash
     dotnet restore

5. **Соберите и запустите проект**:
   ```bash
   dotnet build
   dotnet run

## Использование

1. **Регистрация**:
   - Перейдите на `/Account/Register`.
   - Заполните форму (имя, фамилия, email, телефон, имя пользователя, пароль, балл аттестата).
   - После регистрации автоматически перенаправляетесь в личный кабинет.

2. **Вход**:
   - Перейдите на `/Account/Login`.
   - Введите имя пользователя и пароль.
   - При успешном входе перенаправляетесь в личный кабинет (`/Application/Index`).

3. **Подача заявления**:
   - В личном кабинете нажмите "Создать новую заявку" (если заявок нет).
   - Заполните данные (пол, адрес, паспорт, телефон, email, дата рождения).
   - Выберите факультет и специальность.
   - Подтвердите подачу заявления.

4. **Прохождение экзамена**:
   - Если экзамен назначен, нажмите "Начать экзамен" в личном кабинете.
   - Экзамен открывается в полноэкранном режиме (30 минут, до 3 попыток списывания).
   - Ответьте на все вопросы и подтвердите отправку.
   - Результаты отображаются в личном кабинете.

5. **Просмотр специальностей**:
   - Перейдите на `/Specialization/Index`.
   - Просмотрите список специальностей, факультетов и доступных грантов.

6. **Выход**:
   - Нажмите "Выйти" на `/Account/Logout` для завершения сессии.

## Структура проекта

- **Controllers**:
  - `AccountController`: Регистрация, вход, выход.
  - `ApplicationController`: Управление заявлениями, выбор специальностей.
  - `ExamController`: Логика экзаменов, античит, обработка ответов.
  - `SpecializationController`: Отображение списка специальностей.
  - `HomeController`: Главная страница и политика конфиденциальности.

- **Models**:
  - `Applicant`: Данные абитуриента (имя, email, телефон, паспорт).
  - `User`: Учетные данные пользователя (логин, хеш пароля, роль).
  - `AdmissionApplication`: Заявления (факультет, специальность, статус).
  - `Exam`, `ExamQuestion`, `ExamResult`, `UserExamAttempt`: Экзамены, вопросы, результаты и попытки.
  - `Specialization`, `Faculty`, `Grant`: Специальности, факультеты, гранты.
  - `ApplicationExam`: Связь заявлений с экзаменами.

- **Views**:
  - `Account`: `Login.cshtml`, `Register.cshtml`, `Logout.cshtml`, `LoggedOut.cshtml`.
  - `Application`: `Index.cshtml` (личный кабинет), `Create.cshtml` (форма заявления).
  - `Exam`: `Start.cshtml` (экзамен), `Blocked.cshtml` (блокировка за списывание).
  - `Specialization`: `Index.cshtml` (список специальностей).
  - `Home`: `Index.cshtml` (главная страница), `Privacy.cshtml`.

- **Конфигурация**:
  - `appsettings.json`: Строка подключения к базе данных и настройки JWT.
  - `Program.cs`: Настройка сервисов, сессий, аутентификации.

## Технологии

- **Язык**: C#
- **Фреймворк**: ASP.NET Core 6.0
- **UI**: Tailwind CSS, Font Awesome
- **ORM**: Entity Framework Core
- **СУБД**: Microsoft SQL Server
- **Аутентификация**: BCrypt для хеширования паролей, JWT для API, сессии
- **Клиентская логика**: JavaScript (античит, динамическая загрузка специальностей)

## Примечания

- Пароли хешируются с использованием BCrypt.
- Экзамены имеют ограничение по времени (30 минут) и античит-механизмы (3 попытки списывания).
- Заявления ограничены одним на пользователя.
- JWT настроен для API, но в текущей реализации используется сессионная авторизация.
- Для работы требуется стабильное интернет-соединение.

## 📸 Скриншоты

<div style="display: flex; flex-wrap: wrap; gap: 10px; justify-content: center;">
  <img src="https://github.com/TemhaN/Vstupi.kz/blob/master/AdmissionApplicant/Screenshots/1.png?raw=true" alt="Service App" width="30%">
  <img src="https://github.com/TemhaN/Vstupi.kz/blob/master/AdmissionApplicant/Screenshots/2.png?raw=true" alt="Service App" width="30%">
  <img src="https://github.com/TemhaN/Vstupi.kz/blob/master/AdmissionApplicant/Screenshots/3.png?raw=true" alt="Service App" width="30%">
  <img src="https://github.com/TemhaN/Vstupi.kz/blob/master/AdmissionApplicant/Screenshots/4.png?raw=true" alt="Service App" width="30%">
  <img src="https://github.com/TemhaN/Vstupi.kz/blob/master/AdmissionApplicant/Screenshots/5.png?raw=true" alt="Service App" width="30%">
  <img src="https://github.com/TemhaN/Vstupi.kz/blob/master/AdmissionApplicant/Screenshots/6.png?raw=true" alt="Service App" width="30%">
  <img src="https://github.com/TemhaN/Vstupi.kz/blob/master/AdmissionApplicant/Screenshots/7.png?raw=true" alt="Service App" width="30%">
  <img src="https://github.com/TemhaN/Vstupi.kz/blob/master/AdmissionApplicant/Screenshots/8.png?raw=true" alt="Service App" width="30%">
</div>      

## 🧠 Автор

**TemhaN**  
[GitHub профиль](https://github.com/TemhaN)

## 🧾 Лицензия

Проект распространяется под лицензией MIT.

## 📬 Обратная связь

Нашли баг или хотите предложить улучшение?
Создайте **issue** или отправьте **pull request** в репозиторий!
