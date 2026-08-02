namespace TicketReservationSystem.Domain.ValueObjects
{
    public readonly record struct DateTimeRange
    {
        public DateTime StartTime { get; }
        public DateTime EndTime { get; }

        public DateTimeRange(DateTime startTime, DateTime endTime)
        {
            if (endTime < startTime) throw new ArgumentException("End time should be later than start time.");
            StartTime = startTime;
            EndTime = endTime;
        }

        public TimeSpan Duration => EndTime - StartTime;

    }
}
