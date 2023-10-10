using ClosedXML.Excel;
using Library.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Library.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index(string search)
        {
            var UserId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            var UserRole = await _context.UserRoles.Where(q => q.UserId == UserId).FirstAsync();

            var Role = await _context.Roles.Where(q => q.Id == UserRole.RoleId).FirstAsync();

            var sql = "exec get_transactions";

            var UserIdParam = new SqlParameter("@UserId", "");

            if (Role.Name != "Admin")
            {
                sql += " @UserId";

                UserIdParam = new SqlParameter("@UserId", UserId);
            }

            var q = await _context.GetTransactions.FromSqlRaw(sql, UserIdParam).ToListAsync();

            return _context.GetTransactions != null ?
                          View(q) :
                          Problem("Entity set 'ApplicationDbContext.GetTransactions' is null.");
        }


        [Authorize(Roles = "Admin")]
        public IActionResult DownloadReport()
        {
            string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            string fileName = "peminjaman.xlsx";
            try
            {
                var data = _context.GetTransactions.FromSqlRaw("exec get_transactions").ToList();

                using (var workbook = new XLWorkbook())
                {
                    IXLWorksheet worksheet = workbook.Worksheets.Add("Data");
                    worksheet.Cell(1, 1).Value = "Peminjam";
                    worksheet.Cell(1, 2).Value = "Judul";
                    worksheet.Cell(1, 3).Value = "Lama Durasi (Hari)";
                    worksheet.Cell(1, 4).Value = "Jumlah Bayar";
                    worksheet.Cell(1, 5).Value = "Tanggal Peminjaman";
                    worksheet.Cell(1, 6).Value = "Tanggal Pengembalian";
                    worksheet.Cell(1, 7).Value = "Status";
                    for (int index = 1; index <= data.Count; index++)
                    {
                        worksheet.Cell(index + 1, 1).Value = data[index - 1].UserName;
                        worksheet.Cell(index + 1, 2).Value = data[index - 1].Title;
                        worksheet.Cell(index + 1, 3).Value = data[index - 1].Day;
                        worksheet.Cell(index + 1, 4).Value = data[index - 1].Total;
                        worksheet.Cell(index + 1, 5).Value = data[index - 1].StartAt;
                        if (data[index - 1].ReturnedAt != null)
                        {
                            worksheet.Cell(index + 1, 6).Value = data[index - 1].ReturnedAt;
                        }
                        else
                        {
                            worksheet.Cell(index + 1, 6).Value = data[index - 1].EndAt;

                        }
                        if (data[index -1].Status == 0)
                        {
                            worksheet.Cell(index + 1, 7).Value = "Dipinjam";
                        }
                        else
                        {
                            if (data[index - 1].DayLeft >= 0)
                            {
                                worksheet.Cell(index + 1, 7).Value = "Tepat Waktu";
                            }
                            else
                            {
                                worksheet.Cell(index + 1, 7).Value = "Terlambat";

                            }
                        }
                    }

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();
                        return File(content, contentType, fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                return Ok(ex);
            }
        }
    }
}
