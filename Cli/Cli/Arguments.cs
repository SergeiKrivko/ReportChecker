using CommandLine;

namespace ReportChecker.Cli;

public class Arguments
{
    [Value(0, HelpText = "Путь к основному файлу отчета", Required = false, Default = null)]
    public string? Path { get; init; } = null;

    [Option(longName: "logout", HelpText = "Выйти из аккаунта при запуске программы", Required = false,
        Default = false)]
    public bool LogOut { get; init; } = false;

    [Option(longName: "new", HelpText = "Создать новый отчет, даже если указанный файл уже был загружен ранее",
        Required = false, Default = false)]
    public bool NewReport { get; init; } = false;
}