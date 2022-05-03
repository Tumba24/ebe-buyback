using System.Reflection;
using Evebuyback.Acl;
using Evebuyback.Data;
using EveBuyback.App;
using EveBuyback.Domain;
using MediatR;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddControllers(o => o.InputFormatters.Insert(o.InputFormatters.Count, new PlainTextInputFormatter()));
builder.Services.AddMediatR(Assembly.GetExecutingAssembly());
builder.Services.AddScoped<IItemTypeRepository, InMemoryItemTypeRepository>();
builder.Services.AddScoped<IOrderRepository, EsiOrderRepository>();
builder.Services.AddScoped<IRefinedContractItemAggregateRepository, InMemoryRefinedContractItemAggregateRepository>();
builder.Services.AddScoped<IStationOrderSummaryAggregateRepository, InMemoryStationOrderSummaryAggregateRepository>();
builder.Services.AddScoped<IStationRepository, InMemoryStationRepository>();
builder.Services.AddSwaggerGen();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

var app = builder.Build();

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
