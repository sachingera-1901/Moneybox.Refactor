using Moneybox.App.DataAccess;
using Moneybox.App.Domain.Services;
using Moneybox.App.Features;
using Moq;
using NUnit.Framework;
using System;

namespace Moneybox.App.Tests
{
    [Category("Unit")]
    public class AccountTests
    {
        private Mock<INotificationService> mockNotificationService;
        private Account account;

        [SetUp]
        public void Setup()
        {
            mockNotificationService = new Mock<INotificationService>();
            account = new Account(mockNotificationService.Object);
        }

        [Test]
        public void GivenAccountAndWithdrawlAmount_WhenAccountBalanceLessThanAmountAndChecksPerformed_ThenThrowInvalidOperationException()
        {
            //Arrange
            var amount = 20m;

            account.Balance = 19m;

            //Act and Assert
            Assert.Throws<InvalidOperationException>(() => account.PerformChecksToWithdrawMoney(amount), "Insufficient funds to make transfer");
        }

        [Test]
        public void GivenAccountAndWithdrawlAmount_WhenBalanceLeftIsLessThanLowFundsLimitandChecksPerformed_ThenNotifyLowFundOnUserEmail()
        {
            //Arrange
            var amount = 19m;

            account.Balance = 20m;
            account.User = new User
            {
                Email = "e@mail.com"
            };

            //Act
            account.PerformChecksToWithdrawMoney(amount);

            //Assert
            mockNotificationService.Verify(x => x.NotifyFundsLow(It.Is<string>(y => y.Equals("e@mail.com"))), Times.Once);
        }

        [Test]
        public void GivenAccountAndPaidInAmount_WhenaccountPaidIsInGreaterThanPaidInLimitAndChecksPerformed_ThenThrowInvalidOperationException()
        {
            //Arrange
            var amount = 200m;
            account.PaidIn = 3900m;

            //Act and Assert
            Assert.Throws<InvalidOperationException>(() => account.PerformChecksToPayInMoney(amount), "Account pay in limit reached");
        }

        [Test]
        public void GivenAccountAndPaidInAmount_WhenPayInApproachesPaidInLimitandChecksPerformed_ThenNotifyUserOnEmail()
        {
            //Arrange
            var amount = 200m;

            account.PaidIn = 3700m;
            account.User = new User
            {
                Email = "e@mail.com"
            };

            //Act
            account.PerformChecksToPayInMoney(amount);

            //Assert
            mockNotificationService.Verify(x => x.NotifyApproachingPayInLimit(It.Is<string>(y => y.Equals("e@mail.com"))), Times.Once);
        }

        [Test]
        public void GivenAccountandWithdrawlAmount_WhenSuccessfulWithdrawl_ThenVerifyAmountsForAccount()
        {
            //Arrange
            var amount = 200m;
            account.Balance = 1000m;

            //Act
            account.WithdrawMoney(amount);

            //Assert
            Assert.AreEqual(800m, account.Balance);
            Assert.AreEqual(-200m, account.Withdrawn);
        }


        [Test]
        public void GivenAccountandPaidInAmount_WhenSuccessfulPayIn_ThenVerifyAmountsForAccount()
        {
            //Arrange
            var amount = 200m;
            account.PaidIn = 1000m;

            //Act
            account.PayInMoney(amount);

            //Assert
            Assert.AreEqual(200, account.Balance);
            Assert.AreEqual(1200m, account.PaidIn);
        }
    }
}