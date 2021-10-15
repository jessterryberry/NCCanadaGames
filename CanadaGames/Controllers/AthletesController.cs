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
    public class AthletesController : Controller
    {
        private readonly CanadaGamesContext _context;

        public AthletesController(CanadaGamesContext context)
        {
            _context = context;
        }

        // GET: Athletes
        public async Task<IActionResult> Index(int? ContingentID, int? SportID, int? GenderID, int? CoachID, int? page, int? pageSizeID,
            string SearchAthlete, string SearchMedia, string actionButton, string sortDirection = "asc", string sortField = "Athlete")
        {
            //Toggle the Open/Closed state of the collapse 
            ViewData["Filtering"] = "";

            string[] sortOptions = new[] { "Athlete", "Age", "Contingent", "Sport" };

            PopulateDropDownLists();

            var athletes = from a in _context.Athletes
                .Include(a => a.Coach)
                .Include(a => a.Contingent)
                .Include(a => a.Gender)
                .Include(a => a.Sport)
                .Include(a => a.AthleteSports)
                .ThenInclude(s => s.Sport)
                select a;
            //applying the filters
            if (ContingentID.HasValue)
            {
                athletes = athletes.Where(a => a.ContingentID == ContingentID);
                ViewData["Filtering"] = " show";
            }
            if (SportID.HasValue)
            {
                athletes = athletes.Where(a => a.SportID == SportID);
                ViewData["Filtering"] = " show";
            }
            if (GenderID.HasValue)
            {
                athletes = athletes.Where(a => a.GenderID == GenderID);
                ViewData["Filtering"] = " show";
            }
            if (CoachID.HasValue)
            {
                athletes = athletes.Where(a => a.CoachID == CoachID);
                ViewData["Filtering"] = " show";
            }
            if (!String.IsNullOrEmpty(SearchAthlete))
            {
                athletes = athletes.Where(a => a.LastName.ToUpper().Contains(SearchAthlete.ToUpper())
                                       || a.FirstName.ToUpper().Contains(SearchAthlete.ToUpper()));
                ViewData["Filtering"] = " show";
            }
            if (!String.IsNullOrEmpty(SearchMedia))
            {
                athletes = athletes.Where(a => a.MediaInfo.ToUpper().Contains(SearchMedia.ToUpper()));
                ViewData["Filtering"] = " show";
            }

            //checking to see if there was a change of filtering or sorting first
            if (!String.IsNullOrEmpty(actionButton))
            {
                page = 1; //resetting the page

                if (sortOptions.Contains(actionButton))
                {
                    if (actionButton == sortField)
                    {
                        sortDirection = sortDirection == "asc" ? "desc" : "asc";
                    }
                    sortField = actionButton;
                }
            }
            //sort direction and field is complete
            if (sortField == "Athlete")
            {
                if (sortDirection == "asc")
                {
                    athletes = athletes
                        .OrderBy(a => a.LastName)
                        .ThenBy(a => a.FirstName);
                }
                else
                {
                    athletes = athletes
                        .OrderByDescending(a => a.LastName)
                        .ThenByDescending(a => a.FirstName);
                }
            }
            else if (sortField == "Age")
            {
                if (sortDirection == "asc")
                {
                    athletes = athletes
                        .OrderByDescending(a => a.DOB);
                }
                else
                {
                    athletes = athletes
                        .OrderBy(a => a.DOB);
                }
            }
            else if (sortField == "Contingent")
            {
                if (sortDirection == "asc")
                {
                    athletes = athletes
                        .OrderByDescending(a => a.Contingent);
                }
                else
                {
                    athletes = athletes
                        .OrderBy(a => a.Contingent);
                }
            }
            else
            {
                if (sortDirection == "asc")
                {
                    athletes = athletes
                        .OrderByDescending(a => a.Sport);
                }
                else
                {
                    athletes = athletes
                        .OrderBy(a => a.Sport);
                }
            }

            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;

            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID);
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);

            var pagedData = await PaginatedList<Athlete>.CreateAsync(athletes.AsNoTracking(), page ?? 1, pageSize);

            return View(pagedData);
        }
        

        // GET: Athletes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var athlete = await _context.Athletes
                .Include(a => a.Coach)
                .Include(a => a.Contingent)
                .Include(a => a.Gender)
                .Include(a => a.Sport)
                .Include(a => a.AthleteSports)
                .ThenInclude(s => s.Sport)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ID == id);
            if (athlete == null)
            {
                return NotFound();
            }

            return View(athlete);
        }

        // GET: Athletes/Create
        public IActionResult Create()
        {
            var athlete = new Athlete();
            PopulateOtherSportData(athlete);
            PopulateDropDownLists();
            return View();
        }

        // POST: Athletes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,FirstName,MiddleName,LastName,AthleteCode,Hometown,DOB,Height,Weight,YearsInSport,Affiliation," +
            "Goals,MediaInfo,ContingentID,SportID,GenderID,CoachID")] Athlete athlete, string[] selectdOptions)
        {
            try
            {
                //adding selected sports
                if (selectdOptions != null)
                {
                    foreach (var sport in selectdOptions)
                    {
                        var sportToAdd = new AthleteSport { AthleteID = athlete.ID, SportID = int.Parse(sport) };
                        athlete.AthleteSports.Add(sportToAdd);
                    }
                }
                if (ModelState.IsValid)
                {
                    _context.Add(athlete);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (RetryLimitExceededException /* dex */)
            {
                ModelState.AddModelError("", "Unable to save changes after multiple attempts. Try again, and if the problem persists, see your system administrator.");
            }
            catch (DbUpdateException dex)
            {
                if (dex.GetBaseException().Message.Contains("UNIQUE constraint failed"))
                {
                    ModelState.AddModelError("AthleteCode", "Unable to save changes. Remember, you cannot have duplicate Athlete Codes.");
                }
                else
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                }
            }

            PopulateOtherSportData(athlete);
            PopulateDropDownLists(athlete);
            return View(athlete);
        }

        // GET: Athletes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var athlete = await _context.Athletes
                .Include(a => a.AthleteSports)
                .ThenInclude(a => a.Sport)
                .FirstOrDefaultAsync(a => a.ID == id);
            if (athlete == null)
            {
                return NotFound();
            }

            PopulateOtherSportData(athlete);
            PopulateDropDownLists(athlete);
            return View(athlete);
        }

        // POST: Athletes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string[] selectedOptions, Byte[] RowVersion)
        {
            //Go get the athlete to update
            var athleteToUpdate = await _context.Athletes
                .Include(a => a.AthleteSports)
                .ThenInclude(a => a.Sport)
                .FirstOrDefaultAsync(a => a.ID == id); ;

            //Check that you got it or exit with a not found error
            if (athleteToUpdate == null)
            {
                return NotFound();
            }

            //update the athlete's sports
            UpdateAthleteSports(selectedOptions, athleteToUpdate);

            _context.Entry(athleteToUpdate).Property("RowVersion").OriginalValue = RowVersion;

            //Try updating it with the values posted
            if (await TryUpdateModelAsync<Athlete>(athleteToUpdate, "",
                p => p.AthleteCode, p => p.FirstName, p => p.MiddleName, p => p.LastName, p => p.DOB,
                p => p.Height, p => p.Weight, p => p.YearsInSport, p => p.Affiliation, p => p.Goals,
                p => p.MediaInfo, p => p.ContingentID, p => p.GenderID, p => p.CoachID, p => p.SportID))
            {
                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (RetryLimitExceededException /* dex */)
                {
                    ModelState.AddModelError("", "Unable to save changes after multiple attempts. Try again, and if the problem persists, see your system administrator.");
                }
                catch (DbUpdateConcurrencyException ex)// Added for concurrency
                {
                    var exceptionEntry = ex.Entries.Single();
                    var athleteValues = (Athlete)exceptionEntry.Entity;
                    var databaseEntry = exceptionEntry.GetDatabaseValues();
                    if (databaseEntry == null)
                    {
                        ModelState.AddModelError("",
                            "Unable to save changes. The Athlete was deleted by another user.");
                    }
                    else
                    {
                        var databaseValues = (Athlete)databaseEntry.ToObject();
                        if (databaseValues.FirstName != athleteValues.FirstName)
                            ModelState.AddModelError("FirstName", "Current value: "
                                + databaseValues.FirstName);

                        if (databaseValues.MiddleName != athleteValues.MiddleName)
                            ModelState.AddModelError("MiddleName", "Current value: "
                                + databaseValues.MiddleName);

                        if (databaseValues.LastName != athleteValues.LastName)
                            ModelState.AddModelError("LastName", "Current value: "
                                + databaseValues.LastName);

                        if (databaseValues.AthleteCode != athleteValues.AthleteCode)
                            ModelState.AddModelError("AthleteCode", "Current value: "
                                + databaseValues.AthleteCode);

                        if (databaseValues.DOB != athleteValues.DOB)
                            ModelState.AddModelError("DOB", "Current value: "
                                + String.Format("{0:d}", databaseValues.DOB));

                        if (databaseValues.Height != athleteValues.Height)
                            ModelState.AddModelError("Height", "Current value: "
                                + databaseValues.Height);

                        if (databaseValues.Weight != athleteValues.Weight)
                            ModelState.AddModelError("Weight", "Current value: "
                                + databaseValues.Weight);

                        if (databaseValues.YearsInSport != athleteValues.YearsInSport)
                            ModelState.AddModelError("YearsInSport", "Current value: "
                                + databaseValues.YearsInSport);

                        if (databaseValues.Affiliation != athleteValues.Affiliation)
                            ModelState.AddModelError("Affiliation", "Current value: "
                                + databaseValues.Affiliation);

                        if (databaseValues.Goals != athleteValues.Goals)
                            ModelState.AddModelError("Goals", "Current value: "
                                + databaseValues.Goals);

                        if (databaseValues.MediaInfo != athleteValues.MediaInfo)
                            ModelState.AddModelError("MediaInfo", "Current value: "
                                + databaseValues.MediaInfo);

                        //For the foreign keys
                        if (databaseValues.ContingentID != athleteValues.ContingentID)
                        {
                            Contingent databaseContingent = await _context.Contingents.SingleOrDefaultAsync(i => i.ID == databaseValues.ContingentID);
                            ModelState.AddModelError("ContingentID", $"Current value: {databaseContingent?.Name}");
                        }

                        if (databaseValues.GenderID != athleteValues.GenderID)
                        {
                            Gender databaseGender = await _context.Genders.SingleOrDefaultAsync(i => i.ID == databaseValues.GenderID);
                            ModelState.AddModelError("GenderID", $"Current value: {databaseGender?.Name}");
                        }

                        if (databaseValues.SportID != athleteValues.SportID)
                        {
                            Sport databaseSport = await _context.Sports.SingleOrDefaultAsync(i => i.ID == databaseValues.SportID);
                            ModelState.AddModelError("SportID", $"Current value: {databaseSport?.Name}");
                        }

                        if (databaseValues.CoachID != athleteValues.CoachID)
                        {
                            if (databaseValues.CoachID.HasValue)
                            {
                                Coach databaseCoach = await _context.Coaches.SingleOrDefaultAsync(i => i.ID == databaseValues.CoachID);
                                ModelState.AddModelError("CoachID", $"Current value: {databaseCoach?.FullName}");
                            }
                            else
                            {
                                ModelState.AddModelError("CoachID", $"Current value: None");
                            }
                        }
                        ModelState.AddModelError(string.Empty, "The record you attempted to edit "
                                + "was modified by another user after you received your values. The "
                                + "edit operation was canceled and the current values in the database "
                                + "have been displayed. If you still want to save your version of this record, click "
                                + "the Save button again. Otherwise click the 'Back to List' hyperlink.");
                        athleteToUpdate.RowVersion = (byte[])databaseValues.RowVersion;
                        ModelState.Remove("RowVersion");
                    }
                }
                catch (DbUpdateException dex)
                {
                    if (dex.GetBaseException().Message.Contains("UNIQUE constraint failed"))
                    {
                        ModelState.AddModelError("AthleteCode", "Unable to save changes. Remember, you cannot have duplicate Athlete Codes.");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                    }
                }
            }

            PopulateOtherSportData(athleteToUpdate);
            PopulateDropDownLists(athleteToUpdate);
            return View(athleteToUpdate);
        }

        // GET: Athletes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var athlete = await _context.Athletes
                .Include(a => a.Coach)
                .Include(a => a.Contingent)
                .Include(a => a.Gender)
                .Include(a => a.Sport)
                .Include(a => a.AthleteSports)
                .ThenInclude(s => s.Sport)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ID == id);
            if (athlete == null)
            {
                return NotFound();
            }

            return View(athlete);
        }

        // POST: Athletes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var athlete = await _context.Athletes
               .Include(a => a.Coach)
               .Include(a => a.Contingent)
               .Include(a => a.Gender)
               .Include(a => a.Sport)
               .Include(a => a.AthleteSports)
               .ThenInclude(s => s.Sport)
               .FirstOrDefaultAsync(m => m.ID == id); 

            try
            {
                _context.Athletes.Remove(athlete);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                //Note: there is really no reason a delete should fail if you can "talk" to the database.
                ModelState.AddModelError("", "Unable to delete record. Try again, and if the problem persists see your system administrator.");
            }
            return View(athlete);

        }

        private void PopulateOtherSportData(Athlete athlete)
        {
            var allOptions = _context.Sports;
            var currentOptionIDs = new HashSet<int>(athlete.AthleteSports.Select(a => a.SportID));
            var checkBoxes = new List<OptionVM>();
            foreach (var option in allOptions)
            {
                checkBoxes.Add(new OptionVM
                {
                    ID = option.ID,
                    DisplayText = option.Name,
                    Assigned = currentOptionIDs.Contains(option.ID)
                });
            }
            ViewData["SportOptions"] = checkBoxes;
        }
        private void UpdateAthleteSports(string[] selectedOptions, Athlete athleteToUpdate)
        {
            if (selectedOptions == null)
            {
                athleteToUpdate.AthleteSports = new List<AthleteSport>();
                return;
            }

            var selectedOptionsHS = new HashSet<string>(selectedOptions);
            var athleteOptionsHS = new HashSet<int>
                (athleteToUpdate.AthleteSports.Select(c => c.SportID));
            foreach (var option in _context.Sports)
            {
                if (selectedOptionsHS.Contains(option.ID.ToString()))
                {
                    if (!athleteOptionsHS.Contains(option.ID))
                    {
                        athleteToUpdate.AthleteSports.Add(new AthleteSport { AthleteID = athleteToUpdate.ID, SportID = option.ID });
                    }
                }
                else
                {
                    if (athleteOptionsHS.Contains(option.ID))
                    {
                        AthleteSport sportToRemove = athleteToUpdate.AthleteSports.SingleOrDefault(c => c.SportID == option.ID);
                        _context.Remove(sportToRemove);
                    }
                }
            }
        }

        private SelectList CoachSelectList(int? selectedId)
        {
            return new SelectList(_context.Coaches
                .OrderBy(d => d.LastName)
                .ThenBy(d => d.FirstName), "ID", "FormalName", selectedId);
        }
        private SelectList ContingentSelectList(int? selectedId)
        {
            return new SelectList(_context.Contingents
                .OrderBy(d => d.Name), "ID", "Name", selectedId);
        }
        private SelectList GenderSelectList(int? selectedId)
        {
            return new SelectList(_context.Genders
                .OrderBy(d => d.Name), "ID", "Name", selectedId);
        }
        private SelectList SportSelectList(int? selectedId)
        {
            return new SelectList(_context.Sports
                .OrderBy(d => d.Name), "ID", "Name", selectedId);
        }
        private void PopulateDropDownLists(Athlete athlete = null)
        {
            ViewData["CoachID"] = CoachSelectList(athlete?.CoachID);
            ViewData["ContingentID"] = ContingentSelectList(athlete?.ContingentID);
            ViewData["GenderID"] = GenderSelectList(athlete?.GenderID);
            ViewData["SportID"] = SportSelectList(athlete?.SportID);
        }

        private bool AthleteExists(int id)
        {
            return _context.Athletes.Any(e => e.ID == id);
        }
    }
}
