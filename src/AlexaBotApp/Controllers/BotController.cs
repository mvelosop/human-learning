// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EmptyBot v4.6.2

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;

namespace AlexaBotApp.Controllers
{
    [ApiController]
    public class BotController : ControllerBase
    {
        private readonly IAdapterIntegration _botAdapter;
        private readonly IBotFrameworkHttpAdapter _alexaAdapter;
        private readonly IBot _bot;

        public BotController(
            IAdapterIntegration botAdapter,
            IBotFrameworkHttpAdapter alexaAdapter,
            IBot bot)
        {
            _botAdapter = botAdapter;
            _alexaAdapter = alexaAdapter;
            _bot = bot;
        }

        [HttpPost("api/messages")]
        public async Task<InvokeResponse> BotPostAsync([FromBody]Activity activity)
        {
            var authHeader = Request.Headers["Authorization"];

            return await _botAdapter.ProcessActivityAsync(authHeader, activity, _bot.OnTurnAsync, default);
        }

        [HttpPost("api/alexa")]
        public async Task AlexaPostAsync()
        {
            Request.EnableBuffering();

            using (var reader = new StreamReader(Request.Body))
            {
                var body = await reader.ReadToEndAsync();
                Request.Body.Position = 0;

                await _alexaAdapter.ProcessAsync(Request, Response, _bot);
            }

        }
    }
}
