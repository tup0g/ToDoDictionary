using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NodaTime;
using Serilog;

public enum PriorityLevel
{
    Low,
    Medium,
    High
}

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public Instant ReminderTime { get; set; }
    public bool IsCompleted { get; set; }
    public int ReminderBeforeMinutes { get; set; }  // Скільки хвилин до нагадування
    public PriorityLevel Priority { get; set; }  // Пріоритет
}

public class TaskManager
{
    private readonly List<TaskItem> _tasks;
    private int _nextId;

    public TaskManager()
    {
        _tasks = new List<TaskItem>();
        _nextId = 1;
    }

    // Додавання завдання
    public void AddTask(string title, string description, Instant reminderTime, int reminderBeforeMinutes, PriorityLevel priority)
    {
        var task = new TaskItem
        {
            Id = _nextId++,
            Title = title,
            Description = description,
            ReminderTime = reminderTime,
            IsCompleted = false,
            ReminderBeforeMinutes = reminderBeforeMinutes,
            Priority = priority
        };
        _tasks.Add(task);

        Log.Information("Додано завдання: {Title}. Індекс завдання: {Id}. Час нагадування: {ReminderTime}", title, task.Id, reminderTime);
        Console.WriteLine($"Завдання додано! Індекс завдання: {task.Id}. Час нагадування: {reminderTime}");
    }

    // Показати всі завдання з пріоритетами
    public void ShowTasksWithPriority()
    {
        foreach (var task in _tasks.OrderBy(t => t.Priority))
        {
            Console.WriteLine($"[{task.Id}] {task.Title} - {task.Description} - {task.Priority} - {(task.IsCompleted ? "Виконано" : "Очікує")} - Час нагадування: {task.ReminderTime}");
        }
    }

    // Фільтрація завдань за виконаним станом
    public List<TaskItem> GetTasksByStatus(bool isCompleted)
    {
        return _tasks.Where(t => t.IsCompleted == isCompleted).ToList();
    }

    // Перевірка нагадувань
    public void CheckReminders()
    {
        while (true)
        {
            var now = SystemClock.Instance.GetCurrentInstant();
            foreach (var task in _tasks)
            {
                var reminderTime = task.ReminderTime.Minus(Duration.FromMinutes(task.ReminderBeforeMinutes));
                if (!task.IsCompleted && reminderTime <= now)
                {
                    Console.WriteLine($"Нагадування: {task.Title} - {task.Description}");
                    task.IsCompleted = true;
                    Log.Information("Нагадано про завдання: {Title}", task.Title);
                }
            }
            Thread.Sleep(60000); // Перевірка кожну хвилину
        }
    }

    // Оновлення завдання
    public void UpdateTask(int id, string newTitle, string newDescription, Instant newReminderTime, int newReminderBeforeMinutes, PriorityLevel newPriority)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == id);
        if (task != null)
        {
            string oldTitle = task.Title;
            string oldDescription = task.Description;
            Instant oldReminderTime = task.ReminderTime;
            int oldReminderBeforeMinutes = task.ReminderBeforeMinutes;
            PriorityLevel oldPriority = task.Priority;

            task.Title = newTitle;
            task.Description = newDescription;
            task.ReminderTime = newReminderTime;
            task.ReminderBeforeMinutes = newReminderBeforeMinutes;
            task.Priority = newPriority;

            Log.Information("Завдання з індексом {Id} було змінено: {OldTitle} -> {NewTitle}, {OldDescription} -> {NewDescription}, {OldReminderTime} -> {NewReminderTime}, {OldReminderBeforeMinutes} -> {NewReminderBeforeMinutes}, {OldPriority} -> {NewPriority}",
                id, oldTitle, newTitle, oldDescription, newDescription, oldReminderTime, newReminderTime, oldReminderBeforeMinutes, newReminderBeforeMinutes, oldPriority, newPriority);
        }
    }

    // Отримання введеного користувачем часу
    public Instant GetUserInputDateTime()
    {
        Console.WriteLine("Введіть дату та час для нагадування (формат: dd.MM.yyyy HH:mm):");
        string dateTimeInput = Console.ReadLine();

        // Перевірка правильності формату
        if (DateTime.TryParseExact(dateTimeInput, "dd.MM.yyyy HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime dateTime))
        {
            return Instant.FromDateTimeUtc(dateTime.ToUniversalTime());
        }
        else
        {
            Console.WriteLine("Невірний формат дати та часу.");
            return SystemClock.Instance.GetCurrentInstant(); // Повертаємо поточний час за умовчанням
        }
    }
}

class Program
{
    static void Main()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        var manager = new TaskManager();

        // Запуск потоку для нагадувань
        var reminderThread = new Thread(manager.CheckReminders);
        reminderThread.Start();

        while (true)
        {
            Console.WriteLine("\nTo Do Dictionary");
            Console.WriteLine("1. Додати завдання");
            Console.WriteLine("2. Показати завдання");
            Console.WriteLine("3. Показати лише невиконані завдання");
            Console.WriteLine("4. Показати лише виконані завдання");
            Console.WriteLine("5. Оновити завдання");
            Console.WriteLine("6. Вийти");
            Console.Write("Оберіть опцію: ");

            string choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    Console.Write("Введіть назву завдання: ");
                    string title = Console.ReadLine();
                    Console.Write("Введіть опис завдання: ");
                    string description = Console.ReadLine();

                    var reminderTime = manager.GetUserInputDateTime(); // Отримуємо дату та час нагадування
                    Console.Write("Введіть кількість хвилин до нагадування: ");
                    int reminderBeforeMinutes = int.Parse(Console.ReadLine());

                    Console.WriteLine("Виберіть пріоритет завдання (Low, Medium, High): ");
                    string priorityInput = Console.ReadLine();
                    PriorityLevel priority = Enum.Parse<PriorityLevel>(priorityInput);

                    manager.AddTask(title, description, reminderTime, reminderBeforeMinutes, priority);
                    break;

                case "2":
                    manager.ShowTasksWithPriority(); // Показує всі завдання з пріоритетами
                    break;

                case "3":
                    var incompleteTasks = manager.GetTasksByStatus(false); // Невиконані
                    Console.WriteLine("Невиконані завдання:");
                    foreach (var task in incompleteTasks)
                    {
                        Console.WriteLine($"[{task.Id}] {task.Title} - {task.Description} - {task.Priority} - {(task.IsCompleted ? "Виконано" : "Очікує")} - Час нагадування: {task.ReminderTime}");
                    }
                    break;

                case "4":
                    var completedTasks = manager.GetTasksByStatus(true); // Виконані
                    Console.WriteLine("Виконані завдання:");
                    foreach (var task in completedTasks)
                    {
                        Console.WriteLine($"[{task.Id}] {task.Title} - {task.Description} - {task.Priority} - {(task.IsCompleted ? "Виконано" : "Очікує")} - Час нагадування: {task.ReminderTime}");
                    }
                    break;

                case "5":
                    Console.Write("Введіть ID завдання для оновлення: ");
                    if (int.TryParse(Console.ReadLine(), out int updateId))
                    {
                        Console.Write("Введіть нову назву: ");
                        string newTitle = Console.ReadLine();
                        Console.Write("Введіть новий опис: ");
                        string newDescription = Console.ReadLine();

                        var newReminderTime = manager.GetUserInputDateTime(); // Отримуємо новий час нагадування
                        Console.Write("Введіть нову кількість хвилин до нагадування: ");
                        int newReminderBeforeMinutes = int.Parse(Console.ReadLine());

                        Console.WriteLine("Виберіть новий пріоритет завдання (Low, Medium, High): ");
                        string newPriorityInput = Console.ReadLine();
                        PriorityLevel newPriority = Enum.Parse<PriorityLevel>(newPriorityInput);

                        manager.UpdateTask(updateId, newTitle, newDescription, newReminderTime, newReminderBeforeMinutes, newPriority);
                        Console.WriteLine("Завдання оновлено!");
                    }
                    break;

                case "6":
                    Environment.Exit(0);
                    break;

                default:
                    Console.WriteLine("Невідома команда.");
                    break;
            }
        }
    }
}