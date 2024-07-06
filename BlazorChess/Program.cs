using BlazorChessMiddleware.DependencyInjectionsInterfaces;
using Microsoft.AspNetCore.ResponseCompression;
using BlazorChess.Components;


// CHANGE YOUR DEPENDENCIES
using LobbyCodeGenerator;
using PlayerLobbies;
using GameManager;
using ChessCore;
using ChessHub;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream"]);
});

// CHANGE YOUR CLASSES IMPLEMENTATIONS HERE
builder.Services.AddSingleton<IPlayerLobbies, MyPlayerLobbies>();
builder.Services.AddSingleton<IGameManager, MyGameManager>();
builder.Services.AddSingleton<ILobbyCodeGenetor, MyLobbyCodeGenerator>();
builder.Services.AddSingleton<IChessCore, ClassicalChessCore>();

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseResponseCompression();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

// CHANGE YOUR CHESSHUB CLASS IMPLEMENTATION HERE
app.MapHub<MyChessHub>("/chesshub");

app.Run();
