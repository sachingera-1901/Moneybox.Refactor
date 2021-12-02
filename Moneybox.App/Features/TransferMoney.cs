using Moneybox.App.DataAccess;
using Moneybox.App.Domain.Services;
using System;

namespace Moneybox.App.Features
{
    public class TransferMoney
    {
        private IAccountRepository accountRepository;

        public TransferMoney(IAccountRepository accountRepository)
        {
            this.accountRepository = accountRepository;
        }

        public void Execute(Guid fromAccountId, Guid toAccountId, decimal amount)
        {
            var from = accountRepository.GetAccountById(fromAccountId);
            var to = accountRepository.GetAccountById(toAccountId);

            from.PerformChecksToWithdrawMoney(amount);
            to.PerformChecksToPayInMoney(amount);
            from.WithdrawMoney(amount);
            to.PayInMoney(amount);

            accountRepository.Update(from);
            accountRepository.Update(to);
        }
    }
}
