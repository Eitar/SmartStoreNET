﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Web;
using log4net;
using log4net.Core;
using log4net.Util;
using SmartStore.Core.Infrastructure;
using SmartStore.Utilities;

namespace SmartStore.Core.Logging
{
	[DebuggerDisplay("Logger: {Name}")]
	public class Log4netLogger : ILogger
	{
		private static readonly Type _declaringType = typeof(Log4netLogger);

		private readonly log4net.Core.ILogger _logger;

		public Log4netLogger(log4net.Core.ILogger logger)
		{
			Guard.NotNull(logger, nameof(logger));

			_logger = logger;
		}

		public string Name
		{
			get { return _logger.Name;  }
		}

		public bool IsEnabledFor(LogLevel level)
		{
			return _logger.IsEnabledFor(ConvertLevel(level));
		}

		public void Log(LogLevel level, Exception exception, string message, object[] args)
		{
			var logLevel = ConvertLevel(level);

			if (message == null && exception != null)
			{
				message = exception.Message;
			}

			object messageObj = message;

			if (args != null && args.Length > 0)
			{
				messageObj = new SystemStringFormat(CultureInfo.CurrentUICulture, message, args);
			}

			ThreadContext.Properties["LevelId"] = (int)level;

			if (Data.DataSettings.DatabaseIsInstalled())
			{
				TryAddExtendedThreadInfo();
			}
			
			_logger.Log(_declaringType, logLevel, messageObj, exception);
		}

		protected internal void TryAddExtendedThreadInfo()
		{
			var props = LogicalThreadContext.Properties;

			// Load the log4net thread with additional properties if they are available
			var threadInfoMissing = props["sm:ThreadInfoAdded"] == null;

			if (threadInfoMissing)
			{
				using (new ActionDisposable(() => props["sm:ThreadInfoAdded"] = true))
				{
					
					var workContext = EngineContext.Current.Resolve<IWorkContext>();			

					// CustomerId
					try
					{
						props["CustomerId"] = workContext.CurrentCustomer.Id;
					}
					catch
					{
						props["CustomerId"] = DBNull.Value;
					}

					var webHelper = EngineContext.Current.Resolve<IWebHelper>();
					var httpContext = EngineContext.Current.Resolve<HttpContextBase>();

					// Url & stuff
					try
					{
						props["Url"] = webHelper.GetThisPageUrl(true);
						props["Referrer"] = webHelper.GetUrlReferrer();
						props["HttpMethod"] = httpContext.Request.HttpMethod;
						props["Ip"] = webHelper.GetCurrentIpAddress();
					}
					catch
					{
						props["Url"] = DBNull.Value;
						props["Referrer"] = DBNull.Value;
						props["HttpMethod"] = DBNull.Value;
						props["Ip"] = DBNull.Value;
					}
				}
			}
		}

		private static Level ConvertLevel(LogLevel level)
		{
			switch (level)
			{
				case LogLevel.Information:
					return Level.Info;
				case LogLevel.Warning:
					return Level.Warn;
				case LogLevel.Error:
					return Level.Error;
				case LogLevel.Fatal:
					return Level.Fatal;
				default:
					return Level.Debug;
			}
		}
	}
}
