using System;
using System.IO;
using System.Net.Http;
using System.Text;
using Amazon.Lambda.Core;
using System.Text.Json;
using System.Text.Json.Serialization;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook
{
    public class Function
    {
        private static readonly HttpClient Client = new HttpClient();

        public string FunctionHandler(GitHubEvent input, ILambdaContext context)
        {
            string message = $"Issue Created: {input.Issue.HtmlUrl}";
            string payload = JsonSerializer.Serialize(new { text = message });

            var slackUrl = Environment.GetEnvironmentVariable("SLACK_URL");
            if (string.IsNullOrEmpty(slackUrl))
            {
                context.Logger.LogLine("Slack URL environment variable is not set.");
                return "Error: Slack URL is not configured.";
            }

            var webRequest = new HttpRequestMessage(HttpMethod.Post, slackUrl)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            var response = Client.Send(webRequest);
            if (!response.IsSuccessStatusCode)
            {
                context.Logger.LogLine($"Failed to post message to Slack. Status code: {response.StatusCode}");
                return "Error: Failed to send message to Slack.";
            }

            using var reader = new StreamReader(response.Content.ReadAsStream());
            var responseContent = reader.ReadToEnd();
            context.Logger.LogLine($"Message posted to Slack successfully. Response: {responseContent}");

            return "Success";
        }
    }

    public class GitHubEvent
    {
        public Issue Issue { get; set; }
    }

    public class Issue
    {
        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; }
    }
}
