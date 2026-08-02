namespace TicketReservationSystem.Domain.ValueObjects
{
    using TicketReservationSystem.Domain.Exceptions;

        public record struct Money
        {
            public decimal Amount { get; }
            public string Currency { get; }

            public Money(decimal amount, string currency)
            {
                Amount = amount;
                Currency = currency;
            }

            public Money Add(Money other)
            {
                if (Currency != other.Currency)
                    throw new CurrencyMismatchException();

                return new(Amount + other.Amount, Currency);
            }

            public Money Subtract(Money other)
            {
                if (Currency != other.Currency)
                    throw new CurrencyMismatchException();

                return new(Amount - other.Amount, Currency);
            }

            public Money Multiply(int factor)
            {
                return new(Amount * factor, Currency);
            }

            public override string ToString()
            {
                return $"{Amount:C} {Currency}";
            }

        }
}
