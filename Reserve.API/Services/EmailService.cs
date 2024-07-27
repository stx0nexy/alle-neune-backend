using Mailjet.Client;
using Mailjet.Client.Resources;
using Newtonsoft.Json.Linq;

namespace Reserve.API.Services;

public class EmailService
{
    private readonly string _apiKey;
    private readonly string _apiSecret;

    public EmailService(IConfiguration configuration)
    {
        _apiKey = configuration["Mailjet:ApiKey"];
        _apiSecret = configuration["Mailjet:ApiSecret"];
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var client = new MailjetClient(_apiKey, _apiSecret);

        var request = new MailjetRequest
            {
                Resource = Send.Resource
            }
            .Property(Send.FromEmail, "st.x0nexy@gmail.com")
            .Property(Send.FromName, "Sofiia Nesterenko")
            .Property(Send.Subject, subject)
            .Property(Send.TextPart, body)
            .Property(Send.HtmlPart, body)
            .Property(Send.Recipients, new JArray {
                new JObject {
                    { "Email", to }
                }
            });

        var response = await client.PostAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            throw new System.Exception($"Error sending email: {response.StatusCode} {response.GetData()}");
        }
    }
}