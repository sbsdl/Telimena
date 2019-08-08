﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DotNetLittleHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.PageObjects;
using OpenQA.Selenium.Support.UI;
using Telimena.WebApp.UiStrings;
using Assert = NUnit.Framework.Assert;
using ExpectedConditions = SeleniumExtras.WaitHelpers.ExpectedConditions;
using TestContext = NUnit.Framework.TestContext;

namespace Telimena.TestUtilities.Base
{
    [TestFixture]
    public abstract class WebsiteTestBase : IntegrationTestBase
    {

        protected Guid RegisterApp(string name, Guid? key, string description, string assemblyName, bool canAlreadyExist, bool hasToExistAlready)
        {
            this.GoToAdminHomePage();

            WebDriverWait wait = new WebDriverWait(this.Driver, TimeSpan.FromSeconds(25));

            this.Driver.FindElement(By.Id(Strings.Id.RegisterApplicationLink)).Click();
            wait.Until(ExpectedConditions.ElementIsVisible(By.Id(@Strings.Id.RegisterApplicationForm)));
            if (key != null)
            {
                this.Driver.FindElement(By.Id(Strings.Id.TelemetryKeyInputBox)).Clear();
                this.Driver.FindElement(By.Id(Strings.Id.TelemetryKeyInputBox)).SendKeys(key.ToString());
            }
            else
            {
                IWebElement ele = this.Driver.FindElement(By.Id(Strings.Id.TelemetryKeyInputBox));

                string autoGeneratedGuid = ele.GetAttribute("value");
                Assert.AreNotEqual(Guid.Empty, Guid.Parse(autoGeneratedGuid));
                key = new Guid(autoGeneratedGuid);
            }
            
            this.Driver.FindElement(By.Id(Strings.Id.ProgramNameInputBox)).SendKeys(name);
            this.Driver.FindElement(By.Id(Strings.Id.ProgramDescriptionInputBox)).SendKeys(description);
            this.Driver.FindElement(By.Id(Strings.Id.PrimaryAssemblyNameInputBox)).SendKeys(assemblyName);

            this.Driver.FindElement(By.Id(Strings.Id.SubmitAppRegistration)).Click();


            IAlert alert = this.Driver.WaitForAlert(10000);
            if (alert != null)
            {
                if (canAlreadyExist)
                {
                    if (alert.Text != "Use different telemetry key")
                    {
                        Assert.AreEqual($"A program with name [{name}] was already registered by TelimenaSystemDevTeam", alert.Text);
                    }
                    alert.Accept();
                    return key.Value;
                }
                else
                {
                    Assert.Fail("Test scenario expects that the app does not exist");
                }
            }
            else
            {
                if (hasToExistAlready)
                {
                    Assert.Fail("The app should already exist and the error was expected");
                }
            }

            IWebElement programTable = wait.Until(ExpectedConditions.ElementIsVisible(By.Id(Strings.Id.ProgramSummaryBox)));

            var infoElements = programTable.FindElements(By.ClassName(Strings.Css.ProgramInfoElement));

            Assert.AreEqual(name, infoElements[0].Text);
            Assert.AreEqual(description, infoElements[1].Text);
            Assert.AreEqual(key.ToString(), infoElements[2].Text);
            Assert.AreEqual(assemblyName, infoElements[4].Text);
            return key.Value;
        }

        protected void LogOut()
        {
            this.Driver.Navigate().GoToUrl(this.GetAbsoluteUrl("Account/LogOff"));
            this.Driver.Navigate().GoToUrl(this.GetAbsoluteUrl(""));
        }
     


        /// <summary>
        /// Start a download and wait for a file to appear
        /// https://stackoverflow.com/a/46440261/1141876
        /// </summary>
        /// <param name="expectedName">If we don't know the extension, Chrome creates a temp file in download folder and we think we have the file already</param>
        protected FileInfo ActAndWaitForFileDownload(
            Action action
            , string expectedName
            , TimeSpan maximumWaitTime
            , string downloadDirectory)
        {
            var expectedPath = Path.Combine(downloadDirectory, expectedName);
            try
            {
                File.Delete(expectedPath);
            }
            catch (Exception ex)
            {
                this.Log($"Could not delete file {expectedPath }, error: {ex}");
            }
            var stopwatch = Stopwatch.StartNew();
            Assert.IsFalse(File.Exists(expectedPath));
            action();
            this.Log($"Action executed");

            var isTimedOut = false;
            string filePath = null;
            Func<bool> fileAppearedOrTimedOut = () =>
            {
                isTimedOut = stopwatch.Elapsed > maximumWaitTime;
                filePath = Directory.GetFiles(downloadDirectory)
                    .FirstOrDefault(x => Path.GetFileName(x) == expectedName);
                if (filePath != null)
                {
                    this.Log($"File stored at {filePath}");
                }
                else
                {
                    this.Log($"File not ready yet. Elapsed: {stopwatch.Elapsed}");
                }

                return filePath != null || isTimedOut;
            };

            do
            {
                Thread.Sleep(500);
            }
            while (!fileAppearedOrTimedOut());

            if (!string.IsNullOrEmpty(filePath))
            {
                Assert.AreEqual(expectedPath, filePath);
                return new FileInfo(filePath);
            }
            if (isTimedOut)
            {
                Assert.Fail("Failed to download");
            }

            return null;

        }

        protected void ClickOnProgramMenuButton(string appName, string buttonSuffix)
        {
            try
            {

                Retrier.RetryAsync(() =>
                {
                    WebDriverWait wait = new WebDriverWait(this.Driver, TimeSpan.FromSeconds(15));
                    IWebElement element = this.TryFind(By.Id($"{appName}_menu"));

                    this.Log($"Found { appName}_menu button");

                    var prgMenu = wait.Until(ExpectedConditions.ElementToBeClickable(element));


                    prgMenu.Click();
                    this.Log($"Clicked { appName}_menu button");


                    IWebElement link = this.TryFind(By.Id(appName + buttonSuffix));
                    this.Log($"Found { appName}{buttonSuffix} button");

                    link = wait.Until(ExpectedConditions.ElementToBeClickable(link));

                    link.Click();
                }, TimeSpan.FromMilliseconds(500), 3).GetAwaiter().GetResult();

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to click on button {buttonSuffix} for app {appName}.", ex);
            }
        }

        protected void ClickOnManageProgramMenu(string appName)
        {
            this.ClickOnProgramMenuButton(appName, "_manageLink");

        }

        public void WaitForSuccessConfirmationWithText(WebDriverWait wait, Func<string, bool> validateText)
        {
            this.WaitForConfirmationWithTextAndClass(wait, "success", validateText);
        }

        public void WaitForErrorConfirmationWithText(WebDriverWait wait, Func<string, bool> validateText)
        {
            this.WaitForConfirmationWithTextAndClass(wait, "danger", validateText);
        }

        public void WaitForConfirmationWithTextAndClass(WebDriverWait wait, string cssPart, Func<string, bool> validateText)
        {
            var confirmationBox = wait.Until(ExpectedConditions.ElementIsVisible(By.Id(Strings.Id.TopAlertBox)));

            Assert.IsTrue(confirmationBox.GetAttribute("class").Contains(cssPart), "The alert has incorrect class: " + confirmationBox.GetAttribute("class"));
            Assert.IsTrue(validateText(confirmationBox.Text), "Incorrect message: " + confirmationBox.Text);
        }

        public static Lazy<RemoteWebDriver> RemoteDriver = new Lazy<RemoteWebDriver>(() =>
        {
            var browser = GetBrowser("Chrome");

          //  browser.Manage().Window.Maximize();
            browser.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
            return browser;
        });

        public static bool ShowBrowser
        {
            get
            {
                try
                {
#if DEBUG
                    return true;
#endif
#pragma warning disable 162
                    return GetSetting<bool>(ConfigKeys.ShowBrowser);
#pragma warning restore 162
                }
                catch
                {
                    return false;
                }
            }
        }

        private static RemoteWebDriver GetBrowser(string browser)
        {
            switch (browser)
            {
                case "Chrome":
                    ChromeOptions opt = new ChromeOptions();
                    opt.AddArgument("--safebrowsing-disable-download-protection");
                    opt.AddUserProfilePreference("safebrowsing", "enabled");
                    if (!ShowBrowser)
                    {
                      opt.AddArgument("--headless");
                    }
                    var driverService = ChromeDriverService.CreateDefaultService();
                    var driver = new ChromeDriver(driverService, opt);
                    driver.Manage().Window.Maximize();
                    //driver.Manage().Window.Size = new System.Drawing.Size(4800, 2560);

                    Task.Run(() => AllowHeadlessDownload(driver, driverService));
                    return driver;
                case "Firefox":
                    FirefoxOptions options = new FirefoxOptions();
             //       options.AddArguments("--headless");
                    var ff = new FirefoxDriver(options);
                    return ff;
                case "IE":
                    return new InternetExplorerDriver();
                default:
                    return new ChromeDriver();
            }
        }

        protected static string DownloadPath => Environment.GetEnvironmentVariable("USERPROFILE") + "\\Downloads";


        static async Task AllowHeadlessDownload(IWebDriver driver,  ChromeDriverService driverService)
        {
            var jsonContent = new JObject(
                new JProperty("cmd", "Page.setDownloadBehavior"),
                new JProperty("params",
                    new JObject(new JObject(
                        new JProperty("behavior", "allow"),
                        new JProperty("downloadPath", DownloadPath)))));
            var content = new StringContent(jsonContent.ToString(), Encoding.UTF8, "application/json");
            var sessionIdProperty = typeof(ChromeDriver).GetProperty("SessionId");
            var sessionId = sessionIdProperty.GetValue(driver, null) as SessionId;

            using (var client = new HttpClient())
            {
                client.BaseAddress = driverService.ServiceUrl;
                var result = await client.PostAsync("session/" + sessionId.ToString() + "/chromium/send_command", content);
                var resultContent = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
        }

        public IWebDriver Driver => RemoteDriver.Value;
        internal ITakesScreenshot Screenshooter => this.Driver as ITakesScreenshot;

        public void WaitForPageLoad(int maxWaitTimeInSeconds)
        {
            string state = string.Empty;
            try
            {
                WebDriverWait wait = new WebDriverWait(this.Driver, TimeSpan.FromSeconds(maxWaitTimeInSeconds));

                //Checks every 500 ms whether predicate returns true if returns exit otherwise keep trying till it returns ture
                wait.Until(d =>
                {

                    try
                    {
                        state = ((IJavaScriptExecutor) this.Driver).ExecuteScript(@"return document.readyState").ToString();
                    }
                    catch (InvalidOperationException)
                    {
                        //Ignore
                    }
                    catch (NoSuchWindowException)
                    {
                        //when popup is closed, switch to last windows
                        this.Driver.SwitchTo().Window(this.Driver.WindowHandles.Last());
                    }

                    //In IE7 there are chances we may get state as loaded instead of complete
                    return (state.Equals("complete", StringComparison.InvariantCultureIgnoreCase) ||
                            state.Equals("loaded", StringComparison.InvariantCultureIgnoreCase));

                });
            }
            catch (TimeoutException)
            {
                //sometimes Page remains in Interactive mode and never becomes Complete, then we can still try to access the controls
                if (!state.Equals("interactive", StringComparison.InvariantCultureIgnoreCase))
                    throw;
            }
            catch (NullReferenceException)
            {
                //sometimes Page remains in Interactive mode and never becomes Complete, then we can still try to access the controls
                if (!state.Equals("interactive", StringComparison.InvariantCultureIgnoreCase))
                    throw;
            }
            catch (WebDriverException)
            {
                if (this.Driver.WindowHandles.Count == 1)
                {
                    this.Driver.SwitchTo().Window(this.Driver.WindowHandles[0]);
                }

                state = ((IJavaScriptExecutor) this.Driver).ExecuteScript(@"return document.readyState").ToString();
                if (!(state.Equals("complete", StringComparison.InvariantCultureIgnoreCase) ||
                      state.Equals("loaded", StringComparison.InvariantCultureIgnoreCase)))
                    throw;
            }
        }

        public void GoToAdminHomePage()
        {
            try
            {
                this.Driver.Navigate().GoToUrl(this.GetAbsoluteUrl(""));
                this.LoginAdminIfNeeded();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error while logging in admin", ex);
            }
        }

        public string GetAbsoluteUrl(string relativeUrl)
        {
            return this.TestEngine.GetAbsoluteUrl(relativeUrl);
        }

        public void RecognizeAdminDashboardPage()
        {
            WebDriverWait wait = new WebDriverWait(this.Driver, TimeSpan.FromSeconds(15));
            if (this.Driver.Url.Contains("ChangePassword"))
            {
                this.Log("Going from change password page to Admin dashboard");
                this.Driver.Navigate().GoToUrl(this.GetAbsoluteUrl(""));
            }

            wait.Until(x => x.FindElement(By.Id(Strings.Id.PortalSummary)));
        }

        public IWebElement TryFind(string nameOrId, int timeoutSeconds = 10)
        {
            try
            {
                WebDriverWait wait = new WebDriverWait(this.Driver, TimeSpan.FromSeconds(timeoutSeconds));
                return wait.Until(x => x.FindElement(By.Id(nameOrId)));
            }
            catch
            {
            }

            return null;
        }

        protected IWebElement TryFind(By by, int timeoutSeconds = 10)
        {
            try
            {
                WebDriverWait wait = new WebDriverWait(this.Driver, TimeSpan.FromSeconds(timeoutSeconds));
                var element = wait.Until(x => x.FindElement(by));
                Actions actions = new Actions(this.Driver);
                actions.MoveToElement(element);
                actions.Perform();
                return element;
            }
            catch
            {
            }

            return null;
        }

        protected ReadOnlyCollection<IWebElement> TryFindMany(By by, int timeoutSeconds = 10)
        {
            try
            {
                WebDriverWait wait = new WebDriverWait(this.Driver, TimeSpan.FromSeconds(timeoutSeconds));
                var elements = wait.Until(x => x.FindElements(by));
                if (elements.Any())
                {

                    Actions actions = new Actions(this.Driver);
                    actions.MoveToElement(elements.Last());
                    actions.Perform();
                }

                return elements;
            }
            catch
            {
            }

            return null;
        }

        protected IWebElement TryFind(Func<IWebElement> finderFunc, TimeSpan timeout)
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeout.TotalMilliseconds)
            {
                try
                {
                    IWebElement result = finderFunc();
                    if (result != null)
                    {
                        return result;
                    }
                }
                catch
                {
                    //np
                }
            }

            return null;
        }

        protected bool IsLoggedIn()
        {
            if (this.TryFind(Strings.Id.MainHeader) != null)
            {
                return true;
            }

            return false;
        }

        protected void HandleError(Exception ex, string screenshotName, List<string> outputs = null, List<string> errors = null)
        {
            Screenshot screen = this.Screenshooter.GetScreenshot();
            string path = Common.CreatePngPath(screenshotName, this.TestRunFolderName);
            this.Log($"Saving error screen shot at {path}");
            screen.SaveAsFile(path, ScreenshotImageFormat.Png);
            TestContext.AddTestAttachment(path);
            string page = this.Driver.PageSource;

            string errorOutputs = "";
            if (errors != null)
            {
                errorOutputs = string.Join("\r\n", errors);
            }

            string normalOutputs = "";
            if (outputs != null)
            {
                normalOutputs = string.Join("\r\n", outputs);
            }

            IAlert alert = this.Driver.WaitForAlert(500);
            alert?.Dismiss();
            throw new AssertFailedException($"{ex}\r\n\r\n{this.PresentParams()}\r\n\r\n{errorOutputs}\r\n\r\n{normalOutputs}\r\n\r\n{page}", ex);

        }

        private string PresentParams()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Url: " + this.Driver.Url);
            sb.Append("TestContext Parameters: ");
            foreach (string testParameter in TestContext.Parameters.Names)
            {
                sb.Append(testParameter + ": " + TestContext.Parameters[testParameter] + " ");
            }

            return sb.ToString();
        }

        public void LoginAdminIfNeeded()
        {
            this.LoginIfNeeded(this.AdminName, this.AdminPassword);
        }

        protected void LoginIfNeeded(string userName, string password)
        {
            if (!this.IsLoggedIn())
            {
                this.Driver.Navigate().GoToUrl(this.GetAbsoluteUrl("Account/Login"));
            }

            if (this.Driver.Url.IndexOf("Login", StringComparison.InvariantCultureIgnoreCase) != -1 &&
                this.Driver.FindElement(new ByIdOrName(Strings.Id.LoginForm)) != null)
            {
                this.Log("Trying to log in...");
                IWebElement login = this.Driver.FindElement(new ByIdOrName(Strings.Id.Email));

                if (login != null)
                {
                    IWebElement pass = this.Driver.FindElement(new ByIdOrName(Strings.Id.Password));
                    login.SendKeys(userName);
                    pass.SendKeys(password);
                    IWebElement submit = this.Driver.FindElement(new ByIdOrName(Strings.Id.SubmitLogin));
                    submit.Click();
                    this.GoToAdminHomePage();
                    this.RecognizeAdminDashboardPage();
                }
            }
            else
            {
                this.Log("Skipping logging in");
            }
        }
    }
}