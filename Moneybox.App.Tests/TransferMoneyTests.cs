using Moneybox.App.DataAccess;
using Moneybox.App.Domain.Services;
using Moneybox.App.Features;
using Moq;
using NUnit.Framework;
using System;

namespace Moneybox.App.Tests
{
    [Category("Integration")]
    public class TransferMoneyTests
    {
        private Mock<INotificationService> mockNotificationService;
        private Mock<IAccountRepository> mockAccountRepository;
        private TransferMoney transferMoney;

        [SetUp]
        public void Setup()
        {
            mockNotificationService = new Mock<INotificationService>();
            mockAccountRepository = new Mock<IAccountRepository>();
            transferMoney = new TransferMoney(mockAccountRepository.Object);
        }

        [Test]
        public void GivenFromAndToAccuntsAndTransferAmount_WhenFromAccountBalanceLessThanAmount_ThenThrowInvalidOperationException()
        {
            //Arrange
            var amount = 20m;

            var fromAccount = new Account(mockNotificationService.Object);
            fromAccount.Balance = 19m;
            mockAccountRepository.Setup(x => x.GetAccountById(It.IsAny<Guid>())).Returns(fromAccount);

            //Act and Assert
            Assert.Throws<InvalidOperationException>(() => transferMoney.Execute(Guid.NewGuid(), Guid.NewGuid(), amount), "Insufficient funds to make transfer");
        }

        [Test]
        public void GivenFromAndToAccuntsAndTransferAmount_WhenBalanceLeftIsLessThanLowFundsLimit_ThenNotifyLowFundOnUserEmail()
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
            transferMoney.Execute(Guid.NewGuid(), Guid.NewGuid(), amount);

            //Assert
            mockNotificationService.Verify(x => x.NotifyFundsLow(It.Is<string>(y => y.Equals("e@mail.com"))), Times.Once);
        }

        [Test]
        public void GivenFromAndToAccuntsAndTransferAmount_WhenToAccountPaidInGreaterThanPaidInLimit_ThenThrowInvalidOperationException()
        {
            //Arrange
            var amount = 200m;
            
            var fromAccount = new Account(mockNotificationService.Object);
            fromAccount.Balance = 1000m;
            fromAccount.Id = Guid.NewGuid();
            mockAccountRepository.Setup(x => x.GetAccountById(fromAccount.Id)).Returns(fromAccount);

            var toAccount = new Account(mockNotificationService.Object);
            toAccount.PaidIn = 3900m;
            toAccount.Id = Guid.NewGuid();
            mockAccountRepository.Setup(x => x.GetAccountById(toAccount.Id)).Returns(toAccount);


            //Act and Assert
            Assert.Throws<InvalidOperationException>(() => transferMoney.Execute(fromAccount.Id, toAccount.Id, amount), "Account pay in limit reached");
        }

        [Test]
        public void GivenFromAndToAccuntsAndTransferAmount_WhenPayInApproachesPaidInLimit_ThenNotifyUserOnEmail()
        {
            //Arrange
            var amount = 200m;
            
            var fromAccount = new Account(mockNotificationService.Object);
            fromAccount.Balance = 1000m;
            fromAccount.Id = Guid.NewGuid();
            mockAccountRepository.Setup(x => x.GetAccountById(fromAccount.Id)).Returns(fromAccount);

            var toAccount = new Account(mockNotificationService.Object);
            toAccount.PaidIn = 3700m;
            toAccount.Id = Guid.NewGuid();
            toAccount.User = new User
            {
                Email = "e@mail.com"
            };
            mockAccountRepository.Setup(x => x.GetAccountById(toAccount.Id)).Returns(toAccount);


            //Act
            transferMoney.Execute(fromAccount.Id, toAccount.Id, amount);

            //Assert
            mockNotificationService.Verify(x => x.NotifyApproachingPayInLimit(It.Is<string>(y => y.Equals("e@mail.com"))), Times.Once);
        }

        [Test]
        public void GivenFromAndToAccuntsAndTransferAmount_WhenSuccessfulTransfer_ThenVerifyAmountsForFromAndToAccounts()
        {
            //Arrange
            var amount = 200m;
            
            var fromAccount = new Account(mockNotificationService.Object);
            fromAccount.Balance = 1000m;
            fromAccount.Id = Guid.NewGuid();
            mockAccountRepository.Setup(x => x.GetAccountById(fromAccount.Id)).Returns(fromAccount);

            var toAccount = new Account(mockNotificationService.Object);
            toAccount.PaidIn = 3000m;
            toAccount.Id = Guid.NewGuid();
            mockAccountRepository.Setup(x => x.GetAccountById(toAccount.Id)).Returns(toAccount);


            //Act
            transferMoney.Execute(fromAccount.Id, toAccount.Id, amount);

            //Assert
            Assert.AreEqual(800m, fromAccount.Balance);
            Assert.AreEqual(-200m, fromAccount.Withdrawn);
            mockAccountRepository.Verify(x => x.Update(It.Is<Account>(y => y.Equals(fromAccount))), Times.Once);

            Assert.AreEqual(200m, toAccount.Balance);
            Assert.AreEqual(3200m, toAccount.PaidIn);
            mockAccountRepository.Verify(x => x.Update(It.Is<Account>(y => y.Equals(toAccount))), Times.Once);
        }
    }
}