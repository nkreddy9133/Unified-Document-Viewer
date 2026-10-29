-> Understand the requirements and plan the project

I need to complete the Keyloop technical assessment. I've decided to
go with Scenario D, the Unified Document Viewer.

I'm planning to build the backend using C# and ASP.NET Core Web API.
The application should accept a VIN, call two mocked APIs representing
the Sales System and Service System, and combine the documents returned
by both systems into a single response.

I also need persistent database storage, unit tests, a system design
document, and a README explaining how to run the application.

Can you first go through these requirements and suggest a simple
architecture and project structure? I want to use interfaces,
dependency injection, async/await, HttpClient, EF Core, and SQLite.

Please explain how the components will communicate with each other,
what models and interfaces we need, and how we should handle failures
when one of the external APIs is unavailable.

Don't start coding yet. Let's finalize the approach first. Keep it
practical because I need to finish and record the demo today.

--------------------------------------

-> Create the Web API project

Let's start implementing the project based on the architecture we
discussed.

Create the ASP.NET Core Web API project and set up the basic structure.
I want separate folders for controllers, services, clients, models,
and data access.

Keep the controller thin and put the document aggregation logic in
a separate service. Use interfaces and dependency injection so that
we can test the business logic independently.

Configure Swagger, logging, and the necessary NuGet packages.
Use the compatible .NET SDK already installed in my environment.

For now, focus on getting the project structure and dependency
injection ready. Build the project and fix any errors before moving
to the next step.


-------------------------------------------

-> Create the mock systems and sample data

Now let's implement the two mock systems.

The Sales System API should return sample vehicle documents such as
purchase invoices and sales agreements. The Service System API should
return documents such as inspection reports and maintenance records.

Use a few predefined VINs and deterministic sample data so I can
demonstrate different scenarios easily.

Keep the mock data simple and in memory for now. The two external
API clients should call their respective mock endpoints through
HttpClient rather than directly accessing the sample collections.

Also, provide a way to simulate an API failure so we can test how
the main application behaves when one of the systems is unavailable.

Please build and test these endpoints before proceeding.


--------------------------------------------------

-> Implement the document aggregation service

Let's implement the main document aggregation service now.

When a user searches for a VIN, the service should call the Sales
System API and Service System API concurrently and combine their
results into a single response.

Each document must clearly identify its source system. Validate the
VIN, handle empty results, and avoid duplicate documents using a
reasonable rule.

I also want to handle partial failures. If one system fails, return
the documents from the successful system and indicate which source
failed. If both systems fail, return an appropriate error.

Please use async/await and CancellationToken properly. Keep the
business logic out of the controller and make sure exceptions are
logged rather than silently ignored.

Once implemented, build the application and explain the important
parts of the code to me.


--------------------------------------------

-> Add SQLite and search history

The document aggregation is the main feature. Now let's add SQLite
persistence using Entity Framework Core.

I want to save every search attempt with the VIN, timestamp, status,
and number of documents returned where available.

Create the entity and DbContext, configure the connection string,
and make sure the database can be initialized reliably.

Add an API endpoint to retrieve the search history. The records
should still be available after restarting the application.

Keep the database implementation simple. We don't need to store
the mock documents in SQL because those are currently supplied by
the mock systems.

Please verify that search history is actually persisted and explain
how I can inspect or reset the database during development.


----------------------------------------------------


-> Add tests and review the implementation

Now review the implementation as a senior .NET developer.

I want unit tests for the aggregation service, including:
- Both APIs returning documents.
- Both APIs returning empty results.
- One API failing while the other succeeds.
- Both APIs failing.
- Duplicate documents.
- Invalid VINs.
- Correct source-system attribution.

Use xUnit and mock the external dependencies so the unit tests
don't depend on live network calls.

Also review the dependency injection, HttpClient configuration,
error handling, cancellation, logging, and SQLite persistence.

Run dotnet build and dotnet test. If anything fails, investigate
the actual error, fix it, and rerun the relevant checks.

Please tell me which tests passed and which issues remain.
Don't claim that something works unless you have verified it.


-------------------------------------------------------

-> Create the system design document

Let's prepare the system design document based on the code we
actually implemented.

Create SystemDesign.md and include the architecture diagram,
component responsibilities, data flow, API contracts, technology
choices, database design, and testing strategy.

Explain how the parallel API calls work and how we handle partial
failures and total failures.

Include an observability section covering logging, metrics, and
tracing. Clearly distinguish what we implemented from what we
would add in a production environment.

Also document the assumptions we made and explain how AI assisted
with the design and implementation.

Keep the diagram in Mermaid format so it can be maintained alongside
the source code.


--------------------------------------------------------

-> Prepare the README and AI collaboration section

Create a README.md that makes this project easy for another
developer to understand and run.

Include the prerequisites, setup instructions, database initialization,
commands to run the application and tests, Swagger usage, API
endpoints, example requests, and sample VINs.

Add a section called AI Collaboration Narrative.

Explain how I broke the work into smaller tasks, directed the coding
agent, reviewed its changes, investigated issues, and verified the
implementation through builds and tests.

Base the narrative on the actual development process and results.
Don't invent bugs, test results, or verification steps that didn't
happen.

Finally, check that the instructions are consistent with the
project's actual structure and configuration.


---------------------------------------------------------

-> Final acceptance check

Before I record the demo, compare the implementation against the
original Keyloop assessment requirements.

Check the document aggregation, parallel requests, source attribution,
error handling, persistent database, automated tests, system design
document, README, and AI Collaboration Narrative.

Build and test the application, and tell me the actual results.

Give me a checklist showing each requirement, where it is implemented,
how it was tested, and whether anything is still missing.

Prioritize fixing mandatory requirements and critical defects.
Don't add new features unless they are necessary for the assessment.

I need the application to be stable enough to demonstrate today.
