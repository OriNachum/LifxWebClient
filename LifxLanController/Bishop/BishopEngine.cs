﻿using Infrared;
using Infrared.Impl;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ProvidersInterface;
using ProvidersInterface.Enums;
using ActionService.Logic;
using ProvidersInterface.Models;
using System.Net.Http;
using Newtonsoft.Json;
using Infrared.Enums;

namespace Bishop
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
                return this.ServiceUrlProvider.GetUrl(eService.ActionService, eActionServiceUrl.GetNext);
            }
        }

        public BishopEngine(IHttpClientFactory httpClientFactory, ILogger logger, IServiceUrlProvider serviceUrlProvider)
        {
            this.HttpClientFactory = httpClientFactory;
            this.Logger = logger;
            this.ServiceUrlProvider = serviceUrlProvider;
        }

        public void Start()
        {
            Func<Task> GenerateNextCycleAction = NextCycleAction;
            timer = new GapBasedTimer(GenerateNextCycleAction, SleepTime, Logger);
            timer.InitializeCallback(GenerateNextCycleAction, SleepTime);
            // wait NextCycleAction();
            // Loop queries database (Refresh) and asks for state
        }


        private async Task NextCycleAction()
        {
            ActionModel model = null;
            // IActionProvider actionProvider = _actionProvider;
            using (var client = this.HttpClientFactory.CreateClient())
            {
                HttpResponseMessage response = await client.GetAsync(GetNextActionUrl); 
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Error($"BishopEngine - NextCycleAction - Error in request for next action. response: {response}");
                    return;
                }
                string responseString = await response.Content.ReadAsStringAsync();
                model = JsonConvert.DeserializeObject<ActionModel>(responseString);
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

        #region IDisposable
        public void Dispose()
        {
            if (timer != null)
            {
                timer.Dispose();
            }
        } 
        #endregion

    }
}
