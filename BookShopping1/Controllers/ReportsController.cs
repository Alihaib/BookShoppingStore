using BookShopping1.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BookShopping1.Controllers;
public class ReportsController : Controller
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IReportRepository _reportRepository;
    public ReportsController(IReportRepository reportRepository, IPaymentRepository paymentRepository)
    {
        _reportRepository = reportRepository;
        _paymentRepository = paymentRepository;
    }

    public async Task<ActionResult> TopFiveSellingBooks(DateTime? sDate = null, DateTime? eDate = null)
    {
        try
        {
           
            DateTime startDate = sDate ?? DateTime.UtcNow.AddDays(-7);
            DateTime endDate = eDate ?? DateTime.UtcNow;
            var topFiveSellingBooks = await _reportRepository.GetTopNSellingBooksByDate(startDate, endDate);
            var vm = new TopNSoldBooksVm(startDate, endDate, topFiveSellingBooks);
            return View(vm);
        }
        catch (Exception ex)
        {
            TempData["errorMessage"] = "Something went wrong";
            return RedirectToAction("Index", "Home");
        }
    }
}