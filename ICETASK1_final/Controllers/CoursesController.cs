using Microsoft.AspNetCore.Mvc;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace UniversityApp.Controllers
{
    // Table Entity Model for Azure Table Storage
    // shape follows the ITableEntity interface as documented by Microsoft (2025a)
    public class CourseEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "CoursePartition";
        public string RowKey { get; set; } = default!;
        public string CourseName { get; set; } = default!;
        public string Instructor { get; set; } = default!;
        public ETag ETag { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }

    public class CoursesController : Controller
    {
        private readonly TableClient _tableClient;
        private readonly QueueClient _queueClient;

        public CoursesController()
        {
            string connectionString = "UseDevelopmentStorage=true";

            _tableClient = new TableClient(connectionString, "Courses");
            _tableClient.CreateIfNotExists();

            _queueClient = new QueueClient(connectionString, "courseenrollmentqueue");
            _queueClient.CreateIfNotExists();
        }

        // GET: /Courses  Retrieve and list all available courses
        public async Task<IActionResult> Index()
        {
            List<CourseEntity> courses = new List<CourseEntity>();

            await foreach (CourseEntity course in _tableClient.QueryAsync<CourseEntity>())
            {
                courses.Add(course);
            }

            return View(courses);
        }

        // GET: /Courses/Create  creation form
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Courses/Create - Insert a new course into Azure Table Storage
        [HttpPost]
        public async Task<IActionResult> Create(CourseEntity course)
        {
            course.PartitionKey = "CoursePartition";
            course.RowKey = Guid.NewGuid().ToString(); // Assign unique identifier

            await _tableClient.AddEntityAsync(course);

            return RedirectToAction(nameof(Index));
        }

        // POST: /Courses/Enroll - queues the request instead of writing to the
        // student record right away, following the queue producer pattern in Microsoft (2025b)
        [HttpPost]
        public async Task<IActionResult> Enroll(string courseId, string studentId)
        {
            var enrollmentPayload = new
            {
                StudentID = studentId,
                CourseID = courseId,
                Timestamp = DateTime.UtcNow
            };

            string messageText = JsonSerializer.Serialize(enrollmentPayload);

            await _queueClient.SendMessageAsync(messageText);

            TempData["Success"] = "Enrollment request added to processing queue!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Courses/Edit/{rowKey}
        public async Task<IActionResult> Edit(string id)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<CourseEntity>("CoursePartition", id);
                return View(response.Value);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return NotFound();
            }
        }

        // POST: /Courses/Edit/{rowKey} - upsert keeps this simple since we
        // already have the RowKey from the hidden field on the form
        [HttpPost]
        public async Task<IActionResult> Edit(string id, CourseEntity course)
        {
            course.RowKey = id;
            course.PartitionKey = "CoursePartition";

            await _tableClient.UpsertEntityAsync(course, TableUpdateMode.Replace);

            return RedirectToAction(nameof(Index));
        }

        // GET: /Courses/Delete/{rowKey}
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<CourseEntity>("CoursePartition", id);
                return View(response.Value);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return NotFound();
            }
        }

        // POST: /Courses/Delete/{rowKey}
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            await _tableClient.DeleteEntityAsync("CoursePartition", id);
            return RedirectToAction(nameof(Index));
        }
    }
}
