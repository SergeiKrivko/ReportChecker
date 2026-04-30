using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using ReportChecker.Abstractions;
using YooMoney.Dtos;

namespace YooMoney;

public class YooMoneyService(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IPaymentClient
{
    public async Task<string> CreatePaymentAsync(decimal sum, Guid paymentId, CancellationToken ct = default)
    {
        var label = paymentId.ToString();
        var quickPay = new QuickPay
        {
            Receiver = configuration["YooMoney.WalletId"] ?? throw new Exception("WalletId not set"),
            Sum = sum,
            Label = label,
            Source = PaymentSource.Wallet,
            SuccessRedirectUrl = configuration["YooMoney.PaymentRedirectUrl"]
        };
        var url = await RequestData(quickPay, ct);
        return url;
    }

    private async Task<string> RequestData(QuickPay quickPay, CancellationToken ct)
    {
        // Создаем словарь для параметров тела запроса
        var payload = new Dictionary<string, string>();
        payload["receiver"] = quickPay.Receiver;
        if (quickPay.Street is not null)
            payload["street"] = quickPay.Street;
        if (quickPay.Building is not null)
            payload["building"] = quickPay.Building;
        if (quickPay.Suite is not null)
            payload["suite"] = quickPay.Suite;
        if (quickPay.Flat is not null)
            payload["flat"] = quickPay.Flat;
        if (quickPay.Zip is not null)
            payload["zip"] = quickPay.Zip;
        if (quickPay.FirstName is not null)
            payload["firstname"] = quickPay.FirstName;
        if (quickPay.LastName is not null)
            payload["lastname"] = quickPay.LastName;
        if (quickPay.FathersName is not null)
            payload["fathersname"] = quickPay.FathersName;
        if (quickPay.Email is not null)
            payload["email"] = quickPay.Email;
        if (quickPay.Phone is not null)
            payload["phone"] = quickPay.Phone;
        if (quickPay.City is not null)
            payload["city"] = quickPay.City;
        if (quickPay.Sender is not null)
            payload["sender"] = quickPay.Sender;
        if (quickPay.SuccessRedirectUrl is not null)
            payload["successURL"] = quickPay.SuccessRedirectUrl;
        payload["quickpay-form"] = quickPay.QuickPayForm;
        payload["paymentType"] = quickPay.Source switch
        {
            PaymentSource.Wallet => "PC",
            PaymentSource.BankCard => "AC",
            _ => throw new Exception("Unknown payment source"),
        };
        payload["sum"] = quickPay.Sum.ToString(CultureInfo.InvariantCulture);
        payload["label"] = quickPay.Label;

        var queryParams = string.Join("&", payload.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        var uri = "https://yoomoney.ru/quickpay/confirm.xml?" + queryParams;
        var content = new StringContent(string.Empty);

        var httpClient = httpClientFactory.CreateClient("YooMoney");
        var response = await httpClient.PostAsync(uri, content, ct);

        if (response.IsSuccessStatusCode)
        {
            // Читаем содержимое ответа
            var requestUri = response.RequestMessage?.RequestUri?.ToString();
            if (!string.IsNullOrEmpty(requestUri))
            {
                return requestUri;
            }
        }

        return uri;
    }

    private const string HistoryUrl = "https://yoomoney.ru/api/operation-history";

    public async Task<bool> IsPaymentSuccessfulAsync(Guid paymentId, CancellationToken ct = default)
    {
        var label = paymentId.ToString();
        var request = new HttpRequestMessage(HttpMethod.Post, HistoryUrl);
        request.Content = JsonContent.Create(new HistoryRequestDto
        {
            Label = label,
        });
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", configuration["YooMoney.AccessToken"]);

        var httpClient = httpClientFactory.CreateClient("YooMoney");
        var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var history = await response.Content.ReadFromJsonAsync<HistoryResponseDto>(ct);
        if (history == null)
            return false;
        var operation = history.Operations.SingleOrDefault(e => e.Label == label);
        if (operation == null)
            return false;
        return operation.Status == "success";
    }
}