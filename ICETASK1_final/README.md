# ICETASK1 - University Course Enrolment (ICE Task)

MVC app on .NET, using Azure Table Storage for Courses and Students, and Azure Queue Storage (queue: courseenrollmentqueue) to handle enrolment requests asynchronously. Built to run against Azurite - not deployed to Azure.

## Running it

1. Start Azurite: `azurite --silent --location ./azurite-data`
2. `dotnet run` from this folder
3. Add a course, add a student, then use the Enroll form on the Courses page (you'll need the student's RowKey, shown on the Students page)
4. Wait ~5 seconds for the background worker to process the queue, then refresh Students to see the enrolment reflected

## What was fixed from the starting point

- `ICETASK1.csproj` had no Azure package references at all - added Azure.Data.Tables and Azure.Storage.Queues
- `Program.cs` called `AddHostedService` after `builder.Build()`, which throws at startup since the service collection can't be modified once built - moved it above `Build()`
- `StudentsController.cs` was missing several usings (Azure, Azure.Data.Tables, System, System.Threading.Tasks, System.Collections.Generic) and wouldn't compile
- `StudentsController.cs` had no GET `Create()` action, only the POST - so the Create page 404'd
- `QueueProcessorService.cs` had no error handling - one bad or missing student record would crash the whole background worker permanently. Wrapped the message-handling logic in a try/catch
- No Views existed for Courses or Students at all - added Index and Create for both
- `_ViewImports.cshtml` didn't reference the namespaces the entity classes actually live in (`UniversityApp.Controllers`, `ICETASK1.Controllers`) - added both
- Neither controller had Edit or Delete actions, only Create and Index - added both to `CoursesController` and `StudentsController`, plus the matching views and Index page links. The Student edit form only touches name/email so it can't accidentally overwrite `EnrolledCourses`, which is written by the background queue worker

## Still worth knowing about

- `CourseEntity`/`CoursesController` are in `UniversityApp.Controllers`; everything else is in `ICETASK1.Controllers`. Works fine (routing is by class name, not namespace) but inconsistent.
- `appsettings.json` defines a `ConnectionStrings:DefaultConnection` value but none of the controllers actually read it - they each hardcode `"UseDevelopmentStorage=true"` directly instead.
- RowKeys are GUIDs rather than readable course/student IDs.

## Use of AI assistance

Parts of this submission were completed with help from Claude (Anthropic), specifically diagnosing the compile/runtime errors listed above, drafting the views that were missing, and putting together the reference list below.

## Reference list

Anthropic (2026) *Claude* (Sonnet 5) [Large language model]. Available at: https://claude.ai (Accessed: 1 August 2026).

Microsoft (2025a) *Get started with Azure Table storage using .NET*. Available at: https://learn.microsoft.com/en-us/azure/storage/tables/table-storage-quickstart-dotnet (Accessed: 1 August 2026).

Microsoft (2025b) *Get started with Azure Queue storage using .NET*. Available at: https://learn.microsoft.com/en-us/azure/storage/queues/storage-quickstart-queues-dotnet (Accessed: 1 August 2026).

Microsoft (2025c) *Implement background tasks with IHostedService and BackgroundService*. Available at: https://learn.microsoft.com/en-us/dotnet/architecture/microservices/multi-container-microservice-net-applications/background-tasks-with-ihostedservice (Accessed: 1 August 2026).

Microsoft (2025d) *Use Azurite emulator for local Azure Storage development*. Available at: https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite (Accessed: 1 August 2026).

Microsoft (2025e) *Dependency injection guidelines*. Available at: https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines (Accessed: 1 August 2026).
