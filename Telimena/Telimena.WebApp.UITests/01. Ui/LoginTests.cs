﻿using System;
using System.Reflection;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Telimena.TestUtilities.Base;
using Telimena.WebApp.UiStrings;
using Assert = NUnit.Framework.Assert;

// ReSharper disable once IdentifierTypo
namespace Telimena.WebApp.UITests._01._Ui
{

    [TestFixture]
    public partial class _1_WebsiteTests : WebsiteTestBase
    {

        [Test, Order(1), Retry(3)]
        public void AdminLoginOk()
        {
            try
            {
                if (this.IsLoggedIn())
                {
                    this.LogOut();
                }
                this.Driver.Navigate().GoToUrl(this.GetAbsoluteUrl("Account/Login"));


                IWebElement login = this.Driver.FindElement(By.Id(Strings.Id.Email));
                IWebElement pass = this.Driver.FindElement(By.Id(Strings.Id.Password));
                login.SendKeys(this.AdminName);
                pass.SendKeys(this.AdminPassword);
                IWebElement submit = this.Driver.FindElement(By.Id(Strings.Id.SubmitLogin));
                submit.ClickWrapper(this.Driver);

                  if (this.Driver.TryFind(Strings.Id.PasswordForm) == null)
                {
                    this.RecognizeAdminDashboardPage();

                }
            }
            catch (Exception ex)
            {
                this.HandleError(ex, SharedTestHelpers.GetMethodName());
            }

        }


        [Test, Order(0), Retry(3)]
        public void AdminLogin_Failed()
        {
            try
            {
                if (this.IsLoggedIn())
                {
                    this.LogOut();
                }
                               this.Driver.Navigate().GoToUrl(this.GetAbsoluteUrl("Account/Login"));


                IWebElement login = this.Driver.FindElement(By.Id(Strings.Id.Email));
                IWebElement pass = this.Driver.FindElement(By.Id(Strings.Id.Password));
                login.SendKeys("Wrong");
                pass.SendKeys("Dude");
                IWebElement submit = this.Driver.FindElement(By.Id(Strings.Id.SubmitLogin));
                submit.ClickWrapper(this.Driver);
                WebDriverWait wait = new WebDriverWait(this.Driver, TimeSpan.FromSeconds(15));
                IWebElement element = wait.Until(x => x.FindElement(By.ClassName("validation-summary-errors")));

                Assert.AreEqual("Invalid username or password", element.Text);
            }
            catch (Exception ex)
            {
                this.HandleError(ex, SharedTestHelpers.GetMethodName());
            }
        }

    }
}
