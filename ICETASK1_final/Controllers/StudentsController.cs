using Microsoft.AspNetCore.Mvc;
using Azure;
using Azure.Data.Tables;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace ICETASK1.Controllers
{
    public class StudentEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "StudentPartition";
        public string RowKey { get; set; } = default!;
        public string StudentName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string EnrolledCourses { get; set; } = "";
        public ETag ETag { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }

    public class StudentsController : Controller
    {
        private readonly TableClient _tableClient;

        public StudentsController()
        {
            string connectionString = "UseDevelopmentStorage=true";
            _tableClient = new TableClient(connectionString, "Students");
            _tableClient.CreateIfNotExists();
        }

        // GET: /Students - Lists all registered students
        public async Task<IActionResult> Index()
        {
            List<StudentEntity> students = new List<StudentEntity>();

            await foreach (StudentEntity student in _tableClient.QueryAsync<StudentEntity>())
            {
                students.Add(student);
            }

            return View(students);
        }

        // GET: /Students/Create - shows the creation form
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Students/Create  Adding a new student
        [HttpPost]
        public async Task<IActionResult> Create(StudentEntity student)
        {
            student.PartitionKey = "StudentPartition";
            student.RowKey = Guid.NewGuid().ToString();

            await _tableClient.AddEntityAsync(student);

            return RedirectToAction(nameof(Index));
        }

        // GET: /Students/Edit/{rowKey}
        public async Task<IActionResult> Edit(string id)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<StudentEntity>("StudentPartition", id);
                return View(response.Value);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return NotFound();
            }
        }

        // POST: /Students/Edit/{rowKey} - only overwrites name/email so an
        // edit here can't accidentally wipe EnrolledCourses set by the queue worker
        [HttpPost]
        public async Task<IActionResult> Edit(string id, StudentEntity student)
        {
            var existingResponse = await _tableClient.GetEntityAsync<StudentEntity>("StudentPartition", id);
            StudentEntity existing = existingResponse.Value;

            existing.StudentName = student.StudentName;
            existing.Email = student.Email;

            await _tableClient.UpdateEntityAsync(existing, existing.ETag, TableUpdateMode.Replace);

            return RedirectToAction(nameof(Index));
        }

        // GET: /Students/Delete/{rowKey}
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<StudentEntity>("StudentPartition", id);
                return View(response.Value);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return NotFound();
            }
        }

        // POST: /Students/Delete/{rowKey}
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            await _tableClient.DeleteEntityAsync("StudentPartition", id);
            return RedirectToAction(nameof(Index));
        }
    }
}
