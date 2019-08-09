﻿using System;
using System.Diagnostics;
using System.Threading;
using DotNetLittleHelpers;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.Extensions;

namespace Telimena.TestUtilities.Base
{
    public static class DOMActionsExtensions
    {
        public static void ClickWrapper(this IWebElement element, IWebDriver driver, Action<string> log)
        {
            int attempt = 0;
            while (attempt < 4)
            {
                attempt++;
                try
                {
                    element.Click();
                    break;
                }
                catch (Exception ex)
                {
                    log($"Attempt {attempt} - Could not click on  [{element?.TagName}] element: [{element?.Text}]. {ex}");
                    Actions actions = new Actions(driver);
                    actions.MoveToElement(element).Click().Perform();
                    actions.Perform();
                    driver.ExecuteJavaScript("arguments[0].scrollIntoView(true);", element);
                    Thread.Sleep(500);

                    if (element != null)
                    {

                        try
                        {
                            element.Click();
                        }
                        catch (WebDriverException anotherEx)
                        {
                            if (attempt >= 3)
                            {
                                throw new InvalidOperationException($"Cannot click on [{element?.TagName}] element: [{element?.Text}] regardless of moving it to viewport." +
                                                                    anotherEx);
                            }
                            else
                            {
                                log($"Attempt {attempt} - Could not click on [{element?.TagName}] element: [{element?.Text}] Regardless of moving to viewport. {ex}");
                            }

                        }
                    }
                }
            }
        }

    }
}