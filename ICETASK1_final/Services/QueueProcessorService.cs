using Azure.Data.Tables;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using ICETASK1.Controllers;
using Microsoft.Extensions.Hosting;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UniversityApp.Controllers;

namespace UniversityApp.Services
{
    // Long-running queue worker, structured around the BackgroundService
    // approach described in Microsoft (2025c)
    public class QueueProcessorService : BackgroundService
    {
        private readonly TableClient _studentTableClient;
        private readonly QueueClient _queueClient;

        public QueueProcessorService()
        {
            string connectionString = "UseDevelopmentStorage=true";

            _studentTableClient = new TableClient(connectionString, "Students");
            _studentTableClient.CreateIfNotExists();

            _queueClient = new QueueClient(connectionString, "courseenrollmentqueue");
            _queueClient.CreateIfNotExists();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    QueueMessage[] messages = await _queueClient.ReceiveMessagesAsync(maxMessages: 1, cancellationToken: stoppingToken);

                    if (messages.Length > 0)
                    {
                        QueueMessage message = messages[0];

                        using (JsonDocument doc = JsonDocument.Parse(message.MessageText))
                        {
                            string studentId = doc.RootElement.GetProperty("StudentID").GetString()!;
                            string courseId = doc.RootElement.GetProperty("CourseID").GetString()!;

                            var response = await _studentTableClient.GetEntityAsync<StudentEntity>("StudentPartition", studentId);
                            StudentEntity student = response.Value;

                            student.EnrolledCourses = string.IsNullOrEmpty(student.EnrolledCourses)
                                ? courseId
                                : $"{student.EnrolledCourses}, {courseId}";

                            await _studentTableClient.UpdateEntityAsync(student, student.ETag, TableUpdateMode.Replace);
                        }

                        await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);
                    }
                }
                catch (Exception)
                {

                }

                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}
