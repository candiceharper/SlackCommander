﻿using System;
using System.Threading.Tasks;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using Refit;

namespace SlackCommander.Web
{
    public class WebhooksModule : NancyModule
    {
        public WebhooksModule(IAppSettings appSettings, IPendingCommands pendingCommands)
            : base("/webhooks")
        {
            Post["/fullcontact/person", runAsync: true] = async (_, ct) =>
            {
                // Parse the request data
                var personResult = this.Bind<FullContactPersonResult>();
                if (personResult == null)
                {
                    return await Task.FromResult(HttpStatusCode.BadRequest.WithReason("Unable to parse request body."));
                }

                // Get the pending command that corresponds to the posted data
                if (string.IsNullOrWhiteSpace(personResult.WebhookId))
                {
                    return await Task.FromResult(HttpStatusCode.BadRequest.WithReason("The webhookId property is missing from the request body."));
                }
                var command = pendingCommands.Get(personResult.WebhookId);
                if (command == null)
                {
                    return await Task.FromResult(HttpStatusCode.BadRequest.WithReason("No pending command matching the webhookId could be found."));
                }

                // Prepare message
                var slackMessage = new SlackMessage
                {
                    username = "SlackCommander",
                    icon_emoji = ":bust_in_silhouette:",
                    channel = "@" + command.user_name
                };
                if (personResult.Status != 200 ||
                    personResult.Likelihood < 0.7)
                {
                    slackMessage.text = string.Format(
                        "Unfortunately I'm unable to find any reliable information on who *{0}* is. " +
                        "I suggest you try <https://google.com/q={0}|Google>.",
                        command.text);
                }
                else
                {
                    slackMessage.text = string.Format("Likelihood: *{0}*", personResult.Likelihood.ToString("F2"));
                }

                // Post message to Slack
                var slackApi = RestService.For<ISlackApi>(appSettings.Get("slack:responseBaseUrl"));
                await slackApi.SendMessage(slackMessage, appSettings.Get("slack:responseToken"));
                return await Task.FromResult(HttpStatusCode.OK);
            };
        }
    }
}