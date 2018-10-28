﻿using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Threading.Tasks;
using TestStack.White;
using TestStack.White.Factory;
using TestStack.White.UIItems.WindowItems;

namespace Telimena.WebApp.UITests.Base
{
    public static class TestHelpers
    {

        public static async Task<Window> WaitForWindowAsync(Expression<Predicate<string>> match, TimeSpan timeout, string errorMessage = "")
        {
            Window win = null;
            Stopwatch timeoutWatch = Stopwatch.StartNew();
            while (true)
            {
                await Task.Delay(50);

                Process[] allProcesses = Process.GetProcesses();
                var compiled = match.Compile();

                foreach (Process allProcess in allProcesses)
                {
                    if (!compiled.Invoke(allProcess.MainWindowTitle))
                    {
                        continue;
                    }

                    Application app = TestStack.White.Application.Attach(allProcess);
                    win = app.Find(compiled, InitializeOption.NoCache);
                    if (win != null)
                    {
                        return win;
                    }
                }
                if (timeoutWatch.Elapsed > timeout)
                {
                    string expBody = ((LambdaExpression)match).Body.ToString();

                    throw new InvalidOperationException($"Failed to find window by expression on Title: {expBody}.{errorMessage}");
                }
            }

            return win;
        }

        public static async Task<Window> WaitForMessageBoxAsync(Window parent, string title, TimeSpan timeout, string errorMessage = "")
        {
            Window win = null;
            Stopwatch timeoutWatch = Stopwatch.StartNew();
            while (win == null)
            {
                await Task.Delay(50);
                win = parent.MessageBox(title);

                if (timeoutWatch.Elapsed > timeout)
                {
                    throw new InvalidOperationException($"Failed to find MessageBox {errorMessage}");
                }
            }

            return win;
        }
    }
}