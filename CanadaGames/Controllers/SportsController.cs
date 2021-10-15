using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CanadaGames.Data;
using CanadaGames.Models;
using CanadaGames.Utilities;
using CanadaGames.ViewModels;
using Microsoft.EntityFrameworkCore.Storage;

namespace CanadaGames.Controllers
{
    public class SportsController : Controller
    {
        private readonly CanadaGamesContext _context;

        public SportsController(CanadaGamesContext context)
        {
            _context = context;
        }

        // GET: Sports
        public async Task<IActionResult> Index(int? page, int? pageSizeID, string actionButton, string sortDirection = "asc", string sortField = "Sport")
        {
            string[] sortOptions = new[] { "Name" };

            var sports = from c in _context.Sports
                         .Include(c =>c.AthleteSports)
                         .ThenInclude(c => c.Athlete)
                          select c;

            if (!String.IsNullOrEmpty(actionButton)) //Form Submitted!
            {
                page = 1;//Reset page to start
            }

            //checking to see if there was a change in sorting
            if (!String.IsNullOrEmpty(actionButton))
            {
                if (sortOptions.Contains(actionButton))
                {
                    if (actionButton == sortField)
                    {
                        sortDirection = sortDirection == "asc" ? "desc" : "asc";
                    }
                    sortField = actionButton;
                }
            }
            //now the sort direction and field are known
            if (sortField == "Name")
            {
                if (sortDirection == "asc")
                {
                    sports = sports
                        .OrderByDescending(s => s.Name);
                }
                else
                {
                    sports = sports
                        .OrderBy(s => s.Name);
                }
            }

            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;

            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID);
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);
            var pagedData = await PaginatedList<Sport>.CreateAsync(sports.AsNoTracking(), page ?? 1, pageSize);

            return View(pagedData);
        }

        // GET: Sports/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sport = await _context.Sports
                .Include(c => c.AthleteSports)
                .ThenInclude(c => c.Athlete)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ID == id);
            if (sport == null)
            {
                return NotFound();
            }

            return View(sport);
        }

        // GET: Sports/Create
        public IActionResult Create()
        {
            Sport sport = new Sport();
            PopulateAssignedSportData(sport);
            return View();
        }

        // POST: Sports/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Name")] Sport sport, string[] selectedOptions)
        {
            try
            {
                UpdateSports(selectedOptions, sport);
                if (ModelState.IsValid)
                {
                    _context.Add(sport);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Details", new { sport.ID});
                }
            }
            catch (RetryLimitExceededException /* dex */)
            {
                ModelState.AddModelError("", "Unable to save changes after multiple attempts. Try again, and if the problem persists see your system administrator.");
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
            }
            PopulateAssignedSportData(sport);
            return View(sport);
        }

        // GET: Sports/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sport = await _context.Sports
                .Include(c => c.AthleteSports)
                .ThenInclude(c => c.Athlete)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (sport == null)
            {
                return NotFound();
            }

            PopulateAssignedSportData(sport);
            return View(sport);
        }

        // POST: Sports/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Code,Name")] Sport sport, string[] selectedOptions)
        {
            var sportToUpdate = await _context.Sports
                .Include(c => c.AthleteSports)
                .ThenInclude(c => c.Athlete)
                .FirstOrDefaultAsync(m => m.ID == id);

            if (id != sport.ID)
            {
                return NotFound();
            }
            UpdateSports(selectedOptions, sportToUpdate);

            if (await TryUpdateModelAsync<Sport>(sport))
            {
                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Details", new { sport.ID });
                }
                catch (RetryLimitExceededException /* dex */)
                {
                    ModelState.AddModelError("", "Unable to save changes after multiple attempts. Try again, and if the problem persists see your system administrator.");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SportExists(sport.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                }
            }
            PopulateAssignedSportData(sportToUpdate);
            return View(sportToUpdate);
        }

        // GET: Sports/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sport = await _context.Sports
                .Include(c => c.AthleteSports)
                .ThenInclude(c => c.Athlete)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ID == id);
            if (sport == null)
            {
                return NotFound();
            }

            return View(sport);
        }

        // POST: Sports/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sport = await _context.Sports
                .Include(c => c.AthleteSports)
                .ThenInclude(c => c.Athlete)
                .FirstOrDefaultAsync(m => m.ID == id);

            try
            {
                _context.Sports.Remove(sport);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException dex)
            {
                if (dex.GetBaseException().Message.Contains("FOREIGN KEY constraint failed"))
                {
                    ModelState.AddModelError("", "Unable to Delete Sport. Remember, you cannot delete a Sport with Athletes.");
                }
                else
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                }
            }
            return View(sport);
        }

        private void PopulateAssignedSportData(Sport sport)
        {
            var allOptions = _context.Athletes;
            var currentOptionsHS = new HashSet<int>(sport.AthleteSports.Select(a => a.AthleteID));
            var selected = new List<ListOptionVM>();
            var available = new List<ListOptionVM>();
            foreach (var s in allOptions)
            {
                if (currentOptionsHS.Contains(s.ID))
                {
                    selected.Add(new ListOptionVM
                    {
                        ID = s.ID,
                        DisplayText = s.FullName
                    });
                }
                else
                {
                    available.Add(new ListOptionVM
                    {
                        ID = s.ID,
                        DisplayText = s.FullName
                    });
                }
            }

            ViewData["selOpts"] = new MultiSelectList(selected.OrderBy(s => s.DisplayText), "ID", "DisplayText");
            ViewData["availOpts"] = new MultiSelectList(available.OrderBy(s => s.DisplayText), "ID", "DisplayText");
        }
        private void UpdateSports(string[] selectedOptions, Sport athleteToUpdate)
        {
            if (selectedOptions == null)
            {
                athleteToUpdate.AthleteSports = new List<AthleteSport>();
                return;
            }

            var selectedOptionsHS = new HashSet<string>(selectedOptions);
            var currentOptionsHS = new HashSet<int>(athleteToUpdate.AthleteSports.Select(a => a.SportID));
            foreach (var s in _context.Sports)
            {
                if (selectedOptionsHS.Contains(s.ID.ToString()))
                {
                    if (!currentOptionsHS.Contains(s.ID))
                    {
                        athleteToUpdate.AthleteSports.Add(new AthleteSport
                        {
                            AthleteID = s.ID,
                            SportID = athleteToUpdate.ID
                        });
                    }
                }
                else
                {
                    if (currentOptionsHS.Contains(s.ID))
                    {
                        AthleteSport sportToRemove = athleteToUpdate.AthleteSports.FirstOrDefault(a => a.SportID == s.ID);
                        _context.Remove(sportToRemove);
                    }
                }
            }
        }

        private bool SportExists(int id)
        {
            return _context.Sports.Any(e => e.ID == id);
        }
    }
}
