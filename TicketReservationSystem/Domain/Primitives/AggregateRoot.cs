namespace TicketReservationSystem.Domain.Primitives
{
    using System.Collections.Generic;
    using TicketReservationSystem.Domain.Events;

        public abstract class AggregateRoot<TKey> : Entity<TKey> where TKey : notnull
        {
            private bool _isDeleted;
            public bool IsDeleted => _isDeleted;

            protected AggregateRoot() { }
            protected AggregateRoot(TKey id) : base(id)
            {
            
            }

            public void MarkAsDeleted()
            {
                _isDeleted = true;
            }

            public void UnmarkAsDeleted()
            {
                _isDeleted = false;
            }

            protected virtual void EnforceInvariants()
            {
            }

            public void ApplyChanges()
            {
                EnforceInvariants();
            }
        }
}
