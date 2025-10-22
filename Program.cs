using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.OpenApi.Models;


    // Program class: entry point for the web application.
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ===== Swagger with API Key (no JWT) =====
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

                // Define an API Key security scheme shown by the "Authorize" button
                c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
                {
                    Description = "Paste your API key here.",
                    Type = SecuritySchemeType.ApiKey,
                    Name = "X-API-Key",           // header name
                    In = ParameterLocation.Header
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "ApiKey"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            builder.Services.AddAuthentication();
            builder.Services.AddAuthorization();

            var app = builder.Build();

            // ===== Global error handler =====
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json";
                    var exception = context.Features.Get<IExceptionHandlerFeature>();
                    if (exception != null)
                    {
                        var errorMessage = $"Exception: {exception.Error.Message}{Environment.NewLine}";
                        Console.WriteLine(errorMessage);
                        var logFilePath = "server.log";
                        await File.AppendAllTextAsync(logFilePath, errorMessage);
                    }
                    await context.Response.WriteAsJsonAsync(new { error = "Internal server error." });
                });
            });

            // ===== Swagger (public) =====
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API v1");
                c.EnablePersistAuthorization(); // keeps the API key across reloads
            });

            // app.UseHttpsRedirection();

            // ===== Minimal API-key protection middleware for /users/* =====
            const string ApiKeyHeaderName = "X-API-Key";
            string ApiKeySecret = Environment.GetEnvironmentVariable("MYAPI_API_KEY")
                                  ?? "HereComesTheSunAndISayItsAlright"; // <-- change this in production

            app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/users"),
                branch =>
                {
                    branch.Use(async (context, next) =>
                    {
                        // Accept header or ?api_key= for convenience
                        var provided =
                            context.Request.Headers[ApiKeyHeaderName].FirstOrDefault()
                            ?? context.Request.Query["api_key"].FirstOrDefault();

                        if (string.IsNullOrWhiteSpace(provided) || provided != ApiKeySecret)
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            await context.Response.WriteAsJsonAsync(new { error = "Invalid or missing API key." });
                            return;
                        }

                        await next();
                    });
                });

            // app.UseHttpLogging();

            // ===== Simple logging =====
            app.Use(async (context, next) =>
            {
                await next();
                var logEntry = $"HTTP {context.Request.Method} {context.Request.Path} -> {context.Response.StatusCode}{Environment.NewLine}";
                Console.WriteLine(logEntry);
                await File.AppendAllTextAsync("server.log", logEntry);
            });

        // ===== Sample data =====
        // Use explicit type with target-typed new() to satisfy simplification diagnostics,
        // and initialize the collection with one sample user.
        List<User> users = new()
            {
                new User { Name = "Alice", Email = "Alice@ntu.ac.uk", Password = "Password123" },
            };
            // Checking encryption

            // Log initial users (serialized) for clarity.
            Console.WriteLine($"Initialized users: {JsonSerializer.Serialize(users)}");

            // "Encrypt" the last user's sensitive data (demo only; use proper crypto in real apps).
            users[^1].EncryptData();

            // Log the encrypted representation (serialize the collection to show contents).
            Console.WriteLine($"Initialized encrypted users: {JsonSerializer.Serialize(users)}");

            // ===== Public endpoints =====
            app.MapGet("/", () => Results.Ok("OK")); // public health

            // ===== Users API (protected by API key middleware) =====
            var usersApi = app.MapGroup("/users");  // no RequireAuthorization; middleware handles protection

            // Return the whole list (in-memory store for demo).
            usersApi.MapGet("/", () => users);

            usersApi.MapGet("/{id}", (int id) =>
            {
                if (id < 0 || id >= users.Count) return Results.NotFound();
                return Results.Ok(users[id]);
            });

            usersApi.MapPost("/add", (User user) =>
            {
                if (user is null) return Results.BadRequest("User payload is null.");
                if (string.IsNullOrWhiteSpace(user.Name)) return Results.BadRequest("Name is required.");
                if (string.IsNullOrWhiteSpace(user.Email) || !user.Email.Contains("@")) return Results.BadRequest("A valid email is required.");
                if (string.IsNullOrWhiteSpace(user.Password) || user.Password.Length < 8) return Results.BadRequest("Password is required and must be at least 8 characters.");

                user.Name = user.Name.Trim();
                user.Email = user.Email.Trim();
                users.Add(user);
                users[^1].EncryptData();
                return Results.Created($"/users/{users.Count - 1}", user);
            });

            usersApi.MapPut("/update/{id}", (int id, User updatedUser) =>
            {
                if (id < 0 || id >= users.Count) return Results.NotFound();
                users[id] = updatedUser;
                users[id].EncryptData();
                return Results.Ok(users[id]);
            });

            usersApi.MapDelete("/delete/{id}", (int id) =>
            {
                if (id < 0 || id >= users.Count) return Results.NotFound();
                users.RemoveAt(id);
                return Results.Ok(users);
            });

            app.Run();
        }
    }
