using System.IO.Compression;
using System.Text;
using FluentAssertions;
using ReportChecker.SourceProviders.GoogleDrive;

namespace Application.Tests.Services;

[TestFixture]
public class GoogleDriveEntryDetectorTests
{
    [Test]
    public void Detect_SingleTex_ReturnsIt()
    {
        GoogleDriveEntryDetector.Detect(new[] { "images/logo.png", "report.tex" })
            .Should().Be("report.tex");
    }

    [Test]
    public void Detect_MainTex_PreferredOverOtherTex()
    {
        GoogleDriveEntryDetector.Detect(new[] { "chapters/intro.tex", "main.tex" })
            .Should().Be("main.tex");
    }

    [Test]
    public void Detect_MultipleTexWithoutMain_ReturnsNull()
    {
        GoogleDriveEntryDetector.Detect(new[] { "intro.tex", "conclusion.tex" })
            .Should().BeNull();
    }

    [Test]
    public void Detect_SingleDocx_ReturnsIt()
    {
        GoogleDriveEntryDetector.Detect(new[] { "readme.md", "report.docx" })
            .Should().Be("report.docx");
    }

    [Test]
    public void Detect_NoSupportedFiles_ReturnsNull()
    {
        GoogleDriveEntryDetector.Detect(new[] { "readme.md", "data.csv" })
            .Should().BeNull();
    }

    [Test]
    public void IsZip_IsCaseInsensitive()
    {
        GoogleDriveEntryDetector.IsZip("SRC.ZIP").Should().BeTrue();
        GoogleDriveEntryDetector.IsZip("main.tex").Should().BeFalse();
        GoogleDriveEntryDetector.IsZip(null).Should().BeFalse();
    }

    [Test]
    public void Detect_ZipArchive_FindsMainTex()
    {
        using var memory = new MemoryStream();
        using (var zip = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(zip, "main.tex", "\\documentclass{article}");
            AddEntry(zip, "chapters/intro.tex", "Intro");
            AddEntry(zip, "images/logo.png", "png");
        }

        memory.Position = 0;
        using var archive = new ZipArchive(memory, ZipArchiveMode.Read);

        GoogleDriveEntryDetector.Detect(archive).Should().Be("main.tex");
    }

    private static void AddEntry(ZipArchive zip, string name, string content)
    {
        var entry = zip.CreateEntry(name);
        using var stream = entry.Open();
        var bytes = Encoding.UTF8.GetBytes(content);
        stream.Write(bytes);
    }
}
