using Infrared;
using Infrared.Impl;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ProvidersInterface;
using ProvidersInterface.Enums;
using ProvidersInterface.Models;
using System.Net.Http;
using Newtonsoft.Json;
using Infrared.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using System.Threading;

namespace Bishop.Engine
{
    public class BishopEngine : IBishopEngine
    {
        ITimer timer;
        private readonly TimeSpan SleepTime = TimeSpan.FromSeconds(5);
        ILogger Logger;

        public IServiceUrlProvider ServiceUrlProvider { get; }

        IHttpClientFactory HttpClientFactory;
        string GetNextActionUrl
        {
            get
            {
                return this.ServiceUrlProvider.GetUrl(eService.ActionService, eActionServiceUrl.GetNext.ToString());
            }
        }

        public IFeatureCollection ServerFeatures => throw new NotImplementedException();

        public IServiceProvider Services => throw new NotImplementedException();

        public BishopEngine(IHttpClientFactory httpClientFactory, ILogger logger, IServiceUrlProvider serviceUrlProvider)
        {
            this.HttpClientFactory = httpClientFactory;
            this.Logger = logger;
            this.ServiceUrlProvider = serviceUrlProvider;
        }

        private Func<Task<string>> GenerateActionFromScheduleModel(ActionModel actionModel)
        {
            Func<Task<string>> nextAction = async () =>
            {
                this.Logger.Information("ActionProvider - GenerateActionFromScheduleModel - nextAction - Generated Action started");
                using (var client = this.HttpClientFactory.CreateClient())
                {
                    this.Logger.Debug($"ActionProvider - GenerateActionFromScheduleModel - nextAction - Calling url request: {actionModel.FullUrl}");
                    var response = await client.GetAsync(actionModel.FullUrl);
                    this.Logger.Debug($"ActionProvider - GenerateActionFromScheduleModel - nextAction - Calling url response: {response}");
                    return response.ToString();
                }
            };
            return nextAction;
        }
        

        public void Start()
        {
            Func<Task> GenerateNextCycleAction = NextCycleAction;
            timer?.Dispose();
            timer = new GapBasedTimer(GenerateNextCycleAction, SleepTime, Logger);
            timer.InitializeCallback(GenerateNextCycleAction, SleepTime);
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }


        private async Task NextCycleAction()
        {
            ActionModel model = null;
            // IActionProvider actionProvider = _actionProvider;
            using (var client = this.HttpClientFactory.CreateClient())
            {
                var request = GetNextActionUrl;
                Logger.Debug($"BishopEngine - NextCycleAction - httpClient Request { request}");
                try
                {
                    HttpResponseMessage response = await client.GetAsync(request);
                    Logger.Debug($"BishopEngine - NextCycleAction - httpClient response { response}");

                    if (!response.IsSuccessStatusCode)
                    {
                        Logger.Error($"BishopEngine - NextCycleAction - Error in request for next action. request: { request } response: {response}");
                        return;
                    }
                    string responseString = await response.Content.ReadAsStringAsync();
                    model = JsonConvert.DeserializeObject<ActionModel>(responseString);
                }
                catch (Exception ex)
                {
                    Logger.Error($"BishopEngine - NextCycleAction - Error with httpClinet {ex}");
                }
            }

            if (model == null)
            {
                Logger.Information("BishopEngine - NextCycleAction - no action to perform");
                return;
            }

            Func<Task<string>> action = GenerateActionFromScheduleModel(model);
            if (action == null)
            {
                Logger.Information($"BishopEngine - NextCycleAction - couldn't generate action from model {model}");

                return;
            }

            try
            {
                Logger.Information($"BishopEngine - NextCycleAction - Found an action to perform.. starting {model.FullUrl}");
                string response = await action();
                Logger.Information($"BishopEngine - NextCycleAction - Completed. Response: {response}");
            }
            catch (Exception ex)
            {
                Logger.Error($"BishopEngine - NextCycleAction - Error. Exception: {ex}");
            }
        }

        public void Dispose()
        {
            timer?.Dispose();
            timer = null;
        }
    }
}
