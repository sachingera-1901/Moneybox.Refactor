using Moneybox.App.Domain.Services;
using System;

namespace Moneybox.App
{
    public class Account
    {
        public const decimal PayInLimit = 4000m;

        private readonly decimal lowFundsLimit;
        private INotificationService notificationService;

        public Account(INotificationService notificationService)
        {
            this.notificationService = notificationService;
            lowFundsLimit = 500m;
        }

        public Guid Id { get; set; }

        public User User { get; set; }

        public decimal Balance { get; set; }

        public decimal Withdrawn { get; set; }

        public decimal PaidIn { get; set; }

        public void PerformChecksToWithdrawMoney(decimal amount)
        {
            var fromBalance = Balance - amount;
            if (fromBalance < 0m)
            {
                throw new InvalidOperationException("Insufficient funds to make transfer");
            }

            if (fromBalance < lowFundsLimit)
            {
                this.notificationService.NotifyFundsLow(User.Email);
            }
        }
       
        public void WithdrawMoney(decimal amount)
        {
            Balance -= amount;
            Withdrawn -= amount;
        }

        public void PerformChecksToPayInMoney(decimal amount)
        {
            var paidIn = PaidIn + amount;
            if (paidIn > PayInLimit)
            {
                throw new InvalidOperationException("Account pay in limit reached");
            }

            if (PayInLimit - paidIn < lowFundsLimit)
            {
                this.notificationService.NotifyApproachingPayInLimit(User.Email);
            }
        }

        public void PayInMoney(decimal amount)
        {
            Balance += amount;
            PaidIn += amount;
        }
    }
}
