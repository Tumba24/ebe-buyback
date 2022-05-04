using System.Reflection;
using Evebuyback.Acl;
using Evebuyback.Data;
using EveBuyback.App;
using EveBuyback.Domain;
using MediatR;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(o => o.InputFormatters.Insert(o.InputFormatters.Count, new PlainTextInputFormatter()))
    .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = context =>
    {
        var errorMessage = context.ModelState?
            .Values.FirstOrDefault(v => v.Errors.Any())?
            .Errors?.First()?
            .ErrorMessage ?? string.Empty;

        throw new BadHttpRequestException(errorMessage, StatusCodes.Status400BadRequest);
    });

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

app.UseStatusCodePagesWithReExecute("/error/{0}");
app.UseExceptionHandler("/error/500");

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
