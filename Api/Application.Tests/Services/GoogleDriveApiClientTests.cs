using System.Net;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using ReportChecker.Exceptions;
using ReportChecker.SourceProviders.GoogleDrive;

namespace Application.Tests.Services;

[TestFixture]
public class GoogleDriveApiClientTests
{
    private const string Token = "go-token";
    private const string FolderId = "folder-id";

    [Test]
    public async Task GetState_File_ParsesStringSizeAndUsesMd5AsHash()
    {
        var client = CreateClient(request =>
        {
            request.RequestUri!.AbsolutePath.Should().Be("/drive/v3/files/file-id");
            request.Headers.Authorization!.Scheme.Should().Be("Bearer");
            request.Headers.Authorization.Parameter.Should().Be(Token);
            return Json("""
                        {
                          "id": "file-id",
                          "name": "report.docx",
                          "mimeType": "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                          "size": "12345",
                          "modifiedTime": "2026-01-02T03:04:05.000Z",
                          "md5Checksum": "abc123"
                        }
                        """);
        });

        var state = await client.GetStateAsync(Token, "file-id");

        state.IsFolder.Should().BeFalse();
        state.FileId.Should().Be("file-id");
        state.Name.Should().Be("report.docx");
        state.Size.Should().Be(12345);
        state.Hash.Should().Be("abc123");
        state.Modified.Should().Be(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc));
        state.Files.Should().ContainSingle();
        state.Files[0].RelativePath.Should().Be("report.docx");
        state.SuggestedEntryPath.Should().Be("report.docx");
    }

    [Test]
    public async Task GetState_FileWithoutMd5_HashesSizeAndModifiedTime()
    {
        var client = CreateClient(_ => Json("""
                                            {
                                              "id": "file-id",
                                              "name": "report.tex",
                                              "mimeType": "text/x-tex",
                                              "size": "10",
                                              "modifiedTime": "2026-01-02T03:04:05.000Z"
                                            }
                                            """));

        var state = await client.GetStateAsync(Token, "file-id");

        state.Hash.Should().Be(Sha256($"10|{new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc).Ticks}"));
    }

    [Test]
    public async Task GetState_GoogleDocument_ThrowsBadRequest()
    {
        var client = CreateClient(_ => Json("""
                                            {
                                              "id": "doc-id",
                                              "name": "Отчёт",
                                              "mimeType": "application/vnd.google-apps.document"
                                            }
                                            """));

        var act = () => client.GetStateAsync(Token, "doc-id");

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Test]
    public async Task GetFolder_ListsFolderAndParsesPageToken()
    {
        var client = CreateClient(request =>
        {
            request.RequestUri!.AbsolutePath.Should().Be("/drive/v3/files");
            var query = Uri.UnescapeDataString(request.RequestUri.Query);
            query.Should().Contain("'root' in parents");
            query.Should().Contain("pageToken=next-1");
            return Json("""
                        {
                          "nextPageToken": "next-2",
                          "files": [
                            {
                              "id": "sub-id",
                              "name": "chapters",
                              "mimeType": "application/vnd.google-apps.folder"
                            },
                            {
                              "id": "file-id",
                              "name": "main.tex",
                              "mimeType": "text/x-tex",
                              "size": "7",
                              "modifiedTime": "2026-01-02T03:04:05.000Z",
                              "md5Checksum": "abc123"
                            }
                          ]
                        }
                        """);
        });

        var listing = await client.GetFolderAsync(Token, "root", "next-1", 100);

        listing.FolderId.Should().Be("root");
        listing.NextPageToken.Should().Be("next-2");
        listing.Items.Should().HaveCount(2);
        listing.Items[0].Type.Should().Be("dir");
        listing.Items[1].Type.Should().Be("file");
        listing.Items[1].Size.Should().Be(7);
        listing.Items[1].Md5.Should().Be("abc123");
    }

    [Test]
    public async Task GetState_Folder_CollectsFilesRecursivelyAndSuggestsEntry()
    {
        var requestedParents = new List<string>();
        var client = CreateClient(request =>
        {
            var uri = request.RequestUri!;
            if (uri.AbsolutePath == "/drive/v3/files/folder-id")
                return Json("""
                            {
                              "id": "folder-id",
                              "name": "Отчёты",
                              "mimeType": "application/vnd.google-apps.folder"
                            }
                            """);

            uri.AbsolutePath.Should().Be("/drive/v3/files");
            var query = Uri.UnescapeDataString(uri.Query);
            if (query.Contains($"'{FolderId}' in parents"))
            {
                requestedParents.Add(FolderId);
                return Json("""
                            {
                              "files": [
                                {
                                  "id": "sub-id",
                                  "name": "chapters",
                                  "mimeType": "application/vnd.google-apps.folder"
                                },
                                {
                                  "id": "main-id",
                                  "name": "main.tex",
                                  "mimeType": "text/x-tex",
                                  "size": "10",
                                  "md5Checksum": "aaa"
                                }
                              ]
                            }
                            """);
            }

            if (query.Contains("'sub-id' in parents"))
            {
                requestedParents.Add("sub-id");
                return Json("""
                            {
                              "files": [
                                {
                                  "id": "intro-id",
                                  "name": "intro.tex",
                                  "mimeType": "text/x-tex",
                                  "size": "5",
                                  "md5Checksum": "bbb"
                                }
                              ]
                            }
                            """);
            }

            throw new AssertionException($"Unexpected request: {uri}");
        });

        var state = await client.GetStateAsync(Token, FolderId);

        state.IsFolder.Should().BeTrue();
        state.Name.Should().Be("Отчёты");
        state.Size.Should().Be(15);
        state.Files.Select(e => e.RelativePath).Should().BeEquivalentTo("main.tex", "chapters/intro.tex");
        state.SuggestedEntryPath.Should().Be("main.tex");
        requestedParents.Should().BeEquivalentTo(FolderId, "sub-id");
    }

    [Test]
    public async Task GetFolder_InvalidFileId_ThrowsBadRequest()
    {
        var client = CreateClient(_ => Json("{}"));

        var act = () => client.GetFolderAsync(Token, "'root' in parents or 1=1", null, 100);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Test]
    public async Task ApiError_MapsToDomainException()
    {
        string body = """
                      {
                        "error": {
                          "code": 404,
                          "message": "File not found: missing.",
                          "errors": [{ "message": "File not found: missing.", "reason": "notFound" }]
                        }
                      }
                      """;
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        });

        var act = () => client.GetStateAsync(Token, "missing");

        (await act.Should().ThrowAsync<NotFoundException>())
            .WithMessage("*File not found*");
    }

    private static GoogleDriveApiClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var httpClient = new HttpClient(new StubHandler(responder))
        {
            BaseAddress = new Uri("https://www.googleapis.com"),
        };
        return new GoogleDriveApiClient(httpClient, NullLogger<GoogleDriveApiClient>.Instance);
    }

    private static HttpResponseMessage Json(string body)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
    }

    private static string Sha256(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(responder(request));
        }
    }
}
