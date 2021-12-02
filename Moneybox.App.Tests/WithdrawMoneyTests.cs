using Moneybox.App.DataAccess;
using Moneybox.App.Domain.Services;
using Moneybox.App.Features;
using Moq;
using NUnit.Framework;
using System;

namespace Moneybox.App.Tests
{
    [Category("Integration")]
    public class WithdrawMoneyTests
    {
        private Mock<INotificationService> mockNotificationService;
        private Mock<IAccountRepository> mockAccountRepository;
        private WithdrawMoney withdrawMoney;

        [SetUp]
        public void Setup()
        {
            mockNotificationService = new Mock<INotificationService>();
            mockAccountRepository = new Mock<IAccountRepository>();
            withdrawMoney = new WithdrawMoney(mockAccountRepository.Object);
        }

        [Test]
        public void GivenFromAccountAndWithdrawlAmount_WhenFromAccountBalanceLessThanAmount_ThenThrowInvalidOperationException()
        {
            //Arrange
            var amount = 20m;

            var fromAccount = new Account(mockNotificationService.Object);
            fromAccount.Balance = 19m;
            mockAccountRepository.Setup(x => x.GetAccountById(It.IsAny<Guid>())).Returns(fromAccount);

            //Act and Assert
            Assert.Throws<InvalidOperationException>(() => withdrawMoney.Execute(Guid.NewGuid(), amount), "Insufficient funds to make transfer");
        }

        [Test]
        public void GivenFromAccuntAndWithdrawlAmount_WhenBalanceLeftIsLessThanLoFundsLimit_ThenNotifyLowFundOnUserEmail()
        {
            //Arrange
            var amount = 19m;

            var fromAccount = new Account(mockNotificationService.Object);
            fromAccount.Balance = 20m;
            fromAccount.User = new User
            {
                Email = "e@mail.com"
            };
            mockAccountRepository.Setup(x => x.GetAccountById(It.IsAny<Guid>())).Returns(fromAccount);

            //Act
            withdrawMoney.Execute(Guid.NewGuid(), amount);

            //Assert
            mockNotificationService.Verify(x => x.NotifyFundsLow(It.Is<string>(y => y.Equals("e@mail.com"))), Times.Once);
        }

        [Test]
        public void GivenFromAccuntsAndWithdrawlAmount_WhenSuccessfulWithdrawl_ThenVerifyAmountsForFromAccount()
        {
            //Arrange
            var amount = 200m;

            var fromAccount = new Account(mockNotificationService.Object);
            fromAccount.Balance = 1000m;
            fromAccount.Id = Guid.NewGuid();
            mockAccountRepository.Setup(x => x.GetAccountById(fromAccount.Id)).Returns(fromAccount);

            //Act
            withdrawMoney.Execute(fromAccount.Id, amount);

            //Assert
            Assert.AreEqual(800m, fromAccount.Balance);
            Assert.AreEqual(-200m, fromAccount.Withdrawn);
            mockAccountRepository.Verify(x => x.Update(It.Is<Account>(y => y.Equals(fromAccount))), Times.Once);
        }
    }
}