namespace Library.Models.Excel
{
    public class Transaction
    {
        public uint Id { get; set; }

        public string UserName { get; set; }

        public string Title { get; set; }

        public uint Day { get; set; }

        public decimal Total { get; set; }

        public DateTime StartAt { get; set; }

        public DateTime EndAt { get; set; }

        public DateTime? ReturnedAt { get; set; }
    }
}
