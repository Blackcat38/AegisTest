using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Spreadsheet;
using Library.Data;
using Library.Models;
using Library.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;

namespace Library.Controllers
{
    public class TransactionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TransactionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(string search)
        {
            var sql = "exec get_transactions";

            var StartParam = new SqlParameter("@StartAt", "");
            var EndParam = new SqlParameter("@EndAt", "");
            if (search != null)
            {
                sql = sql + " @StartAt, @EndAt";
                var searchDate = search.Split('-').ToList();
                StartParam = new SqlParameter("@StartAt", DateTime.ParseExact(searchDate[0].Trim(), "MM/dd/yyyy", null));
                EndParam = new SqlParameter("@EndAt", DateTime.ParseExact(searchDate[1].Trim(), "MM/dd/yyyy", null));
            }

            var q = await _context.GetTransactions.FromSqlRaw(sql, StartParam, EndParam).ToListAsync();

            return _context.GetTransactions != null ?
                          View(q) :
                          Problem("Entity set 'ApplicationDbContext.GetTransactions' is null.");
        }

        [Authorize(Roles = "User")]
        public async Task<IActionResult> Borrow(string search)
        {
            var UserId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var sql = "exec get_transactions @UserId, @Status";

            var UserIdParam = new SqlParameter("@UserId", UserId);
            var StatusParam = new SqlParameter("@Status", "0");
            var StartParam = new SqlParameter("@StartAt", "");
            var EndParam = new SqlParameter("@EndAt", "");
            if (search != null)
            {
                sql = sql + ", @StartAt, @EndAt";
                var searchDate = search.Split('-').ToList();
                StartParam = new SqlParameter("@StartAt", DateTime.ParseExact(searchDate[0].Trim(), "MM/dd/yyyy", null));
                EndParam = new SqlParameter("@EndAt", DateTime.ParseExact(searchDate[1].Trim(), "MM/dd/yyyy", null));
            }
            
            var q = await _context.GetTransactions.FromSqlRaw(sql, UserIdParam, StatusParam, StartParam, EndParam).ToListAsync();

            return _context.GetTransactions != null ?
                          View(q) :
                          Problem("Entity set 'ApplicationDbContext.GetTransactions' is null.");
        }

        public async Task<IActionResult> Create()
        {
            var books = await _context.Books.Where(q => q.Quantity > 0).ToListAsync();
            ViewData["Books"] = new SelectList(books, "Id", "Title");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BookId,Day")] Transaction transaction)
        {
            var UserId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            var StartAt = DateTime.Now;

            var EndAt = StartAt.AddDays(transaction.Day);

            var endpoint = new Calculator.CalculatorSoapClient.EndpointConfiguration();

            var calculator = new Calculator.CalculatorSoapClient(endpoint);

            var book = await _context.Books
                .Where(q => q.Quantity > 0)
                .Where(q => q.Id == transaction.BookId)
                .FirstAsync();

            var total = await calculator.MultiplyAsync((int)book.Price, (int)transaction.Day);

            transaction.UserId = UserId;
            transaction.Status = 0;
            transaction.Total = total;
            transaction.StartAt = StartAt;
            transaction.EndAt = EndAt;

            var context = new ValidationContext(transaction);
            var validationResults = new List<ValidationResult>();

            bool isValid = Validator.TryValidateObject(transaction, context, validationResults, true);

            if (isValid)
            {
                book.Quantity = book.Quantity - 1;
                _context.Books.Update(book);
                await _context.SaveChangesAsync();
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Borrow));
            }
            return View(transaction);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> CalculateBookPrice([FromBody] CalculateBookPriceReq Req)
        {
            var endpoint = new Calculator.CalculatorSoapClient.EndpointConfiguration();

            var calculator = new Calculator.CalculatorSoapClient(endpoint);

            var book = await _context.Books
                .Where(q => q.Quantity > 0)
                .Where(q => q.Id == Req.BookId)
                .FirstAsync();

            if (book == null)
            {
                return Json(new { Price = 0, Total = 0 });
            }

            var value = await calculator.MultiplyAsync((int) book.Price, (int) Req.Day);

            return Json(new { Price = book.Price, Total = value });
        }

        public async Task<IActionResult> Return(uint? id)
        {
            if (id == null || _context.Books == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .FirstOrDefaultAsync(m => m.Id == id);
            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        [HttpPost, ActionName("Return")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnConfirmed(uint id)
        {
            if (_context.Transactions == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Transactions'  is null.");
            }
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction != null)
            {
                var book = await _context.Books.FindAsync(transaction.BookId);
                if (book != null)
                {
                    transaction.Status = 1;
                    transaction.ReturnedAt = DateTime.Now;
                    _context.Transactions.Update(transaction);
                    book.Quantity = book.Quantity + 1;
                    _context.Books.Update(book);
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Borrow));
        }
    }
}
