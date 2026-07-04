using System;
using System.IO;

namespace ReportChecker.Studio;

public static class Config
{
    public static string ApplicationDeveloper => "SergeiKrivko";
    public static string ApplicationShortName => "ReportChecker";
    public static string ApplicationName => "ReportChecker Studio";

    public static string DataPath { get; } =
        Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ApplicationDeveloper,
            ApplicationShortName);
}