using BookShopping1I.Repositories;
using BookShopping1.Models.DTOs;
using BookShopping1.Models;
using BookShopping1.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using BookShopping1.Repositories;

namespace BookShopping1.Controllers;

[Authorize(Roles = nameof(Roles.Admin))]
public class BookController : Controller
{
    private readonly IBookRepository _bookRepo;
    private readonly IGenreRepository _genreRepo;
    private readonly IFileService _fileService;

    public BookController(IBookRepository bookRepo, IGenreRepository genreRepo, IFileService fileService)
    {
        _bookRepo = bookRepo;
        _genreRepo = genreRepo;
        _fileService = fileService;
    }

    public async Task<IActionResult> Index()
    {
        var books = await _bookRepo.GetBooks();
        return View(books);
    }

    public async Task<IActionResult> AddBook()
    {
        var genreSelectList = (await _genreRepo.GetGenres()).Select(genre => new SelectListItem
        {
            Text = genre.GenreName,
            Value = genre.Id.ToString(),
        });
        BookDTO bookToAdd = new() { GenreList = genreSelectList };
        return View(bookToAdd);
    }

    [HttpPost]
    public async Task<IActionResult> AddBook(BookDTO bookToAdd)
    {
        var genreSelectList = (await _genreRepo.GetGenres()).Select(genre => new SelectListItem
        {
            Text = genre.GenreName,
            Value = genre.Id.ToString(),
        });
        bookToAdd.GenreList = genreSelectList;

        if (!ModelState.IsValid)
            return View(bookToAdd);

        try
        {
            // Check if DiscountEndDate is within 7 days from today
            if (bookToAdd.DiscountEndDate.HasValue && bookToAdd.DiscountEndDate.Value > DateTime.Now.AddDays(7))
            {
                ModelState.AddModelError("DiscountEndDate", "Discount End Date cannot be more than 7 days in the future.");
                return View(bookToAdd);
            }

            // Handle Image File
            if (bookToAdd.ImageFile != null)
            {
                if (bookToAdd.ImageFile.Length > 1 * 1024 * 1024) // Limit to 1 MB
                {
                    throw new InvalidOperationException("Image file can not exceed 1 MB");
                }
                string[] allowedExtensions = { ".jpeg", ".jpg", ".png" };
                string imageName = await _fileService.SaveFile(bookToAdd.ImageFile, allowedExtensions);
                bookToAdd.Imaget = imageName;
            }

            // Handle ePub, F2B, Mobi, and PDF Files
            string epubName = null, f2bName = null, mobiName = null, pdfName = null;

            if (bookToAdd.EpubFile != null)
            {
                epubName = await _fileService.SaveFile(bookToAdd.EpubFile, new[] { ".epub" });
            }
            if (bookToAdd.F2bFile != null)
            {
                f2bName = await _fileService.SaveFile(bookToAdd.F2bFile, new[] { ".fb2" });
            }
            if (bookToAdd.MobiFile != null)
            {
                mobiName = await _fileService.SaveFile(bookToAdd.MobiFile, new[] { ".mobi" });
            }
            if (bookToAdd.PdfFile != null)
            {
                pdfName = await _fileService.SaveFile(bookToAdd.PdfFile, new[] { ".pdf" });
            }

            // Save metadata for files in the database
            Book book = new()
            {
                Id = bookToAdd.Id,
                BookName = bookToAdd.BookName,
                AuthorName = bookToAdd.AuthorName,
                Publisher = bookToAdd.Publisher,
                GenreId = bookToAdd.GenreId,
                Price = bookToAdd.Price,
                BorrowPrice = bookToAdd.BorrowPrice,
                Imaget = bookToAdd.Imaget,
                Description = bookToAdd.Description,
                EpubFilePath = epubName,
                F2bFilePath = f2bName,
                MobiFilePath = mobiName,
                PdfFilePath = pdfName,
                YearOfPublishing = bookToAdd.YearOfPublishing,
                AgeLimit = bookToAdd.AgeLimit,
                IsOnSale = bookToAdd.IsOnSale,
                IsBuyOnly = bookToAdd.IsBuyOnly,

                DiscountEndDate = bookToAdd.DiscountEndDate,
                DiscountPrice = bookToAdd.DiscountPrice
            };

            await _bookRepo.AddBook(book);
            TempData["successMessage"] = "Book is added successfully";
            return RedirectToAction(nameof(AddBook));
        }
        catch (Exception ex)
        {
            TempData["errorMessage"] = ex.Message;
            return View(bookToAdd);
        }
    }


    [HttpPost]
    public async Task<IActionResult> UpdateBook(BookDTO bookToUpdate)
    {
        var genreSelectList = (await _genreRepo.GetGenres()).Select(genre => new SelectListItem
        {
            Text = genre.GenreName,
            Value = genre.Id.ToString(),
        });

        bookToUpdate.GenreList = genreSelectList;

        if (!ModelState.IsValid)
            return View(bookToUpdate);

        try
        {
            // Check if DiscountEndDate is within 7 days from today
            if (bookToUpdate.DiscountEndDate.HasValue && bookToUpdate.DiscountEndDate.Value > DateTime.Now.AddDays(7))
            {
                ModelState.AddModelError("DiscountEndDate", "Discount End Date cannot be more than 7 days in the future.");
                return View(bookToUpdate);
            }

            // Image upload
            if (bookToUpdate.ImageFile != null)
            {
                if (bookToUpdate.ImageFile.Length > 1 * 1024 * 1024) // Limit to 1 MB
                {
                    throw new InvalidOperationException("Image file can not exceed 1 MB");
                }

                string[] allowedImageExtensions = { ".jpeg", ".jpg", ".png" };
                string imageName = await _fileService.SaveFile(bookToUpdate.ImageFile, allowedImageExtensions);
                bookToUpdate.Imaget = imageName;
            }

            // Handle ePub, F2B, Mobi, and PDF file uploads
            string epubName = null, f2bName = null, mobiName = null, pdfName = null;

            if (bookToUpdate.EpubFile != null)
            {
                epubName = await _fileService.SaveFile(bookToUpdate.EpubFile, new[] { ".epub" });
            }
            if (bookToUpdate.F2bFile != null)
            {
                f2bName = await _fileService.SaveFile(bookToUpdate.F2bFile, new[] { ".fb2" });
            }
            if (bookToUpdate.MobiFile != null)
            {
                mobiName = await _fileService.SaveFile(bookToUpdate.MobiFile, new[] { ".mobi" });
            }
            if (bookToUpdate.PdfFile != null)
            {
                pdfName = await _fileService.SaveFile(bookToUpdate.PdfFile, new[] { ".pdf" });
            }

            // Update the book metadata
            Book book = new()
            {
                Id = bookToUpdate.Id,
                BookName = bookToUpdate.BookName,
                AuthorName = bookToUpdate.AuthorName,
                Publisher = bookToUpdate.Publisher,
                GenreId = bookToUpdate.GenreId,
                Price = bookToUpdate.Price,
                BorrowPrice = bookToUpdate.BorrowPrice,
                Imaget = bookToUpdate.Imaget,
                Description = bookToUpdate.Description,
                EpubFilePath = epubName,
                F2bFilePath = f2bName,
                MobiFilePath = mobiName,
                PdfFilePath = pdfName,
                YearOfPublishing = bookToUpdate.YearOfPublishing,
                AgeLimit = bookToUpdate.AgeLimit,
                IsOnSale = bookToUpdate.IsOnSale,
                IsBuyOnly = bookToUpdate.IsBuyOnly,
                DiscountEndDate = bookToUpdate.DiscountEndDate,
                DiscountPrice = bookToUpdate.DiscountPrice
            };

            await _bookRepo.UpdateBook(book);
            TempData["successMessage"] = "Book is updated successfully";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["errorMessage"] = ex.Message;
            return View(bookToUpdate);
        }
    }


    public async Task<IActionResult> UpdateBook(int id)
    {
        var bookToUpdate = await _bookRepo.GetBookById(id);
        if (bookToUpdate == null)
        {
            TempData["errorMessage"] = "Book not found.";
            return RedirectToAction(nameof(Index));
        }

        var genreSelectList = (await _genreRepo.GetGenres()).Select(genre => new SelectListItem
        {
            Text = genre.GenreName,
            Value = genre.Id.ToString(),
        });

        BookDTO bookDTO = new()
        {
            Id = bookToUpdate.Id,
            BookName = bookToUpdate.BookName,
            AuthorName = bookToUpdate.AuthorName,
            Publisher = bookToUpdate.Publisher,
            Price = bookToUpdate.Price,
            BorrowPrice = bookToUpdate.BorrowPrice,
            GenreId = bookToUpdate.GenreId,
            Imaget = bookToUpdate.Imaget,
            Description = bookToUpdate.Description,
            GenreList = genreSelectList,
            YearOfPublishing = bookToUpdate.YearOfPublishing,
            AgeLimit = bookToUpdate.AgeLimit,
            IsOnSale = bookToUpdate.IsOnSale,
            IsBuyOnly = bookToUpdate.IsBuyOnly,
            DiscountEndDate = bookToUpdate.DiscountEndDate,
            DiscountPrice = bookToUpdate.DiscountPrice
        };

        return View(bookDTO);
    }

    public async Task<IActionResult> DeleteBook(int id)
    {
        try
        {
            var book = await _bookRepo.GetBookById(id);
            if (book == null)
            {
                TempData["errorMessage"] = $"Book with the id: {id} does not found";
            }
            else
            {
                await _bookRepo.DeleteBook(book);
                if (!string.IsNullOrWhiteSpace(book.Imaget))
                {
                    _fileService.DeleteFile(book.Imaget);
                }
            }
        }
        catch (InvalidOperationException ex)
        {
            TempData["errorMessage"] = ex.Message;
        }
        catch (FileNotFoundException ex)
        {
            TempData["errorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            TempData["errorMessage"] = "Error on deleting the data";
        }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> DownloadFile(int id, string fileType)
    {
        var book = await _bookRepo.GetBookById(id);
        if (book == null)
        {
            TempData["errorMessage"] = "Book not found.";
            return RedirectToAction(nameof(Index));
        }

        string filePath = fileType switch
        {
            "epub" => book.EpubFilePath,
            "fb2" => book.F2bFilePath,
            "mobi" => book.MobiFilePath,
            "pdf" => book.PdfFilePath,
            _ => null
        };

        if (string.IsNullOrEmpty(filePath))
        {
            TempData["errorMessage"] = "File not available.";
            return RedirectToAction(nameof(Index));
        }

        var fileBytes = await System.IO.File.ReadAllBytesAsync(Path.Combine("wwwroot", "files", filePath));
        return File(fileBytes, "application/octet-stream", Path.GetFileName(filePath));
    }

    [HttpPost]
    public async Task<IActionResult> UploadFolder(List<IFormFile> folderUpload)
    {
        // Ensure that at least one file is uploaded
        if (folderUpload == null || folderUpload.Count == 0)
        {
            TempData["errorMessage"] = "No files uploaded.";
            return View();
        }

        try
        {
            // Path where the files will be saved
            string uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            // Create directory if it doesn't exist
            if (!Directory.Exists(uploadDirectory))
            {
                Directory.CreateDirectory(uploadDirectory);
            }

            // Iterate over the uploaded files
            foreach (var file in folderUpload)
            {
                // Validate file extension (for example, allow .epub, .f2b, .mobi, .pdf)
                string[] allowedExtensions = { ".epub", ".fb2", ".mobi", ".pdf" };
                string fileExtension = Path.GetExtension(file.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    TempData["errorMessage"] = "Invalid file type. Only .epub, .fb2, .mobi, .pdf are allowed.";
                    return View();
                }

                // Create a unique file name and save the file
                string filePath = Path.Combine(uploadDirectory, file.FileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
            }

            TempData["successMessage"] = "Files uploaded successfully.";
            return RedirectToAction("Index"); // Or redirect to any other action
        }
        catch (Exception ex)
        {
            TempData["errorMessage"] = $"Error: {ex.Message}";
            return View();
        }
    }
}