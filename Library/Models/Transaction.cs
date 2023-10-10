using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Library.Models
{
    public class Transaction
    {
        public uint Id { get; set; }

        public string UserId {  get; set; }

        public uint BookId {  get; set; }

        [Range(1, uint.MaxValue)]
        public uint Day { get; set; }

        public decimal Total {  get; set; }

        public DateTime StartAt {  get; set; }

        public DateTime EndAt {  get; set; }

        public DateTime? ReturnedAt {  get; set; }

        public UInt16 Status { get; set; }

        public Book? Book { get; set; }

        public IdentityUser? User { get; set; }
    }
}
